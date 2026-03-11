using Config;
using Config.Facade;

using Core;

using Csv.Services;

using Domain;
using Domain.Facade;

using Microsoft.Extensions.DependencyInjection;

using Shared.ServiceContracts;

using Tests.Helpers;

namespace Tests.Csv.Helpers;

internal static class CsvTestHelper
{
	public static async Task<(CsvSerializer Serializer, IServiceProvider Services)> BuildAsync(
		string configName = "Standard")
	{
		var configDir = TestConfigLocator.GetConfigDirectory(configName);
		var configuration = await ConfigFacade.LoadAndValidateAsync(configDir);

		var services = new ServiceCollection()
			.AddSingleton(configuration)
			.AddRecipe()
			.AddConfig()
			.AddDomain()
			.AddSingleton<ICsvService, StubCsvService>()
			.AddSingleton<IS7ConnectionService, StubS7ConnectionService>()
			.AddSingleton<CsvSerializer>()
			.BuildServiceProvider();

		var domainFacade = services.GetRequiredService<DomainFacade>();
		domainFacade.Initialize();

		var serializer = services.GetRequiredService<CsvSerializer>();
		return (serializer, services);
	}
}
