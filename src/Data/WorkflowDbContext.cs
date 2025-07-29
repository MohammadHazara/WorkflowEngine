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
    /// Gets or sets the WorkflowExecutions DbSet
    /// </summary>
    public DbSet<WorkflowExecution> WorkflowExecutions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Users DbSet
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

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
        ConfigureWorkflowExecutionEntity(modelBuilder);
        ConfigureUserEntity(modelBuilder);
        
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
    /// Configures the WorkflowExecution entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureWorkflowExecutionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowExecution>(entity =>
        {
            entity.HasKey(we => we.Id);
            
            entity.Property(we => we.WorkflowId)
                .IsRequired();
                
            entity.Property(we => we.Status)
                .IsRequired()
                .HasConversion<int>();
                
            entity.Property(we => we.StartedAt)
                .IsRequired();
                
            entity.Property(we => we.ErrorMessage)
                .HasMaxLength(2000);
                
            entity.Property(we => we.ProgressPercentage)
                .IsRequired()
                .HasDefaultValue(0);

            entity.HasOne(we => we.Workflow)
                .WithMany()
                .HasForeignKey(we => we.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Configures the User entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(20);
                
            entity.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            entity.Property(u => u.CreatedAt)
                .IsRequired();
                
            entity.Property(u => u.UpdatedAt)
                .IsRequired();

            // Create unique index on username
            entity.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");
                
            // Create unique index on email
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            // Ignore navigation properties to avoid circular references
            entity.Ignore(u => u.CreatedWorkflows);
            entity.Ignore(u => u.StartedExecutions);
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