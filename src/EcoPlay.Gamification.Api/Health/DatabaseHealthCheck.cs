using EcoPlay.Gamification.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EcoPlay.Gamification.Api.Health;

public sealed class DatabaseHealthCheck(GamificationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Oracle acessível.")
            : HealthCheckResult.Unhealthy("Oracle indisponível.");
}
