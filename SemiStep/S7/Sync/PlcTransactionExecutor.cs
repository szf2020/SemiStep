using FluentResults;

using S7.Protocol;
using S7.Serialization;

using Serilog;

using TypesShared.Core;
using TypesShared.Plc;

namespace S7.Sync;

internal sealed class PlcTransactionExecutor
{
	private readonly ArrayCodec _arrayCodec;
	private readonly RecipeConverter _converter;
	private readonly ExecutionStateCodec _executionCodec;
	private readonly PlcProtocolLayout _layout;
	private readonly ManagingAreaCodec _managingCodec;
	private readonly PlcProtocolSettings _protocolSettings;
	private readonly IS7Transport _transport;

	public PlcTransactionExecutor(
		IS7Transport transport,
		RecipeConverter converter,
		PlcConfiguration plcConfiguration)
	{
		_transport = transport;
		_converter = converter;
		_layout = plcConfiguration.Layout;
		_protocolSettings = plcConfiguration.ProtocolSettings;
		_arrayCodec = new ArrayCodec(_layout.IntDb, _layout.FloatDb, _layout.StringDb);
		_executionCodec = new ExecutionStateCodec(_layout.ExecutionDb);
		_managingCodec = new ManagingAreaCodec(_layout.ManagingDb);
	}

	public async Task<bool> IsRecipeActiveAsync(CancellationToken ct = default)
	{
		EnsureConnected();

		var bytes = await _transport.ReadBytesAsync(
			_layout.ExecutionDb.DbNumber,
			0,
			_layout.ExecutionDb.TotalSize,
			ct);

		var state = _executionCodec.Decode(bytes);

		return state.RecipeActive;
	}

	public async Task<PlcExecutionState> ReadExecutionStateAsync(CancellationToken ct = default)
	{
		EnsureConnected();

		var bytes = await _transport.ReadBytesAsync(
			_layout.ExecutionDb.DbNumber,
			0,
			_layout.ExecutionDb.TotalSize,
			ct);

		return _executionCodec.Decode(bytes);
	}

	public async Task<PlcRecipeData> ReadRecipeDataAsync(CancellationToken ct = default)
	{
		EnsureConnected();

		var intHeaderBytes = await _transport.ReadBytesAsync(
			_layout.IntDb.DbNumber,
			0,
			_layout.IntDb.DataStartOffset,
			ct);
		var intCount = ArrayCodec.ReadArrayCurrentSize(intHeaderBytes, _layout.IntDb);

		var floatHeaderBytes = await _transport.ReadBytesAsync(
			_layout.FloatDb.DbNumber,
			0,
			_layout.FloatDb.DataStartOffset,
			ct);
		var floatCount = ArrayCodec.ReadArrayCurrentSize(floatHeaderBytes, _layout.FloatDb);

		var stringHeaderBytes = await _transport.ReadBytesAsync(
			_layout.StringDb.DbNumber,
			0,
			_layout.StringDb.DataStartOffset,
			ct);
		var stringCount = ArrayCodec.ReadArrayCurrentSize(stringHeaderBytes, _layout.StringDb);

		var intDataSize = _layout.IntDb.DataStartOffset + intCount * ProtocolConstants.IntElementSize;
		var floatDataSize = _layout.FloatDb.DataStartOffset + floatCount * ProtocolConstants.FloatElementSize;
		var stringDataSize = _layout.StringDb.DataStartOffset + stringCount * ProtocolConstants.WStringElementSize;

		var intData = await _transport.ReadBytesAsync(_layout.IntDb.DbNumber, 0, intDataSize, ct);
		var floatData = await _transport.ReadBytesAsync(_layout.FloatDb.DbNumber, 0, floatDataSize, ct);
		var stringData = await _transport.ReadBytesAsync(_layout.StringDb.DbNumber, 0, stringDataSize, ct);

		var intValues = _arrayCodec.DecodeIntArray(intData, intCount);
		var floatValues = _arrayCodec.DecodeFloatArray(floatData, floatCount);
		var stringValues = _arrayCodec.DecodeStringArray(stringData, stringCount);

		var stepCount = intCount;

		return new PlcRecipeData(intValues, floatValues, stringValues, stepCount);
	}

