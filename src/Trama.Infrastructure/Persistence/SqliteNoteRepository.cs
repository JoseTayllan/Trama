using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Trama.Application.Knowledge;

namespace Trama.Infrastructure.Persistence;

public sealed class SqliteNoteRepository : INoteRepository, INoteReader
{
    private readonly TramaDbContext _context;

    public SqliteNoteRepository(TramaDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task AddAsync(NoteSnapshot note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);
        cancellationToken.ThrowIfCancellationRequested();

        var record = new NoteRecord { Id = note.Id, Title = note.Title, Body = note.Body };
        _context.Set<NoteRecord>().Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new NotePersistenceException("The note could not be saved.", exception);
        }
    }

    public async Task<NoteSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (await LibraryTrashVisibility.IsHiddenAsync(_context, LibraryEntryKind.Page, id, cancellationToken)) return null;
            return await _context.Set<NoteRecord>()
                .AsNoTracking()
                .Where(record => record.Id == id)
                .Select(record => new NoteSnapshot(record.Id, record.Title, record.Body))
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (DbException exception)
        {
            throw new NotePersistenceException("The note could not be retrieved.", exception);
        }
    }
}
