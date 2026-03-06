using Csv.Facade;
using Csv.Services;

using Domain.Ports;

using Microsoft.Extensions.DependencyInjection;

namespace Csv;

public static class CsvDi
{
	public static IServiceCollection AddCsv(this IServiceCollection services)
	{
		services.AddSingleton<CsvSerializer>();
		services.AddSingleton<ICsvService, CsvService>();

		return services;
	}
}
