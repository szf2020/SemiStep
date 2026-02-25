using Domain.Ports;

using Microsoft.Extensions.DependencyInjection;

using S7.Connection;
using S7.Serialization;
using S7.Sync;

using Serilog;

namespace S7;

public static class S7Di
{
	public static IServiceCollection AddS7(this IServiceCollection services, ILogger? logger = null)
	{
		if (logger is not null)
		{
			services.AddSingleton(logger);
		}

		services.AddSingleton<PlcTransport>();
		services.AddSingleton<S7ConnectionService>();
		services.AddSingleton<RecipeConverter>();
		services.AddSingleton<PlcTransactionExecutor>();
		services.AddSingleton<PlcSyncCoordinator>();
		services.AddSingleton<IS7ConnectionService, S7ConnectionService>();

		return services;
	}
}
