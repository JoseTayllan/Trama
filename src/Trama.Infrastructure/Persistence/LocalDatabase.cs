using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Trama.Infrastructure.Persistence;

public static class LocalDatabase
{
    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Trama");

    public static async Task<string> InitializeAsync(string dataDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory) || !Path.IsPathFullyQualified(dataDirectory))
        {
            throw new ArgumentException("An absolute data directory is required.", nameof(dataDirectory));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetFullPath(dataDirectory);
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (IOException exception)
        {
            // Keep the public I/O failure contract stable across Windows and Linux.
            throw new IOException("The local data directory could not be created.", exception);
        }
        var databasePath = Path.Combine(directory, "trama.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();
        var options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connectionString).Options;
        await using var context = new TramaDbContext(options);
        await context.Database.MigrateAsync(cancellationToken);
        return databasePath;
    }
}
