using System.Buffers.Binary;

using FluentAssertions;

using S7.Protocol;
using S7.Serialization;

using TypesShared.Plc.Memory;

using Xunit;

namespace Tests.S7;

[Trait("Component", "S7")]
[Trait("Area", "Serialization")]
[Trait("Category", "Unit")]
public sealed class ManagingAreaCodecTests
{
	private static ManagingDbLayout DefaultLayout => ManagingDbLayout.Default;

	private static ManagingAreaCodec BuildCodec()
	{
		return new ManagingAreaCodec(DefaultLayout);
	}

	[Fact]
	public void EncodePcData_CommittedTrue_WritesByteOneAtOffset0()
	{
		var codec = BuildCodec();
		var data = new ManagingAreaPcData(Committed: true, RecipeLines: 0);

		var bytes = codec.EncodePcData(data);

		bytes[DefaultLayout.CommittedOffset].Should().Be(0x01);
	}

	[Fact]
	public void EncodePcData_CommittedFalse_WritesZeroAtOffset0()
	{
		var codec = BuildCodec();
		var data = new ManagingAreaPcData(Committed: false, RecipeLines: 0);

		var bytes = codec.EncodePcData(data);

		bytes[DefaultLayout.CommittedOffset].Should().Be(0x00);
	}

	[Fact]
	public void EncodePcData_Returns6ByteArray()
	{
		var codec = BuildCodec();
		var data = new ManagingAreaPcData(Committed: true, RecipeLines: 42);

		var bytes = codec.EncodePcData(data);

		bytes.Should().HaveCount(DefaultLayout.TotalSize);
	}

	[Fact]
	public void EncodePcData_RecipeLines_WrittenBigEndianAtOffset2()
	{
		var codec = BuildCodec();
		const int RecipeLines = 0x01020304;
		var data = new ManagingAreaPcData(Committed: false, RecipeLines: RecipeLines);

		var bytes = codec.EncodePcData(data);

		var decoded = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(DefaultLayout.RecipeLinesOffset));
		decoded.Should().Be(RecipeLines);
	}

	[Fact]
	public void EncodePcData_ZeroRecipeLines_Writes4ZeroBytes()
	{
		var codec = BuildCodec();
		var data = new ManagingAreaPcData(Committed: true, RecipeLines: 0);

		var bytes = codec.EncodePcData(data);

		bytes[2].Should().Be(0);
		bytes[3].Should().Be(0);
		bytes[4].Should().Be(0);
		bytes[5].Should().Be(0);
	}

	[Fact]
	public void Decode_CommittedByte_NonZeroIsTrue()
	{
		var codec = BuildCodec();
		var bytes = new byte[DefaultLayout.TotalSize];
		bytes[DefaultLayout.CommittedOffset] = 0x01;

		var state = codec.Decode(bytes);

		state.Committed.Should().BeTrue();
	}

	[Fact]
	public void Decode_CommittedByte_ZeroIsFalse()
	{
		var codec = BuildCodec();
		var bytes = new byte[DefaultLayout.TotalSize];
		bytes[DefaultLayout.CommittedOffset] = 0x00;

		var state = codec.Decode(bytes);

		state.Committed.Should().BeFalse();
	}

	[Fact]
	public void Decode_RecipeLines_ParsedBigEndianFromOffset2()
	{
		var codec = BuildCodec();
		var bytes = new byte[DefaultLayout.TotalSize];
		const int ExpectedLines = 99;
		BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(DefaultLayout.RecipeLinesOffset), ExpectedLines);

		var state = codec.Decode(bytes);

		state.RecipeLines.Should().Be(ExpectedLines);
	}

	[Fact]
	public void Decode_AllZeroBytes_ReturnsCommittedFalseAndZeroLines()
	{
		var codec = BuildCodec();
		var bytes = new byte[DefaultLayout.TotalSize];

		var state = codec.Decode(bytes);

		state.Committed.Should().BeFalse();
		state.RecipeLines.Should().Be(0);
	}

	[Fact]
	public void Decode_TooShortData_ThrowsArgumentException()
	{
		var codec = BuildCodec();
		var shortBytes = new byte[3];

		var act = () => codec.Decode(shortBytes);

		act.Should().Throw<ArgumentException>()
			.WithMessage("*length*");
	}

	[Fact]
	public void RoundTrip_CommittedTrueWithLines_PreservesValues()
	{
		var codec = BuildCodec();
		const int Lines = 17;
		var pcData = new ManagingAreaPcData(Committed: true, RecipeLines: Lines);

		var bytes = codec.EncodePcData(pcData);
		var state = codec.Decode(bytes);

		state.Committed.Should().BeTrue();
		state.RecipeLines.Should().Be(Lines);
	}

	[Fact]
	public void RoundTrip_CommittedFalseWithLines_PreservesValues()
	{
		var codec = BuildCodec();
		const int Lines = 5;
		var pcData = new ManagingAreaPcData(Committed: false, RecipeLines: Lines);

		var bytes = codec.EncodePcData(pcData);
		var state = codec.Decode(bytes);

		state.Committed.Should().BeFalse();
		state.RecipeLines.Should().Be(Lines);
	}
}
