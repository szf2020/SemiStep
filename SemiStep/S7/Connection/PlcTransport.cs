using S7.Net;

using Shared.Entities;

namespace S7.Connection;

public sealed class PlcTransport : IAsyncDisposable
{
	private Plc? _plc;

	public bool IsConnected => _plc?.IsConnected ?? false;

	public async Task ConnectAsync(PlcConnectionSettings settings, CancellationToken ct = default)
	{
		var endpoint = $"{settings.IpAddress}:{settings.Port}";

		_plc = new Plc(
			CpuType.S71500,
			endpoint,
			(short)settings.Rack,
			(short)settings.Slot);

		await _plc.OpenAsync(ct);
	}

	public Task DisconnectAsync(CancellationToken ct = default)
	{
		_plc?.Close();
		_plc = null;

		return Task.CompletedTask;
	}

	public async Task<byte[]> ReadBytesAsync(int dbNumber, int startByte, int count, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		return await _plc!.ReadBytesAsync(DataType.DataBlock, dbNumber, startByte, count, ct);
	}

	public async Task WriteBytesAsync(int dbNumber, int startByte, byte[] data, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		await _plc!.WriteBytesAsync(DataType.DataBlock, dbNumber, startByte, data, ct);
	}

	public async ValueTask DisposeAsync()
	{
		if (_plc is not null)
		{
			await DisconnectAsync();
		}
	}
}
