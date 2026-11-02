namespace Trama.Desktop;

public static class DataDirectoryOptions
{
    public static string Resolve(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count == 0) return Infrastructure.Persistence.LocalDatabase.DefaultDirectory;
        if (arguments.Count != 2 || arguments[0] != "--data-directory" || string.IsNullOrWhiteSpace(arguments[1]) ||
            !Path.IsPathFullyQualified(arguments[1]))
            throw new ArgumentException("Use --data-directory followed by an absolute profile path.", nameof(arguments));
        return Path.GetFullPath(arguments[1]);
    }
}
