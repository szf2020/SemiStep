using Microsoft.Extensions.DependencyInjection;

using S7.Facade;
using S7.Serialization;
using S7.Sync;

using TypesShared.Config;
using TypesShared.Domain;

namespace S7;

public static class S7Di
{
	public static IServiceCollection AddS7(this IServiceCollection services)
	{
		services.AddSingleton<S7Driver>();
		services.AddSingleton<RecipeConverter>();
		services.AddSingleton<PlcTransactionExecutor>();
		services.AddSingleton<PlcSyncCoordinator>();
		services.AddSingleton<IS7Service, Is7Service>();

		services.AddSingleton(sp => sp.GetRequiredService<AppConfiguration>().PlcConfiguration);
		return services;
	}
}
