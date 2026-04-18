using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data.Entities;

namespace Papra.Companion.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<JobResultEntity> JobResults => Set<JobResultEntity>();
    public DbSet<PipelineSettingsEntity> PipelineSettings => Set<PipelineSettingsEntity>();
    public DbSet<EmailAttachmentSettingsEntity> EmailAttachmentSettings => Set<EmailAttachmentSettingsEntity>();
    public DbSet<EmailAttachmentLogEntity> EmailAttachmentLog => Set<EmailAttachmentLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobResultEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentId).IsRequired();
            e.Property(x => x.OrganizationId).IsRequired();
            e.Property(x => x.Status).IsRequired();
            e.HasIndex(x => x.StartedAt);
        });

        modelBuilder.Entity<PipelineSettingsEntity>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<EmailAttachmentSettingsEntity>(e =>
        {
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<EmailAttachmentLogEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MessageId).IsRequired();
            e.Property(x => x.AttachmentName).IsRequired();
            e.HasIndex(x => new { x.MessageId, x.AttachmentName }).IsUnique();
            e.HasIndex(x => x.DownloadedAt);
        });
    }
}
