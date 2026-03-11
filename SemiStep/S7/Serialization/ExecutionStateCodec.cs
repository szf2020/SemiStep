using System.Buffers.Binary;

using S7.Protocol;

using Shared.Plc.Memory;

namespace S7.Serialization;

internal sealed class ExecutionStateCodec(ExecutionDbLayout layout)
{
	public PlcExecutionState Decode(byte[] data)
	{
		if (data.Length < layout.TotalSize)
		{
			throw new ArgumentException(
				$"Data length {data.Length} is less than expected {layout.TotalSize}");
		}

		return new PlcExecutionState(
			RecipeActive: data[layout.RecipeActiveOffset] != 0 ||
						  data[layout.RecipeActiveOffset + 1] != 0,
			ActualLine: BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(layout.ActualLineOffset)),
			StepCurrentTime: BitConverter.Int32BitsToSingle(
				BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(layout.StepCurrentTimeOffset))),
			ForLoopCount1: BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(layout.ForLoopCount1Offset)),
			ForLoopCount2: BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(layout.ForLoopCount2Offset)),
			ForLoopCount3: BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(layout.ForLoopCount3Offset)));
	}
}
