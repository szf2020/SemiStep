using System.Buffers.Binary;

using S7.Protocol;

using TypesShared.Plc.Memory;

namespace S7.Serialization;

internal sealed class ManagingAreaCodec(ManagingDbLayout layout)
{
	public ManagingAreaState Decode(byte[] data)
	{
		if (data.Length < layout.TotalSize)
		{
			throw new ArgumentException(
				$"Data length {data.Length} is less than expected {layout.TotalSize}");
		}

		return new ManagingAreaState(
			PcStatus: (PcStatus)BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(layout.PcStatusOffset)),
			PcTransactionId:
			BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(layout.PcTransactionIdOffset)),
			PcChecksumInt: BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(layout.PcChecksumIntOffset)),
			PcChecksumFloat:
			BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(layout.PcChecksumFloatOffset)),
			PcChecksumString: BinaryPrimitives.ReadUInt32BigEndian(
				data.AsSpan(layout.PcChecksumStringOffset)),
			PcRecipeLines: BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(layout.PcRecipeLinesOffset)),
			PlcStatus: (PlcSyncStatus)BinaryPrimitives.ReadUInt16BigEndian(
				data.AsSpan(layout.PlcStatusOffset)),
			PlcError: (PlcSyncError)BinaryPrimitives.ReadUInt16BigEndian(
				data.AsSpan(layout.PlcErrorOffset)),
			PlcStoredId: BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(layout.PlcStoredIdOffset)),
			PlcChecksumInt: BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(layout.PlcChecksumIntOffset)),
			PlcChecksumFloat: BinaryPrimitives.ReadUInt32BigEndian(
				data.AsSpan(layout.PlcChecksumFloatOffset)),
			PlcChecksumString: BinaryPrimitives.ReadUInt32BigEndian(
				data.AsSpan(layout.PlcChecksumStringOffset)));
	}

	public byte[] EncodePcData(ManagingAreaPcData data)
	{
		var bytes = new byte[layout.PcDataSize];

		BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(layout.PcStatusOffset), (ushort)data.Status);
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(layout.PcTransactionIdOffset),
			data.TransactionId);
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(layout.PcChecksumIntOffset), data.ChecksumInt);
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(layout.PcChecksumFloatOffset),
			data.ChecksumFloat);
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(layout.PcChecksumStringOffset),
			data.ChecksumString);
		BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(layout.PcRecipeLinesOffset), data.RecipeLines);

		return bytes;
	}
}
