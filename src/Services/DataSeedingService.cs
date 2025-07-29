using Microsoft.Extensions.Logging;
using WorkflowEngine.Data;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for seeding initial data to demonstrate dashboard functionality
/// </summary>
public sealed class DataSeedingService
{
    private readonly WorkflowDbContext _dbContext;
    private readonly ILogger<DataSeedingService> _logger;

    /// <summary>
    /// Initializes a new instance of the DataSeedingService class
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="logger">The logger instance</param>
    public DataSeedingService(WorkflowDbContext dbContext, ILogger<DataSeedingService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds the database with sample workflows and executions for demonstration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SeedSampleDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if data already exists
            if (_dbContext.Workflows.Any())
            {
                _logger.LogInformation("Sample data already exists, skipping seeding");
                return;
            }

            _logger.LogInformation("Starting to seed sample data");

            var workflows = CreateSampleWorkflows();
            await SeedWorkflowsAsync(workflows, cancellationToken);

            var executions = CreateSampleExecutions(workflows);
            await SeedExecutionsAsync(executions, cancellationToken);

            _logger.LogInformation("Successfully seeded {WorkflowCount} workflows and {ExecutionCount} executions",
                workflows.Count, executions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding sample data");
            throw;
        }
    }

    /// <summary>
    /// Creates sample workflows for demonstration
    /// </summary>
    /// <returns>List of sample workflows</returns>
    private static List<Workflow> CreateSampleWorkflows()
    {
        var baseTime = DateTime.UtcNow.AddDays(-30);

        return new List<Workflow>
        {
            new("Data Processing Pipeline", "Processes incoming data files and validates content")
            {
                Id = 1,
                CreatedAt = baseTime.AddDays(0),
                UpdatedAt = baseTime.AddDays(5),
                IsActive = true
            },
            new("Email Notification System", "Sends automated email notifications to users")
            {
                Id = 2,
                CreatedAt = baseTime.AddDays(1),
                UpdatedAt = baseTime.AddDays(3),
                IsActive = true
            },
            new("Report Generation", "Generates monthly financial reports")
            {
                Id = 3,
                CreatedAt = baseTime.AddDays(2),
                UpdatedAt = baseTime.AddDays(10),
                IsActive = false
            },
            new("Database Backup", "Performs daily database backup operations")
            {
                Id = 4,
                CreatedAt = baseTime.AddDays(3),
                UpdatedAt = baseTime.AddDays(7),
                IsActive = true
            },
            new("Security Audit", "Runs security compliance checks")
            {
                Id = 5,
                CreatedAt = baseTime.AddDays(4),
                UpdatedAt = baseTime.AddDays(8),
                IsActive = true
            },
            new("Log Analysis", "Analyzes system logs for anomalies")
            {
                Id = 6,
                CreatedAt = baseTime.AddDays(5),
                UpdatedAt = baseTime.AddDays(12),
                IsActive = false
            }
        };
    }

    /// <summary>
    /// Creates sample workflow executions for demonstration
    /// </summary>
    /// <param name="workflows">The workflows to create executions for</param>
    /// <returns>List of sample executions</returns>
    private static List<WorkflowExecution> CreateSampleExecutions(List<Workflow> workflows)
    {
        var executions = new List<WorkflowExecution>();
        var random = new Random(42); // Fixed seed for consistent data
        var baseTime = DateTime.UtcNow.AddDays(-7);

        for (int i = 1; i <= 25; i++)
        {
            var workflow = workflows[random.Next(workflows.Count)];
            var startTime = baseTime.AddHours(random.Next(0, 168)); // Random time in last 7 days
            var status = GenerateRandomStatus(random, startTime);
            
            var execution = new WorkflowExecution
            {
                Id = i,
                WorkflowId = workflow.Id,
                Status = status,
                StartedAt = startTime,
                TotalSteps = random.Next(3, 8),
                CurrentStepIndex = status == WorkflowExecutionStatus.Running ? random.Next(1, 5) : 0
            };

            // Set completion details for finished executions
            if (status is WorkflowExecutionStatus.Completed or WorkflowExecutionStatus.Failed or WorkflowExecutionStatus.Cancelled)
            {
                var duration = random.Next(1000, 30000); // 1-30 seconds
                execution.CompletedAt = startTime.AddMilliseconds(duration);
                execution.DurationMs = duration;
                execution.CurrentStepIndex = execution.TotalSteps;
            }

            // Add error message for failed executions
            if (status == WorkflowExecutionStatus.Failed)
            {
                execution.ErrorMessage = GetRandomErrorMessage(random);
            }

            execution.UpdateProgress();
            executions.Add(execution);
        }

        return executions;
    }

    /// <summary>
    /// Generates a random execution status based on realistic probabilities
    /// </summary>
    /// <param name="random">Random number generator</param>
    /// <param name="startTime">The start time of the execution</param>
    /// <returns>Random workflow execution status</returns>
    private static WorkflowExecutionStatus GenerateRandomStatus(Random random, DateTime startTime)
    {
        // Recent executions are more likely to be running
        var isRecent = startTime > DateTime.UtcNow.AddHours(-2);
        
        if (isRecent && random.NextDouble() < 0.3) // 30% chance for recent executions to be running
        {
            return WorkflowExecutionStatus.Running;
        }

        var statusValue = random.NextDouble();
        return statusValue switch
        {
            < 0.7 => WorkflowExecutionStatus.Completed,  // 70% success rate
            < 0.85 => WorkflowExecutionStatus.Failed,    // 15% failure rate
            < 0.95 => WorkflowExecutionStatus.Cancelled, // 10% cancelled
            _ => WorkflowExecutionStatus.Pending         // 5% pending
        };
    }

    /// <summary>
    /// Gets a random error message for failed executions
    /// </summary>
    /// <param name="random">Random number generator</param>
    /// <returns>Random error message</returns>
    private static string GetRandomErrorMessage(Random random)
    {
        var errorMessages = new[]
        {
            "Connection timeout while accessing external service",
            "Invalid data format in input file",
            "Insufficient permissions to access required resource",
            "Database connection failed after multiple retries",
            "Memory limit exceeded during processing",
            "External API returned HTTP 500 error",
            "File not found: required configuration file missing",
            "Network connectivity issue detected"
        };

        return errorMessages[random.Next(errorMessages.Length)];
    }

    /// <summary>
    /// Seeds workflows into the database
    /// </summary>
    /// <param name="workflows">Workflows to seed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task SeedWorkflowsAsync(List<Workflow> workflows, CancellationToken cancellationToken)
    {
        _dbContext.Workflows.AddRange(workflows);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Seeded {Count} workflows", workflows.Count);
    }

    /// <summary>
    /// Seeds workflow executions into the database
    /// </summary>
    /// <param name="executions">Executions to seed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task SeedExecutionsAsync(List<WorkflowExecution> executions, CancellationToken cancellationToken)
    {
        _dbContext.WorkflowExecutions.AddRange(executions);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Seeded {Count} workflow executions", executions.Count);
    }

    /// <summary>
    /// Clears all existing data from the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Clearing all data from database");

        _dbContext.WorkflowExecutions.RemoveRange(_dbContext.WorkflowExecutions);
        _dbContext.Workflows.RemoveRange(_dbContext.Workflows);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("All data cleared from database");
    }

    /// <summary>
    /// Resets and re-seeds the database with fresh sample data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ResetAndSeedAsync(CancellationToken cancellationToken = default)
    {
        await ClearAllDataAsync(cancellationToken);
        await SeedSampleDataAsync(cancellationToken);
    }
}