	public async Task WriteRecipeWithRetryAsync(Recipe recipe, CancellationToken ct = default)
	{
		var recipeData = _converter.FromRecipe(recipe);

		for (var attempt = 1; attempt <= _protocolSettings.MaxRetryAttempts; attempt++)
		{
			await WriteRecipeDataAsync(recipeData, ct);

			var verified = await VerifyWriteAsync(recipeData, ct);

			if (verified)
			{
				Log.Information(
					"Recipe synced to PLC successfully ({StepCount} steps, attempt {Attempt})",
					recipe.StepCount,
					attempt);

				return;
			}

			Log.Warning(
				"Write verification failed on attempt {Attempt} of {MaxAttempts}",
				attempt,
				_protocolSettings.MaxRetryAttempts);
		}

		throw new PlcWriteVerificationException(
			$"Recipe write verification failed after {_protocolSettings.MaxRetryAttempts} attempts");
	}

	public async Task<ManagingAreaState> ReadManagingAreaAsync(CancellationToken ct = default)
	{
		EnsureConnected();

		var bytes = await _transport.ReadBytesAsync(
			_layout.ManagingDb.DbNumber,
			0,
			_layout.ManagingDb.TotalSize,
			ct);

		return _managingCodec.Decode(bytes);
	}

	public async Task<Result<Recipe>> ReadRecipeFromPlcAsync(CancellationToken ct = default)
	{
		var managingAreaState = await ReadManagingAreaAsync(ct);

		if (!managingAreaState.Committed)
		{
			return Result.Fail("Recipe not committed on PLC");
		}

		var recipeData = await ReadRecipeDataAsync(ct);

		try
		{
			var recipe = _converter.ToRecipe(recipeData);
			return Result.Ok(recipe);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to convert PLC recipe data to Recipe");
			return Result.Fail($"Failed to convert PLC data to recipe: {ex.Message}");
		}
	}

	private async Task WriteRecipeDataAsync(PlcRecipeData data, CancellationToken ct)
	{
		EnsureConnected();

		await WriteManagingAreaAsync(new ManagingAreaPcData(Committed: false, RecipeLines: 0), ct);

		var intBytes = _arrayCodec.EncodeIntArray(data.IntValues);
		var floatBytes = _arrayCodec.EncodeFloatArray(data.FloatValues);
		var stringBytes = _arrayCodec.EncodeStringArray(data.StringValues);

		await _transport.WriteBytesAsync(_layout.IntDb.DbNumber, 0, intBytes, ct);
		await _transport.WriteBytesAsync(_layout.FloatDb.DbNumber, 0, floatBytes, ct);
		await _transport.WriteBytesAsync(_layout.StringDb.DbNumber, 0, stringBytes, ct);

		await WriteManagingAreaAsync(new ManagingAreaPcData(Committed: false, RecipeLines: data.StepCount), ct);

		await WriteManagingAreaAsync(new ManagingAreaPcData(Committed: true, RecipeLines: data.StepCount), ct);
	}

	private async Task<bool> VerifyWriteAsync(PlcRecipeData expected, CancellationToken ct)
	{
		var actual = await ReadRecipeDataAsync(ct);

		if (actual.IntValues.Length != expected.IntValues.Length)
		{
			return false;
		}

		if (actual.FloatValues.Length != expected.FloatValues.Length)
		{
			return false;
		}

		if (actual.StringValues.Length != expected.StringValues.Length)
		{
			return false;
		}

		for (var i = 0; i < expected.IntValues.Length; i++)
		{
			if (actual.IntValues[i] != expected.IntValues[i])
			{
				return false;
			}
		}

		for (var i = 0; i < expected.FloatValues.Length; i++)
		{
			if (!CompareFloats_ExpectedBytesEqual(actual.FloatValues[i], expected.FloatValues[i]))
			{
				return false;
			}
		}

		for (var i = 0; i < expected.StringValues.Length; i++)
		{
			if (actual.StringValues[i] != expected.StringValues[i])
			{
				return false;
			}
		}

		return true;
	}

	// Floats are serialised to raw IEEE 754 bytes, written to the PLC, and read back without
	// any arithmetic transformation. The round-trip is byte-exact, so bit-exact equality is
	// the correct comparison here — not an epsilon-based approximation.
	// BitConverter.SingleToInt32Bits is used rather than == to correctly handle NaN payloads
	// and distinguish +0 from -0 (different IEEE-754 bit patterns).
	private static bool CompareFloats_ExpectedBytesEqual(float actual, float expected)
	{
		return BitConverter.SingleToInt32Bits(actual) == BitConverter.SingleToInt32Bits(expected);
	}

	private async Task WriteManagingAreaAsync(ManagingAreaPcData data, CancellationToken ct)
	{
		var bytes = _managingCodec.EncodePcData(data);
		await _transport.WriteBytesAsync(_layout.ManagingDb.DbNumber, 0, bytes, ct);
	}

	private void EnsureConnected()
	{
		if (!_transport.IsConnected)
		{
			throw new PlcNotConnectedException("Not connected to PLC");
		}
	}
}
