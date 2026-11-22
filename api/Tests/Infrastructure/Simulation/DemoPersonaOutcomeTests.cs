using System.Net.Http.Json;
using System.Text.Json;
using BabyRadnik.Api;
using BabyRadnik.Domain.Contracts;
using BabyRadnik.Infrastructure.Parameters;
using BabyRadnik.Infrastructure.Simulation;
using BabyRadnik.Tests.Domain.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BabyRadnik.Tests.Infrastructure.Simulation;

/// <summary>
/// Osoby demo przechodzą główny przebieg (logowanie → fakty z integracji + odpowiedzi z ekranu 1 → ocena) i dają to, co opisuje ich `opis`.
/// </summary>
public sealed class DemoPersonaOutcomeTests(AssessmentFixture fixture) : IClassFixture<AssessmentFixture>
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;

    private async Task<AssessmentResult> FlowAsync(string personaId)
    {
        using var client = fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/sim/login", new { personaId }, Ct);
        login.EnsureSuccessStatusCode();
        using var session = JsonDocument.Parse(await login.Content.ReadAsStringAsync(Ct));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sim/me/facts");
        request.Headers.Add(SimulationModule.SessionHeader, session.RootElement.GetProperty("sessionId").GetString());
        var response = await client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        var fromSim = (await response.Content.ReadFromJsonAsync<FactSet>(ApiJson.Options, Ct))!;

        // Odpowiedzi z ekranu 1, rola i pytania z „Sprawdź dane” (umowa na czas określony: nie; jedno dziecko) — reszta tylko z integracji.
        var persona = CatalogKit.LoadPersona(personaId);
        var facts = fromSim with
        {
            Sprawa = fromSim.Sprawa with
            {
                ZdarzenieRodzaj = persona.Sprawa.ZdarzenieRodzaj,
                ZdarzenieData = persona.Sprawa.ZdarzenieData,
                ZdarzenieDlaKogo = persona.Sprawa.ZdarzenieDlaKogo ?? CatalogKit.F(CalculationScope.Ja),
                DzieckoLiczba = fromSim.Sprawa.DzieckoLiczba ?? CatalogKit.F(1),
            },
            Wnioskodawca = fromSim.Wnioskodawca with { OsobaRola = persona.Wnioskodawca.OsobaRola, UmowaNaCzasOkreslony = CatalogKit.F(false) },
        };
        if (facts.DrugiRodzic is not null)
        {
            facts = facts with { DrugiRodzic = facts.DrugiRodzic with { OsobaRola = CatalogKit.F(ParentRole.Matka) } };
        }

        var assessed = await client.PostAsJsonAsync("/api/assessment", new AssessmentRequest(facts, CatalogKit.AsOf), ApiJson.Options, Ct);
        assessed.EnsureSuccessStatusCode();
        return (await assessed.Content.ReadFromJsonAsync<AssessmentResult>(ApiJson.Options, Ct))!;
    }

    private async Task<AssessmentResult> LoginOnlyAsync(string personaId)
    {
        using var client = fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/sim/login", new { personaId }, Ct);
        login.EnsureSuccessStatusCode();
        using var session = JsonDocument.Parse(await login.Content.ReadAsStringAsync(Ct));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sim/me/facts");
        request.Headers.Add(SimulationModule.SessionHeader, session.RootElement.GetProperty("sessionId").GetString());
        var response = await client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        var facts = (await response.Content.ReadFromJsonAsync<FactSet>(ApiJson.Options, Ct))!;
        var assessed = await client.PostAsJsonAsync("/api/assessment", new AssessmentRequest(facts, CatalogKit.AsOf), ApiJson.Options, Ct);
        assessed.EnsureSuccessStatusCode();
        return (await assessed.Content.ReadFromJsonAsync<AssessmentResult>(ApiJson.Options, Ct))!;
    }

    [Theory]
    [InlineData("anna-przyklad")]
    [InlineData("klara-demo")]
    [InlineData("halina-demo")]
    [InlineData("monika-demo")]
    public async Task Logged_in_employee_sees_maternity_amount_without_answering_questions_about_the_event(string personaId)
    {
        var result = await LoginOnlyAsync(personaId);

        var maternity = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.Przysluguje, maternity.Status);
        Assert.NotNull(maternity.Amount);
        // Zostaje co najwyżej pytanie o umowę terminową — tego rejestr ZUS nie zna, a opis osoby nie rozstrzyga.
        Assert.All(result.MissingFacts, m => Assert.Equal(FactKeys.UmowaNaCzasOkreslony, m.Fact));
    }

    [Theory]
    [InlineData("edyta-demo")]
    [InlineData("olga-demo")]
    [InlineData("tadeusz-demo")]
    [InlineData("weronika-demo")]
    public async Task Logged_in_demo_person_is_not_asked_about_the_event_role_or_number_of_children(string personaId)
    {
        var result = await LoginOnlyAsync(personaId);

        Assert.DoesNotContain(result.MissingFacts, m => m.Fact is FactKeys.ZdarzenieRodzaj or FactKeys.ZdarzenieData
            or FactKeys.ZdarzenieDlaKogo or FactKeys.OsobaRola or FactKeys.DzieckoLiczba);
    }

    private static BenefitResult Get(AssessmentResult r, BenefitId id)
    {
        var benefit = CatalogKit.Get(r, id);
        Assert.True(benefit.Status != BenefitStatus.BrakujeDanych, $"{id}: brakuje {string.Join(", ", r.MissingFacts.Select(m => $"{m.Person}/{m.Fact}"))}");
        return benefit;
    }

    [Theory]
    [InlineData("monika-demo", "zus")]
    [InlineData("klara-demo", "płatnik_składek")]
    [InlineData("halina-demo", "zus")]
    public async Task Employees_get_maternity_amounts_from_zus_contribution_bases(string personaId, string payer)
    {
        var result = await FlowAsync(personaId);

        foreach (var id in new[] { BenefitId.Macierzynski, BenefitId.Rodzicielski })
        {
            var benefit = Get(result, id);
            Assert.Equal(BenefitStatus.Przysluguje, benefit.Status);
            Assert.NotNull(benefit.Amount);
            Assert.Equal(payer, JsonSerializer.Serialize(benefit.Payer, ApiJson.Options).Trim('"'));
        }

        Assert.DoesNotContain(result.MissingFacts, m => m.Fact is FactKeys.UbezpieczeniePodstawySkladek or FactKeys.PodstawaUstalonaPrzezPlatnika);
    }

    [Fact]
    public async Task Edyta_is_refused_because_of_contribution_debt_known_today_not_in_the_future()
    {
        var result = await FlowAsync("edyta-demo");

        var maternity = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, maternity.Status);
        Assert.Contains("120,00", maternity.Reason);
        Assert.Contains(result.Risks, r => r.Id == "zadluzenie_skladkowe");
    }

    [Fact]
    public async Task Olga_without_sickness_insurance_is_refused_with_reason()
    {
        var result = await FlowAsync("olga-demo");

        var maternity = Get(result, BenefitId.Macierzynski);
        Assert.Equal(BenefitStatus.NiePrzysluguje, maternity.Status);
        Assert.False(string.IsNullOrWhiteSpace(maternity.Reason));
    }

    [Fact]
    public async Task Tadeusz_as_insured_father_gets_parental_and_paternity()
    {
        var result = await FlowAsync("tadeusz-demo");

        Assert.Equal(BenefitStatus.Przysluguje, Get(result, BenefitId.Rodzicielski).Status);
        Assert.Equal(BenefitStatus.Przysluguje, Get(result, BenefitId.Ojcowski).Status);
    }

    [Fact]
    public async Task Weronika_is_refused_sickness_benefit_for_waiting_period_but_gets_maternity()
    {
        var result = await FlowAsync("weronika-demo");

        var sickness = Get(result, BenefitId.ChorobowyWCiazy);
        Assert.Equal(BenefitStatus.NiePrzysluguje, sickness.Status);
        Assert.Contains("90 dni", sickness.Reason);
        Assert.Equal(BenefitStatus.Przysluguje, Get(result, BenefitId.Macierzynski).Status);
    }
}
