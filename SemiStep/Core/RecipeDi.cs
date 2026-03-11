using Core.Analysis;
using Core.Facade;
using Core.Formulas;
using Core.Services;

using Microsoft.Extensions.DependencyInjection;

using Shared.ServiceContracts;

namespace Core;

public static class RecipeDi
{
	public static IServiceCollection AddRecipe(this IServiceCollection services)
	{
		services.AddSingleton<CoreConfig>();

		services.AddSingleton<TimingCalculator>();
		services.AddSingleton<RecipeAnalyzer>();
		services.AddSingleton<LoopParser>();

		services.AddSingleton<IReadOnlyDictionary<int, CompiledFormula>>(_ => new Dictionary<int, CompiledFormula>());
		services.AddSingleton<FormulaEngine>();
		services.AddSingleton<StepVariableAdapter>();
		services.AddSingleton<FormulaApplicationCoordinator>();

		services.AddSingleton<ICoreService, CoreFacade>();

		services.AddSingleton<StepFactory>();
		services.AddSingleton<PropertyValidator>();

		return services;
	}
}
