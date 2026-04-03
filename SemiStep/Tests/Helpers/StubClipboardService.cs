using FluentResults;

using TypesShared.Core;
using TypesShared.Domain;

namespace Tests.Helpers;

public sealed class StubClipboardService : IClipboardService
{
	public string SerializeSteps(Recipe recipe)
	{
		throw new NotSupportedException("StubCsvClipboardService does not support serialization.");
	}

	public Result<Recipe> DeserializeSteps(string csvBody)
	{
		throw new NotSupportedException("StubCsvClipboardService does not support deserialization.");
	}
}
