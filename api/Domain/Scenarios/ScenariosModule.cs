using BabyRadnik.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace BabyRadnik.Domain.Scenarios;

public static class ScenariosModule
{
    public static IReadOnlyList<IScenario> BuiltIn() =>
    [
        new OdchodzeZPracyScenario(),
        new UmowaNieZostaniePrzedluzonaScenario(),
        new PrzechodzeNaDzialalnoscScenario(),
        new DrugiRodzicBierzeScenario(),
        new PracaNaCzescEtatuScenario(),
        new WariantWyplatyScenario(),
        new SplacamZadluzenieScenario(),
    ];

    public static IServiceCollection AddScenarios(this IServiceCollection services)
    {
        foreach (var scenario in BuiltIn())
        {
            services.AddSingleton(scenario);
        }

        services.AddScoped<IScenarioComparer>(sp => new ScenarioComparer(
            sp.GetRequiredService<IAssessmentEngine>(),
            sp.GetRequiredService<IParameterProvider>(),
            sp.GetServices<IScenario>()));
        return services;
    }
}
