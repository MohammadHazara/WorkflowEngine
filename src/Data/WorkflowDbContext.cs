using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Models;

namespace WorkflowEngine.Data;

/// <summary>
/// Entity Framework DbContext for WorkflowEngine with SQLite support
/// </summary>
public sealed class WorkflowDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the Workflows DbSet
    /// </summary>
    public DbSet<Workflow> Workflows { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Steps DbSet
    /// </summary>
    public DbSet<Step> Steps { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the WorkflowDbContext class
    /// </summary>
    /// <param name="options">The DbContext options</param>
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the database models and relationships
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureWorkflowEntity(modelBuilder);
        ConfigureStepEntity(modelBuilder);
        
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Configures the Workflow entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureWorkflowEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(w => w.Id);
            
            entity.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(w => w.Description)
                .HasMaxLength(1000);
                
            entity.Property(w => w.CreatedAt)
                .IsRequired();
                
            entity.Property(w => w.UpdatedAt)
                .IsRequired();
                
            entity.Property(w => w.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Ignore(w => w.Steps);
        });
    }

    /// <summary>
    /// Configures the Step entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureStepEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Step>(entity =>
        {
            entity.HasKey(s => s.Id);
            
            entity.Property(s => s.Name)
                .HasMaxLength(255);
                
            entity.Property(s => s.CreatedAt)
                .IsRequired();
                
            entity.Property(s => s.UpdatedAt)
                .IsRequired();

            // Ignore function properties as they can't be stored in database
            entity.Ignore(s => s.OnSuccess);
            entity.Ignore(s => s.OnFailure);
            entity.Ignore(s => s.ExecuteFunction);
        });
    }

    /// <summary>
    /// Ensures the database is created and migrated
    /// </summary>
    /// <returns>True if database was created, false if it already existed</returns>
    public async Task<bool> EnsureDatabaseCreatedAsync()
    {
        return await Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Applies any pending migrations to the database
    /// </summary>
    public async Task MigrateDatabaseAsync()
    {
        await Database.MigrateAsync();
    }
}