using Config.Facade;

using FluentAssertions;

using Tests.Config.Helpers;

using Xunit;

namespace Tests.Config.Integration.Errors;

[Trait("Category", "Integration")]
[Trait("Component", "Config")]
[Trait("Area", "FileSystemErrors")]
public sealed class FileSystemErrorTests
{
	[Fact]
	public async Task MissingConfigDirectory_HasError()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		var context = await ConfigFacade.LoadAsync(nonExistentPath);

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
	}

	[Theory]
	[InlineData("MissingPropertiesDir", "properties")]
	[InlineData("MissingColumnsDir", "columns")]
	[InlineData("MissingActionsDir", "actions")]
	public async Task MissingRequiredDirectory_HasError(string caseName, string directoryKeyword)
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync(caseName);

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains(directoryKeyword, StringComparison.OrdinalIgnoreCase) &&
			e.Contains("not found", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task EmptyPropertiesDirectory_HasError()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("EmptyPropertiesDir");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("No YAML files found", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("properties", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task EmptyActionsDirectory_HasError()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("EmptyActionsDir");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("No YAML files found", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("actions", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task MalformedYaml_HasError()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("MalformedYaml");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("Failed to parse", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("parse", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task EmptyYamlFile_HasWarning()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("EmptyYamlFile");

		context.HasWarnings.Should().BeTrue();
		context.Warnings.Should().Contain(w =>
			w.Contains("Empty", StringComparison.OrdinalIgnoreCase) ||
			w.Contains("invalid", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ConfigurationIsNullOnError()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		var context = await ConfigFacade.LoadAsync(nonExistentPath);

		context.HasErrors.Should().BeTrue();
		context.Configuration.Should().BeNull(
			"Configuration should be null when errors occur during loading");
	}

	[Fact]
	public async Task MultipleErrors_AllReported()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("MultipleErrors");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().HaveCountGreaterThanOrEqualTo(2,
			"both invalid system_type and min > max errors should be reported");
	}
}
