using Microsoft.Extensions.DependencyInjection;

using S7.Facade;
using S7.Serialization;
using S7.Sync;

using Shared.Config;
using Shared.ServiceContracts;

namespace S7;

public static class S7Di
{
	public static IServiceCollection AddS7(this IServiceCollection services)
	{
		services.AddSingleton<S7Driver>();
		services.AddSingleton<RecipeConverter>();
		services.AddSingleton<PlcTransactionExecutor>();
		services.AddSingleton<PlcSyncCoordinator>();
		services.AddSingleton<IS7ConnectionService, S7ConnectionService>();

		services.AddSingleton(sp => sp.GetRequiredService<AppConfiguration>().PlcConfiguration);
		return services;
	}
}
