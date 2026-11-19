using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoPlay.Gamification.Api.Persistence;

public sealed class GamificationDbContextFactory : IDesignTimeDbContextFactory<GamificationDbContext>
{
    public GamificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Gamification");
        var builder = new DbContextOptionsBuilder<GamificationDbContext>();

        if (string.IsNullOrWhiteSpace(connectionString))
            builder.UseOracle(oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));
        else
            builder.UseOracle(connectionString, oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));

        return new GamificationDbContext(builder.Options);
    }
}
