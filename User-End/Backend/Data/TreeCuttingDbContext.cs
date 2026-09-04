using Microsoft.EntityFrameworkCore;
using TreeCutting.Api.Models;

namespace TreeCutting.Api.Data;

public class TreeCuttingDbContext : DbContext
{
    public TreeCuttingDbContext(DbContextOptions<TreeCuttingDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationType> ApplicationTypes => Set<ApplicationType>();
    public DbSet<ApplicantType> ApplicantTypes => Set<ApplicantType>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<ApplicationTypeDocumentMapping> ApplicationTypeDocumentMappings => Set<ApplicationTypeDocumentMapping>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<ApplicationPhoto> ApplicationPhotos => Set<ApplicationPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Disable foreign key relationships
        modelBuilder.Entity<Application>().Ignore(x => x.ApplicationType);
        modelBuilder.Entity<Application>().Ignore(x => x.ApplicantType);
        modelBuilder.Entity<Application>().Ignore(x => x.Documents);
        modelBuilder.Entity<Application>().Ignore(x => x.Photos);
        modelBuilder.Entity<ApplicationDocument>().Ignore(x => x.Application);
        modelBuilder.Entity<ApplicationDocument>().Ignore(x => x.DocumentType);
        modelBuilder.Entity<ApplicationDocument>().Ignore(x => x.ApplicationType);
        modelBuilder.Entity<ApplicationType>().Ignore(x => x.Applications);
        modelBuilder.Entity<ApplicantType>().Ignore(x => x.Applications);
        modelBuilder.Entity<DocumentType>().Ignore(x => x.ApplicationDocuments);

        // Table mappings only
        modelBuilder.Entity<ApplicationType>()
            .ToTable("ApplicationType")
            .HasIndex(x => x.ApplicationTypeName)
            .IsUnique();

        modelBuilder.Entity<ApplicantType>()
            .ToTable("ApplicantType")
            .HasIndex(x => x.ApplicantTypeName)
            .IsUnique();

        modelBuilder.Entity<DocumentType>()
            .ToTable("DocumentType")
            .HasIndex(x => x.DocumentTypeName)
            .IsUnique();

        modelBuilder.Entity<ApplicationTypeDocumentMapping>()
            .ToTable("ApplicationTypeDocumentMapping")
            .HasIndex(x => new { x.ApplicationTypeId, x.DocumentTypeId })
            .IsUnique();

        modelBuilder.Entity<Application>()
            .ToTable("Application");

        modelBuilder.Entity<ApplicationDocument>()
            .ToTable("ApplicationDocument");

        modelBuilder.Entity<ApplicationPhoto>()
            .ToTable("ApplicationPhoto");
    }
}
