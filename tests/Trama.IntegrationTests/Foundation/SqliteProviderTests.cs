using Microsoft.EntityFrameworkCore;
using Trama.Infrastructure.Persistence;

namespace Trama.IntegrationTests.Foundation;

public sealed class SqliteProviderTests
{
    [Fact]
    public async Task SqliteProvider_CanWriteAndReadWithoutCreatingAPersonalDatabase()
    {
        var options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite("Data Source=:memory:").Options;
        await using var context = new TramaDbContext(options);
        await context.Database.OpenConnectionAsync();

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "CREATE TABLE FoundationProbe (Value INTEGER NOT NULL);";
        await command.ExecuteNonQueryAsync();
        command.CommandText = "INSERT INTO FoundationProbe (Value) VALUES ($value);";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$value";
        parameter.Value = 42;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        command.CommandText = "SELECT Value FROM FoundationProbe;";

        Assert.Equal(42L, await command.ExecuteScalarAsync());
    }

    [Theory]
    [InlineData()]
    [InlineData("--database", "relative.db")]
    public void DesignTimeFactory_RejectsMissingOrRelativeDatabasePaths(params string[] args)
    {
        Assert.Throws<ArgumentException>(() => new TramaDbContextFactory().CreateDbContext(args));
    }
}
