using System.Buffers.Binary;

using S7.Protocol;

using TypesShared.Plc.Memory;

namespace S7.Serialization;

internal sealed class ManagingAreaCodec
{
	private readonly ManagingDbLayout _layout;

	public ManagingAreaCodec(ManagingDbLayout layout)
	{
		if (layout.TotalSize < layout.RecipeLinesOffset + sizeof(int))
		{
			throw new ArgumentException(
				$"ManagingDbLayout.TotalSize ({layout.TotalSize}) must be at least " +
				$"RecipeLinesOffset ({layout.RecipeLinesOffset}) + 4 bytes",
				nameof(layout));
		}

		if (layout.TotalSize <= layout.CommittedOffset)
		{
			throw new ArgumentException(
				$"ManagingDbLayout.TotalSize ({layout.TotalSize}) must be greater than " +
				$"CommittedOffset ({layout.CommittedOffset})",
				nameof(layout));
		}

		_layout = layout;
	}

	public ManagingAreaState Decode(byte[] data)
	{
		if (data.Length < _layout.TotalSize)
		{
			throw new ArgumentException(
				$"Data length {data.Length} is less than expected {_layout.TotalSize}");
		}

		var committed = data[_layout.CommittedOffset] != 0;
		var recipeLines = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(_layout.RecipeLinesOffset));

		return new ManagingAreaState(committed, recipeLines);
	}

	public byte[] EncodePcData(ManagingAreaPcData data)
	{
		var bytes = new byte[_layout.TotalSize];

		bytes[_layout.CommittedOffset] = data.Committed ? (byte)0x01 : (byte)0x00;
		BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(_layout.RecipeLinesOffset), data.RecipeLines);

		return bytes;
	}
}
