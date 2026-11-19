using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using EcoPlay.Gamification.Api.Domain;
using EcoPlay.Gamification.Api.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcoPlay.Gamification.Tests;

public sealed class GamificationApiTests : IClassFixture<EcoPlayFactory>
{
    private readonly EcoPlayFactory factory;
    public GamificationApiTests(EcoPlayFactory factory) => this.factory = factory;

    [Fact]
    public async Task Progresso_sem_token_retorna_401()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/progresso");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Credito_repetido_com_mesma_chave_nao_duplica_xp()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Role", "SISTEMA");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "entrega-integration-1");
        var request = new CreditoXpRequest("cidadao-1", "ENTREGA", "entrega-1", 3.2m);

        var first = await client.PostAsJsonAsync("/api/xp/creditos", request);
        var replay = await client.PostAsJsonAsync("/api/xp/creditos", request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GamificationDbContext>();
        Assert.Equal(32, await db.LancamentosXp.Where(x => x.UsuarioId == "cidadao-1").SumAsync(x => x.Quantidade));
    }

    [Fact]
    public async Task Reutilizar_chave_com_conteudo_diferente_retorna_409()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Role", "SISTEMA");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "entrega-conflict-1");
        var first = await client.PostAsJsonAsync("/api/xp/creditos", new CreditoXpRequest("cidadao-1", "ENTREGA", "entrega-a", 2m));
        var second = await client.PostAsJsonAsync("/api/xp/creditos", new CreditoXpRequest("cidadao-1", "ENTREGA", "entrega-b", 2m));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Quiz_retorna_429_apos_dez_chamadas_no_minuto()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        HttpResponseMessage? response = null;
        for (var i = 0; i < 11; i++)
            response = await client.PostAsJsonAsync("/api/quiz/respostas", new RespostaQuizRequest(999, 0));
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}

public sealed class EcoPlayFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection anchor = new("Data Source=ecoplay-tests;Mode=Memory;Cache=Shared");

    public EcoPlayFactory() => anchor.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<GamificationDbContext>>();
            services.RemoveAll<GamificationDbContext>();
            services.AddDbContext<GamificationDbContext>(options => options.UseSqlite("Data Source=ecoplay-tests;Mode=Memory;Cache=Shared"));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<GamificationDbContext>().Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) anchor.Dispose();
    }
}

public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Auth")) return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "CIDADAO";
        var claims = new[] { new Claim("sub", "cidadao-1"), new Claim("papel", role) };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
