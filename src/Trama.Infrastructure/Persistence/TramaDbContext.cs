using Microsoft.EntityFrameworkCore;

namespace Trama.Infrastructure.Persistence;

public sealed class TramaDbContext(DbContextOptions<TramaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LibraryTrashRecord>(trash =>
        {
            trash.ToTable("LibraryTrash");
            trash.HasKey(record => new { record.Kind, record.EntryId });
            trash.Property(record => record.EntryId).HasColumnType("TEXT").ValueGeneratedNever();
            trash.Property(record => record.BatchId).HasColumnType("TEXT").IsRequired();
            trash.Property(record => record.Title).HasColumnType("TEXT").IsRequired();
            trash.HasIndex(record => record.BatchId);
        });

        modelBuilder.Entity<NotebookTextStyleRecord>(style =>
        {
            style.ToTable("NotebookTextStyles");
            style.HasKey(record => new { record.PageId, record.BlockId });
            style.Property(record => record.PageId).HasColumnType("TEXT").ValueGeneratedNever();
            style.Property(record => record.BlockId).HasColumnType("TEXT").ValueGeneratedNever();
            style.Property(record => record.Styles).HasColumnType("TEXT").IsRequired();
            style.HasOne<NotebookBlockRecord>().WithMany().HasForeignKey(record => new { record.PageId, record.BlockId }).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PageTagRecord>(tag =>
        {
            tag.ToTable("PageTags");
            tag.HasKey(record => new { record.PageId, record.Name });
            tag.Property(record => record.PageId).HasColumnType("TEXT").ValueGeneratedNever();
            tag.Property(record => record.Name).HasColumnType("TEXT").IsRequired();
            tag.HasIndex(record => record.Name);
        });

        modelBuilder.Entity<NoteRecord>(note =>
        {
            note.ToTable("Notes");
            note.HasKey(record => record.Id);
            note.Property(record => record.Id).HasColumnType("TEXT").ValueGeneratedNever().IsRequired();
            note.Property(record => record.Title).HasColumnType("TEXT").IsRequired();
            note.Property(record => record.Body).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<NotebookPageRecord>(page =>
        {
            page.ToTable("NotebookPages");
            page.HasKey(record => record.Id);
            page.Property(record => record.Id).HasColumnType("TEXT").ValueGeneratedNever().IsRequired();
            page.Property(record => record.Title).HasColumnType("TEXT").IsRequired();
            page.Property(record => record.FormatVersion).IsRequired();
            page.HasMany(record => record.Blocks).WithOne().HasForeignKey(record => record.PageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotebookBlockRecord>(block =>
        {
            block.ToTable("NotebookBlocks");
            block.HasKey(record => new { record.PageId, record.Id });
            block.Property(record => record.PageId).HasColumnType("TEXT").ValueGeneratedNever();
            block.Property(record => record.Id).HasColumnType("TEXT").ValueGeneratedNever();
            block.Property(record => record.Kind).HasColumnType("TEXT").IsRequired();
            block.Property(record => record.Content).HasColumnType("TEXT").IsRequired();
            block.Property(record => record.Language).HasColumnType("TEXT").IsRequired();
            block.Property(record => record.AttachmentId).HasColumnType("TEXT");
            block.HasIndex(record => new { record.PageId, record.Position }).IsUnique();
        });

        modelBuilder.Entity<KnowledgeSpaceRecord>(space =>
        {
            space.ToTable("KnowledgeSpaces");
            space.HasKey(record => record.Id);
            space.Property(record => record.Id).HasColumnType("TEXT").ValueGeneratedNever();
            space.Property(record => record.Title).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<NotebookRecord>(notebook =>
        {
            notebook.ToTable("Notebooks");
            notebook.HasKey(record => record.Id);
            notebook.Property(record => record.Id).HasColumnType("TEXT").ValueGeneratedNever();
            notebook.Property(record => record.SpaceId).HasColumnType("TEXT").IsRequired();
            notebook.Property(record => record.Title).HasColumnType("TEXT").IsRequired();
            notebook.HasOne<KnowledgeSpaceRecord>().WithMany().HasForeignKey(record => record.SpaceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PageLocationRecord>(location =>
        {
            location.ToTable("PageLocations");
            location.HasKey(record => record.PageId);
            location.Property(record => record.PageId).HasColumnType("TEXT").ValueGeneratedNever();
            location.Property(record => record.NotebookId).HasColumnType("TEXT").IsRequired();
            location.HasOne<NotebookRecord>().WithMany().HasForeignKey(record => record.NotebookId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
