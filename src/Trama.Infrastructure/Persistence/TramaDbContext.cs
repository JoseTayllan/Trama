using Microsoft.EntityFrameworkCore;

namespace Trama.Infrastructure.Persistence;

public sealed class TramaDbContext(DbContextOptions<TramaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AgendaWeekReflectionRecord>(reflection =>
        {
            reflection.ToTable("AgendaWeekReflections");
            reflection.HasKey(record => record.WeekStart);
            reflection.Property(record => record.WeekStart).HasColumnType("TEXT").ValueGeneratedNever();
            reflection.Property(record => record.WorkedWell).HasColumnType("TEXT").IsRequired();
            reflection.Property(record => record.Adjustments).HasColumnType("TEXT").IsRequired();
            reflection.Property(record => record.UpdatedAt).HasColumnType("TEXT").IsRequired();
            reflection.HasOne<AgendaWeekRetrospectiveRecord>().WithMany().HasForeignKey(record => record.WeekStart).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgendaWeekRetrospectiveRecord>(report =>
        {
            report.ToTable("AgendaWeekRetrospectives");
            report.HasKey(record => record.StartDate);
            report.Property(record => record.StartDate).HasColumnType("TEXT").ValueGeneratedNever();
            report.Property(record => record.EndDate).HasColumnType("TEXT").IsRequired();
            report.Property(record => record.ClosedAt).HasColumnType("TEXT").IsRequired();
            report.Property(record => record.TimeZoneId).HasColumnType("TEXT").IsRequired();
            report.Property(record => record.TimeZoneData).HasColumnType("TEXT").IsRequired();
            report.Property(record => record.FormatVersion).IsRequired();
            report.Property(record => record.Content).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<AgendaPageLinkRecord>(link =>
        {
            link.ToTable("AgendaPageLinks");
            link.HasKey(record => new { record.TaskId, record.PageId });
            link.Property(record => record.TaskId).HasColumnType("TEXT").ValueGeneratedNever();
            link.Property(record => record.PageId).HasColumnType("TEXT").ValueGeneratedNever();
            link.HasIndex(record => record.PageId);
            link.HasOne<AgendaTaskRecord>().WithMany().HasForeignKey(record => record.TaskId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgendaTaskStartRecord>(start =>
        {
            start.ToTable("AgendaTaskStarts");
            start.HasKey(record => record.TaskId);
            start.Property(record => record.TaskId).HasColumnType("TEXT").ValueGeneratedNever();
            start.HasOne<AgendaTaskRecord>().WithMany().HasForeignKey(record => record.TaskId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgendaTaskRecord>(task =>
        {
            task.ToTable("AgendaTasks");
            task.HasKey(record => record.Id);
            task.Property(record => record.Id).HasColumnType("TEXT").ValueGeneratedNever();
            task.Property(record => record.Title).HasColumnType("TEXT").IsRequired();
            task.Property(record => record.Description).HasColumnType("TEXT").IsRequired();
            task.Property(record => record.PlannedDate).HasColumnType("TEXT").IsRequired();
            task.Property(record => record.PlannedEndDate).HasColumnType("TEXT");
            task.Property(record => record.StartTime).HasColumnType("TEXT");
            task.Property(record => record.EndTime).HasColumnType("TEXT");
            task.Property(record => record.Category).HasConversion<int>().IsRequired();
            task.Property(record => record.Status).HasConversion<int>().IsRequired();
            task.Property(record => record.LastCompletedAt).HasColumnType("TEXT");
            task.Property(record => record.DeletedAt).HasColumnType("TEXT");
            task.HasIndex(record => new { record.PlannedDate, record.StartTime });
        });

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

        modelBuilder.Entity<NotebookPageEditRecord>(edit =>
        {
            edit.ToTable("NotebookPageEdits");
            edit.HasKey(record => record.PageId);
            edit.Property(record => record.PageId).HasColumnType("TEXT").ValueGeneratedNever();
            edit.HasIndex(record => record.EditedAtUtcTicks);
            edit.HasOne<NotebookPageRecord>().WithMany().HasForeignKey(record => record.PageId).OnDelete(DeleteBehavior.Cascade);
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
