using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EcoPlay.Gamification.Api.Health;

public sealed class JavaApiHealthCheck(IHttpClientFactory clients, IConfiguration config) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var url = config["JavaApi:HealthUrl"];
        if (string.IsNullOrWhiteSpace(url)) return HealthCheckResult.Healthy("JavaApi:HealthUrl não configurado; verificação externa desativada.");
        try
        {
            using var response = await clients.CreateClient("java-api").GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("API Java respondeu com sucesso.")
                : HealthCheckResult.Unhealthy($"API Java respondeu HTTP {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Não foi possível alcançar a API Java.", ex);
        }
    }
}
