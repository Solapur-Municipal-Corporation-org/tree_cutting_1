using Microsoft.EntityFrameworkCore;
using AdminPanel.Api.Models;

namespace AdminPanel.Api;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
	public DbSet<AdminApplication> Applications => Set<AdminApplication>();
	public DbSet<AdminApplicationType> ApplicationTypes => Set<AdminApplicationType>();
	public DbSet<AdminApplicantType> ApplicantTypes => Set<AdminApplicantType>();
	public DbSet<AdminDocumentType> DocumentTypes => Set<AdminDocumentType>();
	public DbSet<AdminApplicationDocument> ApplicationDocuments => Set<AdminApplicationDocument>();
	public DbSet<AdminApplicationPhoto> ApplicationPhotos => Set<AdminApplicationPhoto>();
	public DbSet<WorkflowHistory> WorkflowHistory => Set<WorkflowHistory>();
	public DbSet<DepartmentReview> DepartmentReviews => Set<DepartmentReview>();
	public DbSet<AdminDepartment> Departments => Set<AdminDepartment>();
	public DbSet<AdminDesignation> Designations => Set<AdminDesignation>();
	public DbSet<AdminRole> Roles => Set<AdminRole>();
	public DbSet<AdminEmployeeRole> EmployeeRoles => Set<AdminEmployeeRole>();
	public DbSet<WorkflowStatus> WorkflowStatuses => Set<WorkflowStatus>();
	public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
	public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
	public DbSet<WorkflowDecision> WorkflowDecisions => Set<WorkflowDecision>();
	public DbSet<AdminZone> Zones => Set<AdminZone>();
	public DbSet<AdminPrabhag> Prabhags => Set<AdminPrabhag>();
	public DbSet<AdminWard> Wards => Set<AdminWard>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<AdminApplication>().ToTable("Application").Ignore(x => x.ApplicationType).Ignore(x => x.ApplicantType);
		modelBuilder.Entity<AdminApplicationType>().ToTable("ApplicationType");
		modelBuilder.Entity<AdminApplicantType>().ToTable("ApplicantType");
		modelBuilder.Entity<AdminDocumentType>().ToTable("DocumentType");
		modelBuilder.Entity<AdminApplicationDocument>().ToTable("ApplicationDocument").Ignore(x => x.Application).Ignore(x => x.DocumentType);
		modelBuilder.Entity<AdminApplicationPhoto>().ToTable("ApplicationPhoto").Ignore(x => x.Application);
		modelBuilder.Entity<WorkflowHistory>().ToTable("TreeCuttingWorkflowHistory");
		modelBuilder.Entity<DepartmentReview>().ToTable("TreeCuttingDepartmentReview");
		modelBuilder.Entity<AdminDepartment>().ToTable("TreeCuttingDepartment");
		modelBuilder.Entity<AdminDesignation>().ToTable("TreeCuttingDesignation");
		modelBuilder.Entity<AdminRole>().ToTable("TreeCuttingRole");
		modelBuilder.Entity<AdminEmployeeRole>().ToTable("TreeCuttingEmployeeRole");
		modelBuilder.Entity<WorkflowStatus>().ToTable("TreeCuttingWorkflowStatus");
		modelBuilder.Entity<WorkflowStage>().ToTable("TreeCuttingWorkflowStage");
		modelBuilder.Entity<WorkflowTransition>().ToTable("TreeCuttingWorkflowTransition");
		modelBuilder.Entity<WorkflowDecision>().ToTable("TreeCuttingWorkflowDecision");
		modelBuilder.Entity<AdminZone>().ToTable("TreeCuttingZone");
		modelBuilder.Entity<AdminPrabhag>().ToTable("TreeCuttingPrabhag");
		modelBuilder.Entity<AdminWard>().ToTable("TreeCuttingWard");
	}
}

