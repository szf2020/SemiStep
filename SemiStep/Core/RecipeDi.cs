using Core.Analysis;
using Core.Formulas;
using Core.Services;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

namespace Core;

public static class RecipeDi
{
	public static IServiceCollection AddRecipe(this IServiceCollection services, ILogger? logger = null)
	{
		if (logger is not null)
		{
			services.AddSingleton(logger);
		}

		// Core services
		services.AddSingleton<StepFactory>();
		services.AddSingleton<RecipeMutator>();
		services.AddSingleton<PropertyValidator>();

		// Analysis
		services.AddSingleton<LoopParser>();
		services.AddSingleton<TimingCalculator>();
		services.AddSingleton<RecipeAnalyzer>();

		// Formulas (placeholder - empty formulas dictionary for now)
		services.AddSingleton<IReadOnlyDictionary<short, CompiledFormula>>(
			_ => new Dictionary<short, CompiledFormula>());
		services.AddSingleton<IFormulaEngine, FormulaEngine>();
		services.AddSingleton<IStepVariableAdapter, StepVariableAdapter>();
		services.AddSingleton<FormulaApplicationCoordinator>();

		// Facade
		services.AddSingleton<CoreFacade>();

		return services;
	}
}
