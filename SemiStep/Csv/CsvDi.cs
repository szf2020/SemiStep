using Domain.Ports;

using Microsoft.Extensions.DependencyInjection;

namespace Csv;

public static class CsvDi
{
	public static IServiceCollection AddCsv(this IServiceCollection services)
	{
		return services;
	}
}
