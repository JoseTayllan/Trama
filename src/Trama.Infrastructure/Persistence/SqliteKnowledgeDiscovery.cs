using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteKnowledgeDiscovery : IKnowledgeDiscovery
{
    private readonly DbContextOptions<TramaDbContext> _options;

    public SqliteKnowledgeDiscovery(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!Path.IsPathFullyQualified(databasePath))
            throw new ArgumentException("An absolute database path is required.", nameof(databasePath));
        var connection = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false };
        _options = new DbContextOptionsBuilder<TramaDbContext>().UseSqlite(connection.ToString()).Options;
    }

    public async Task<IReadOnlyList<PageSearchResult>> SearchAsync(string query, string? tag = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);
        var text = Fold(query.Trim());
        var filter = string.IsNullOrWhiteSpace(tag) ? null : PageTags.Normalize(tag);
        await using var context = new TramaDbContext(_options);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var structured = await context.Set<NotebookPageRecord>().AsNoTracking().Include(page => page.Blocks).ToArrayAsync(cancellationToken);
            var legacy = await context.Set<NoteRecord>().AsNoTracking()
                .Where(note => !context.Set<NotebookPageRecord>().Any(page => page.Id == note.Id)).ToArrayAsync(cancellationToken);
            var locations = await context.Set<PageLocationRecord>().AsNoTracking().ToDictionaryAsync(item => item.PageId, cancellationToken);
            var notebooks = await context.Set<NotebookRecord>().AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
            var spaces = await context.Set<KnowledgeSpaceRecord>().AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);
            var tags = await context.Set<PageTagRecord>().AsNoTracking().ToArrayAsync(cancellationToken);
            var tagged = filter is null ? null : tags.Where(item => PageTags.Normalize(item.Name) == filter).Select(item => item.PageId).ToHashSet();
            var matchingTags = tags.Where(item => Fold(item.Name).Contains(text, StringComparison.Ordinal)).Select(item => item.PageId).ToHashSet();
            var hidden = await LibraryTrashVisibility.HasTableAsync(context, cancellationToken)
                ? (await context.Set<LibraryTrashRecord>().AsNoTracking().Where(item => item.Kind == LibraryEntryKind.Page).Select(item => item.EntryId).ToArrayAsync(cancellationToken)).ToHashSet()
                : [];
            var results = new List<PageSearchResult>();
            foreach (var page in structured)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (hidden.Contains(page.Id)) continue;
                if (tagged is not null && !tagged.Contains(page.Id)) continue;
                if (text.Length != 0 && !matchingTags.Contains(page.Id) && !Fold(page.Title).Contains(text, StringComparison.Ordinal) &&
                    !page.Blocks.Any(block => Fold(block.Content).Contains(text, StringComparison.Ordinal))) continue;
                Add(page.Id, page.Title);
            }
            foreach (var note in legacy)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (hidden.Contains(note.Id)) continue;
                if (tagged is not null && !tagged.Contains(note.Id)) continue;
                if (text.Length != 0 && !matchingTags.Contains(note.Id) && !Fold(note.Title).Contains(text, StringComparison.Ordinal) &&
                    !Fold(note.Body).Contains(text, StringComparison.Ordinal)) continue;
                Add(note.Id, note.Title);
            }

            return results.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.PageId).ToArray();

            void Add(Guid id, string title)
            {
                if (locations.TryGetValue(id, out var location) && notebooks.TryGetValue(location.NotebookId, out var book) &&
                    spaces.TryGetValue(book.SpaceId, out var space))
                    results.Add(new PageSearchResult(id, title, space.Id, space.Title, book.Id, book.Title));
                else results.Add(new PageSearchResult(id, title));
            }
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("Pages could not be searched.", exception);
        }
    }

    public async Task<IReadOnlyList<string>> GetPageTagsAsync(Guid pageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var context = new TramaDbContext(_options);
        try
        {
            await RequirePageAsync(context, pageId, cancellationToken);
            var tags = await context.Set<PageTagRecord>().AsNoTracking().Where(item => item.PageId == pageId)
                .Select(item => item.Name).ToArrayAsync(cancellationToken);
            return tags.Order(StringComparer.Ordinal).ToArray();
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("Page tags could not be retrieved.", exception);
        }
    }

    public async Task SetPageTagsAsync(Guid pageId, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = PageTags.Normalize(tags);
        await using var context = new TramaDbContext(_options);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await RequirePageAsync(context, pageId, cancellationToken);
            await context.Set<PageTagRecord>().Where(item => item.PageId == pageId).ExecuteDeleteAsync(cancellationToken);
            context.Set<PageTagRecord>().AddRange(normalized.Select(name => new PageTagRecord { PageId = pageId, Name = name }));
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            throw new NotePersistenceException("Page tags could not be saved.", exception);
        }
    }

    private static async Task RequirePageAsync(TramaDbContext context, Guid id, CancellationToken cancellationToken)
    {
        await LibraryTrashVisibility.RequireActiveAsync(context, LibraryEntryKind.Page, id, cancellationToken);
        if (!await context.Set<NotebookPageRecord>().AnyAsync(page => page.Id == id, cancellationToken) &&
            !await context.Set<NoteRecord>().AnyAsync(note => note.Id == id, cancellationToken))
            throw new ArgumentException("The page does not exist.", nameof(id));
    }

    private static string Fold(string value) => new string(value.Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray()).ToUpperInvariant();
}
