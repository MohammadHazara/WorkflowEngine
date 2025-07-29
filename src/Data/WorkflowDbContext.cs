using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Models;

namespace WorkflowEngine.Data;

/// <summary>
/// Entity Framework DbContext for WorkflowEngine with SQLite support
/// </summary>
public sealed class WorkflowDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the Workflows DbSet (legacy)
    /// </summary>
    public DbSet<Workflow> Workflows { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Steps DbSet (legacy)
    /// </summary>
    public DbSet<Step> Steps { get; set; } = null!;

    /// <summary>
    /// Gets or sets the WorkflowExecutions DbSet (legacy)
    /// </summary>
    public DbSet<WorkflowExecution> WorkflowExecutions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Users DbSet
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JobGroups DbSet
    /// </summary>
    public DbSet<JobGroup> JobGroups { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Jobs DbSet
    /// </summary>
    public DbSet<Job> Jobs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JobTasks DbSet
    /// </summary>
    public DbSet<JobTask> JobTasks { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JobExecutions DbSet
    /// </summary>
    public DbSet<JobExecution> JobExecutions { get; set; } = null!;

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
        // Legacy models
        ConfigureWorkflowEntity(modelBuilder);
        ConfigureStepEntity(modelBuilder);
        ConfigureWorkflowExecutionEntity(modelBuilder);
        ConfigureUserEntity(modelBuilder);
        
        // New hierarchical models
        ConfigureJobGroupEntity(modelBuilder);
        ConfigureJobEntity(modelBuilder);
        ConfigureJobTaskEntity(modelBuilder);
        ConfigureJobExecutionEntity(modelBuilder);
        
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Configures the JobGroup entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureJobGroupEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobGroup>(entity =>
        {
            entity.HasKey(jg => jg.Id);
            
            entity.Property(jg => jg.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(jg => jg.Description)
                .HasMaxLength(1000);
                
            entity.Property(jg => jg.CreatedAt)
                .IsRequired();
                
            entity.Property(jg => jg.UpdatedAt)
                .IsRequired();
                
            entity.Property(jg => jg.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Configure the relationship with Jobs
            entity.HasMany(jg => jg.JobsCollection)
                .WithOne(j => j.JobGroup)
                .HasForeignKey(j => j.JobGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore the private collection
            entity.Ignore(jg => jg.Jobs);
        });
    }

    /// <summary>
    /// Configures the Job entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureJobEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(j => j.Id);
            
            entity.Property(j => j.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(j => j.Description)
                .HasMaxLength(1000);
                
            entity.Property(j => j.JobType)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("General");
                
            entity.Property(j => j.ExecutionOrder)
                .IsRequired()
                .HasDefaultValue(1);
                
            entity.Property(j => j.CreatedAt)
                .IsRequired();
                
            entity.Property(j => j.UpdatedAt)
                .IsRequired();
                
            entity.Property(j => j.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Configure the relationship with JobTasks
            entity.HasMany(j => j.TasksCollection)
                .WithOne(jt => jt.Job)
                .HasForeignKey(jt => jt.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship with JobExecutions
            entity.HasMany(j => j.Executions)
                .WithOne(je => je.Job)
                .HasForeignKey(je => je.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore the private collection
            entity.Ignore(j => j.Tasks);
        });
    }

    /// <summary>
    /// Configures the JobTask entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureJobTaskEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobTask>(entity =>
        {
            entity.HasKey(jt => jt.Id);
            
            entity.Property(jt => jt.Name)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(jt => jt.Description)
                .HasMaxLength(1000);
                
            entity.Property(jt => jt.TaskType)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("General");
                
            entity.Property(jt => jt.ExecutionOrder)
                .IsRequired()
                .HasDefaultValue(1);
                
            entity.Property(jt => jt.ConfigurationData)
                .HasMaxLength(4000);
                
            entity.Property(jt => jt.MaxRetries)
                .IsRequired()
                .HasDefaultValue(3);
                
            entity.Property(jt => jt.TimeoutSeconds)
                .IsRequired()
                .HasDefaultValue(300);
                
            entity.Property(jt => jt.CreatedAt)
                .IsRequired();
                
            entity.Property(jt => jt.UpdatedAt)
                .IsRequired();
                
            entity.Property(jt => jt.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Ignore function properties as they can't be stored in database
            entity.Ignore(jt => jt.OnSuccess);
            entity.Ignore(jt => jt.OnFailure);
            entity.Ignore(jt => jt.ExecuteFunction);
        });
    }

    /// <summary>
    /// Configures the JobExecution entity
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureJobExecutionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobExecution>(entity =>
        {
            entity.HasKey(je => je.Id);
            
            entity.Property(je => je.JobId)
                .IsRequired();
                
            entity.Property(je => je.Status)
                .IsRequired()
                .HasConversion<int>();
                
            entity.Property(je => je.StartedAt)
                .IsRequired();
                
            entity.Property(je => je.CurrentTaskIndex)
                .IsRequired()
                .HasDefaultValue(0);
                
            entity.Property(je => je.TotalTasks)
                .IsRequired()
                .HasDefaultValue(0);
                
            entity.Property(je => je.ErrorMessage)
                .HasMaxLength(2000);
                
            entity.Property(je => je.ProgressPercentage)
                .IsRequired()
                .HasDefaultValue(0);
                
            entity.Property(je => je.ExecutionData)
                .HasMaxLength(4000);
        });
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