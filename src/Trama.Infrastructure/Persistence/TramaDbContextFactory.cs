using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Trama.Infrastructure.Persistence;

public sealed class TramaDbContextFactory : IDesignTimeDbContextFactory<TramaDbContext>
{
    public TramaDbContext CreateDbContext(string[] args)
    {
        if (args.Length != 2 || args[0] != "--database" || !Path.IsPathFullyQualified(args[1]))
        {
            throw new ArgumentException("Specify --database followed by an absolute development database path.");
        }

        var connectionString = new SqliteConnectionStringBuilder { DataSource = args[1] }.ToString();
        var options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connectionString).Options;
        return new TramaDbContext(options);
    }
}
