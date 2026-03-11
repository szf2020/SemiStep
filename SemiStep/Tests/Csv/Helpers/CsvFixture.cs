using Csv.Services;

using Xunit;

namespace Tests.Csv.Helpers;

public sealed class CsvFixture : IAsyncLifetime
{
	internal CsvSerializer Serializer { get; private set; } = null!;

	private IServiceProvider? _services;

	public async Task InitializeAsync()
	{
		var (serializer, services) = await CsvTestHelper.BuildAsync();
		Serializer = serializer;
		_services = services;
	}

	public Task DisposeAsync()
	{
		(_services as IDisposable)?.Dispose();
		return Task.CompletedTask;
	}
}
