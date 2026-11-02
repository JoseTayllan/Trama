using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteNotebookRepository : INotebookRepository
{
    private readonly TramaDbContext _context;

    public SqliteNotebookRepository(TramaDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task SaveAsync(NotebookSnapshot notebook, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notebook);
        ArgumentNullException.ThrowIfNull(notebook.Blocks);
        cancellationToken.ThrowIfCancellationRequested();
        if (notebook.Id == Guid.Empty)
        {
            throw new ArgumentException("A notebook must have an identity.", nameof(notebook));
        }

        var draft = new NotebookDraft();
        foreach (var block in notebook.Blocks)
        {
            draft.Insert(draft.Blocks.Count, block);
        }

        var records = draft.Blocks.Select((block, position) => ToRecord(notebook.Id, position, block)).ToArray();
        try
        {
            await using var transaction = _context.Database.CurrentTransaction is null
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;
            await LibraryTrashVisibility.RequireActiveAsync(_context, LibraryEntryKind.Page, notebook.Id, cancellationToken);
            var page = await _context.Set<NotebookPageRecord>().Include(record => record.Blocks)
                .SingleOrDefaultAsync(record => record.Id == notebook.Id, cancellationToken);
            if (page is null)
            {
                page = new NotebookPageRecord { Id = notebook.Id };
                _context.Set<NotebookPageRecord>().Add(page);
            }
            else
            {
                if (page.FormatVersion is not (1 or 2 or 3 or 4))
                {
                    throw new NotePersistenceException("The notebook format is not supported.");
                }

                _context.Set<NotebookBlockRecord>().RemoveRange(page.Blocks);
                page.Blocks.Clear();
            }

            page.Title = notebook.Title;
            page.FormatVersion = ContentVersion(notebook.Blocks);
            await _context.SaveChangesAsync(cancellationToken);
            page.Blocks.AddRange(records);
            await _context.SaveChangesAsync(cancellationToken);
            var styles = notebook.Blocks.OfType<NotebookTextBlock>().Where(block => block.Styles.Count > 0 || block.Lists.Count > 0)
                .Select(block => new NotebookTextStyleRecord { PageId = page.Id, BlockId = block.Id, Styles = SerializeMetadata(block) }).ToArray();
            if (styles.Length > 0)
            {
                _context.Set<NotebookTextStyleRecord>().AddRange(styles);
                await _context.SaveChangesAsync(cancellationToken);
            }
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new NotePersistenceException("The notebook could not be saved.", exception);
        }
    }

    public Task<NotebookSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReadAsync(id, includeTrashed: false, cancellationToken);

    internal Task<NotebookSnapshot?> ReadStoredForBackupAsync(Guid id, CancellationToken cancellationToken) =>
        ReadAsync(id, includeTrashed: true, cancellationToken);

    private async Task<NotebookSnapshot?> ReadAsync(Guid id, bool includeTrashed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (!includeTrashed && await LibraryTrashVisibility.IsHiddenAsync(_context, LibraryEntryKind.Page, id, cancellationToken)) return null;
            var page = await _context.Set<NotebookPageRecord>().AsNoTracking().Include(record => record.Blocks)
                .SingleOrDefaultAsync(record => record.Id == id, cancellationToken);
            if (page is not null)
            {
                if (page.FormatVersion is not (1 or 2 or 3 or 4))
                {
                    throw new NotePersistenceException("The notebook format is not supported.");
                }

                // Plain pages also need metadata checks when the table exists;
                // older schemas legitimately have no text-style table yet.
                var canReadStyles = page.FormatVersion >= 2 || await HasTextStylesTableAsync(cancellationToken);
                var styles = canReadStyles
                    ? await _context.Set<NotebookTextStyleRecord>().AsNoTracking().Where(record => record.PageId == page.Id).ToDictionaryAsync(record => record.BlockId, record => record.Styles, cancellationToken)
                    : new Dictionary<Guid, string>();
                if ((page.FormatVersion == 1 && styles.Count != 0) ||
                    (page.FormatVersion >= 2 && styles.Count == 0) ||
                    styles.Keys.Any(id => !page.Blocks.Any(block => block.Id == id && block.Kind == "text")))
                    throw new NotePersistenceException("The page contains missing or misplaced text styles.");
                var blocks = page.Blocks.OrderBy(block => block.Position).Select(record => record.Kind == "text"
                    ? ReadText(record, styles.GetValueOrDefault(record.Id, "[]")) : ToBlock(record)).ToArray();
                if (ContentVersion(blocks) != page.FormatVersion)
                    throw new NotePersistenceException("The page format does not match its text attributes.");
                return new NotebookSnapshot(page.Id, page.Title, blocks);
            }

            var legacy = await new SqliteNoteRepository(_context).GetByIdAsync(id, cancellationToken);
            return legacy is null ? null : new NotebookSnapshot(legacy.Id, legacy.Title,
                [new NotebookTextBlock(legacy.Id, legacy.Body)]);
        }
        catch (DbException exception)
        {
            throw new NotePersistenceException("The notebook could not be retrieved.", exception);
        }
    }

    private Task<bool> HasTextStylesTableAsync(CancellationToken cancellationToken) =>
        _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'NotebookTextStyles'")
            .AnyAsync(count => count > 0, cancellationToken);

    private static int ContentVersion(IEnumerable<NotebookBlock> blocks)
    {
        if (blocks.OfType<NotebookTextBlock>().Any(block => block.Lists.Count > 0)) return 4;
        var version = 1;
        foreach (var span in blocks.OfType<NotebookTextBlock>().SelectMany(block => block.Styles))
        {
            if (span.FontSize is not null || span.Foreground is not null || span.Highlight is not null) return 3;
            version = 2;
        }
        return version;
    }

    private static NotebookBlockRecord ToRecord(Guid pageId, int position, NotebookBlock block)
    {
        var record = new NotebookBlockRecord { PageId = pageId, Id = block.Id, Position = position };
        switch (block)
        {
            case NotebookTextBlock text:
                _ = new InlineTextDocument(text.Text, text.Styles);
                record.Kind = "text";
                record.Content = text.Text;
                break;
            case NotebookCodeBlock code:
                record.Kind = "code";
                record.Content = code.Code;
                record.Language = code.Language;
                break;
            case NotebookImageBlock image:
                record.Kind = "image";
                record.Content = image.Caption;
                record.AttachmentId = image.AttachmentId;
                break;
            default:
                throw new ArgumentException("The block type is not supported.", nameof(block));
        }

        return record;
    }

    private static NotebookBlock ToBlock(NotebookBlockRecord record) => record.Kind switch
    {
        "text" => ReadText(record, "[]"),
        "code" => new NotebookCodeBlock(record.Id, record.Content, record.Language),
        "image" when record.AttachmentId is { } id && id != Guid.Empty =>
            new NotebookImageBlock(record.Id, id, record.Content),
        _ => throw new NotePersistenceException("The notebook contains an unsupported block.")
    };

    private static NotebookTextBlock ReadText(NotebookBlockRecord record, string serializedStyles)
    {
        try
        {
            using var json = JsonDocument.Parse(serializedStyles);
            var envelope = json.RootElement.ValueKind == JsonValueKind.Object;
            var metadata = envelope ? JsonSerializer.Deserialize<TextMetadata>(serializedStyles)
                ?? throw new ArgumentException("Missing paragraph metadata.") : null;
            var styles = envelope ? metadata!.Styles : JsonSerializer.Deserialize<TextStyleSpan[]>(serializedStyles);
            if (styles is null || (envelope && (metadata!.Lists is null || metadata.Lists.Count == 0)))
                throw new ArgumentException("Missing text metadata.");
            var document = new InlineTextDocument(record.Content, styles);
            var lists = new ParagraphListDocument(record.Content, metadata?.Lists);
            return new NotebookTextBlock(record.Id, record.Content) { Styles = document.Styles, Lists = lists.Lists };
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            throw new NotePersistenceException("The page contains invalid text styles.", exception);
        }
    }

    // Keep versions 1/2/3 in their original array representation. Only content 4
    // uses the envelope in the existing, transactionally managed metadata row.
    private static string SerializeMetadata(NotebookTextBlock block) => block.Lists.Count == 0
        ? JsonSerializer.Serialize(block.Styles)
        : JsonSerializer.Serialize(new TextMetadata(block.Styles, block.Lists));

    private sealed record TextMetadata(IReadOnlyList<TextStyleSpan> Styles, IReadOnlyList<ParagraphListItem> Lists);
}
