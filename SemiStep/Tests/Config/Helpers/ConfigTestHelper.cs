using Config.Facade;
using Config.Models;

using Shared;
using Shared.Config;

namespace Tests.Config.Helpers;

internal static class ConfigTestHelper
{
	public static async Task<AppConfiguration> LoadValidCaseAsync(string caseName = "Standard")
	{
		using var tempDir = TestDataCopier.PrepareValidCase(caseName);
		var context = await ConfigFacade.LoadAsync(tempDir.Path);

		if (context.HasErrors || context.Configuration is null)
		{
			var errors = string.Join("; ", context.Errors);

			throw new InvalidOperationException($"Expected valid config but got errors: {errors}");
		}

		return context.Configuration;
	}

	public static async Task<ConfigContext> LoadInvalidCaseAsync(string invalidCaseName)
	{
		using var tempDir = TestDataCopier.PrepareInvalidCase(invalidCaseName);
		return await ConfigFacade.LoadAsync(tempDir.Path);
	}

	public static async Task<ConfigContext> LoadStandaloneCaseAsync(string caseName)
	{
		using var tempDir = TestDataCopier.PrepareStandaloneCase(caseName);
		return await ConfigFacade.LoadAsync(tempDir.Path);
	}
}
