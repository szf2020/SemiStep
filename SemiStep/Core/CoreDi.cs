using Core.Analysis;
using Core.Facade;
using Core.Formulas;
using Core.Services;

using Microsoft.Extensions.DependencyInjection;

using TypesShared.Core;
using TypesShared.Domain;

namespace Core;

public static class CoreDi
{
	public static IServiceCollection AddRecipe(this IServiceCollection services)
	{
		services.AddSingleton<CoreConfig>();

		services.AddSingleton<RecipeAnalyzer>();
		services.AddSingleton<LoopParser>();

		services.AddSingleton<IReadOnlyDictionary<int, CompiledFormula>>(_ => new Dictionary<int, CompiledFormula>());
		services.AddSingleton<FormulaEngine>();
		services.AddSingleton<FormulaApplicationCoordinator>();

		services.AddSingleton<IPropertyParser, PropertyParser>();
		services.AddSingleton<ICoreService, CoreFacade>();
		return services;
	}
}
