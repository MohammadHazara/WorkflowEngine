using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Data;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

/// <summary>
/// Service implementation for dashboard operations with performance optimization
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly WorkflowDbContext _dbContext;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ILogger<DashboardService> _logger;
    private readonly Dictionary<int, CancellationTokenSource> _runningExecutions;

    /// <summary>
    /// Initializes a new instance of the DashboardService class
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="workflowExecutor">The workflow executor service</param>
    /// <param name="logger">The logger instance</param>
    public DashboardService(
        WorkflowDbContext dbContext,
        IWorkflowExecutor workflowExecutor,
        ILogger<DashboardService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _workflowExecutor = workflowExecutor ?? throw new ArgumentNullException(nameof(workflowExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runningExecutions = new Dictionary<int, CancellationTokenSource>();
    }

    /// <summary>
    /// Gets a paginated list of all workflows with optimized query
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of workflows</returns>
    public async Task<PaginatedResult<Workflow>> GetWorkflowsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        ValidatePaginationParameters(page, pageSize);

        var totalItems = await GetTotalWorkflowCountAsync(cancellationToken);
        var skip = CalculateSkipCount(page, pageSize);

        var workflows = await _dbContext.Workflows
            .AsNoTracking()
            .OrderByDescending(w => w.UpdatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return CreatePaginatedResult(workflows, page, pageSize, totalItems);
    }

    /// <summary>
    /// Gets a specific workflow by ID with optimized query
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow if found, null otherwise</returns>
    public async Task<Workflow?> GetWorkflowByIdAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Workflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);
    }

    /// <summary>
    /// Gets recent workflow executions with optimized query
    /// </summary>
    /// <param name="limit">Maximum number of executions to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent workflow executions</returns>
    public async Task<IEnumerable<WorkflowExecution>> GetRecentExecutionsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 100);

        return await _dbContext.WorkflowExecutions
            .AsNoTracking()
            .Include(we => we.Workflow)
            .OrderByDescending(we => we.StartedAt)
            .Take(clampedLimit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets executions for a specific workflow with pagination
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of workflow executions</returns>
    public async Task<PaginatedResult<WorkflowExecution>> GetWorkflowExecutionsAsync(int workflowId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        ValidatePaginationParameters(page, pageSize);

        var totalItems = await GetTotalExecutionCountForWorkflowAsync(workflowId, cancellationToken);
        var skip = CalculateSkipCount(page, pageSize);

        var executions = await _dbContext.WorkflowExecutions
            .AsNoTracking()
            .Include(we => we.Workflow)
            .Where(we => we.WorkflowId == workflowId)
            .OrderByDescending(we => we.StartedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return CreatePaginatedResult(executions, page, pageSize, totalItems);
    }

    /// <summary>
    /// Gets dashboard statistics with optimized aggregation queries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics</returns>
    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var workflowStats = await GetWorkflowStatsAsync(cancellationToken);
        var executionStats = await GetExecutionStatsAsync(cancellationToken);

        return new DashboardStats
        {
            TotalWorkflows = workflowStats.Total,
            ActiveWorkflows = workflowStats.Active,
            TotalExecutions = executionStats.Total,
            RunningExecutions = executionStats.Running,
            CompletedExecutions = executionStats.Completed,
            FailedExecutions = executionStats.Failed,
            AverageExecutionTimeMs = executionStats.AverageTimeMs,
            SuccessRate = CalculateSuccessRate(executionStats.Completed, executionStats.Total)
        };
    }

    /// <summary>
    /// Starts a workflow execution asynchronously
    /// </summary>
    /// <param name="workflowId">The workflow ID to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created workflow execution</returns>
    public async Task<WorkflowExecution> StartWorkflowExecutionAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = await GetWorkflowByIdAsync(workflowId, cancellationToken);
        if (workflow is null)
        {
            throw new InvalidOperationException($"Workflow with ID {workflowId} not found");
        }

        if (!workflow.IsActive)
        {
            throw new InvalidOperationException($"Workflow {workflowId} is not active");
        }

        var execution = await CreateWorkflowExecutionAsync(workflowId, cancellationToken);
        
        // Start execution in background
        _ = Task.Run(async () => await ExecuteWorkflowInBackgroundAsync(execution, workflow), cancellationToken);

        LogWorkflowExecutionStarted(execution);
        return execution;
    }

    /// <summary>
    /// Cancels a running workflow execution
    /// </summary>
    /// <param name="executionId">The execution ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation was successful</returns>
    public async Task<bool> CancelWorkflowExecutionAsync(int executionId, CancellationToken cancellationToken = default)
    {
        if (_runningExecutions.TryGetValue(executionId, out var cts))
        {
            cts.Cancel();
            _runningExecutions.Remove(executionId);

            var execution = await _dbContext.WorkflowExecutions
                .FirstOrDefaultAsync(we => we.Id == executionId, cancellationToken);

            if (execution is not null)
            {
                execution.MarkCancelled();
                await _dbContext.SaveChangesAsync(cancellationToken);
                LogWorkflowExecutionCancelled(execution);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a new workflow
    /// </summary>
    /// <param name="workflow">The workflow to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created workflow</returns>
    public async Task<Workflow> CreateWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        _dbContext.Workflows.Add(workflow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogWorkflowCreated(workflow);
        return workflow;
    }

    /// <summary>
    /// Updates an existing workflow
    /// </summary>
    /// <param name="workflow">The workflow to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated workflow</returns>
    public async Task<Workflow> UpdateWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        workflow.UpdateTimestamp();
        _dbContext.Workflows.Update(workflow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogWorkflowUpdated(workflow);
        return workflow;
    }

    /// <summary>
    /// Deletes a workflow and all its executions
    /// </summary>
    /// <param name="workflowId">The workflow ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    public async Task<bool> DeleteWorkflowAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        var workflow = await _dbContext.Workflows
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        if (workflow is null)
        {
            return false;
        }

        _dbContext.Workflows.Remove(workflow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogWorkflowDeleted(workflow);
        return true;
    }

    /// <summary>
    /// Executes a workflow in the background with progress tracking
    /// </summary>
    /// <param name="execution">The workflow execution</param>
    /// <param name="workflow">The workflow to execute</param>
    private async Task ExecuteWorkflowInBackgroundAsync(WorkflowExecution execution, Workflow workflow)
    {
        var cts = new CancellationTokenSource();
        _runningExecutions[execution.Id] = cts;

        try
        {
            execution.Status = WorkflowExecutionStatus.Running;
            await _dbContext.SaveChangesAsync();

            var progress = new Progress<int>(percentage =>
            {
                execution.ProgressPercentage = percentage;
                _ = _dbContext.SaveChangesAsync();
            });

            var result = await _workflowExecutor.ExecuteWorkflowWithProgressAsync(workflow, progress, cts.Token);
            
            execution.MarkCompleted(result);
            await _dbContext.SaveChangesAsync();

            LogWorkflowExecutionCompleted(execution, result);
        }
        catch (OperationCanceledException)
        {
            execution.MarkCancelled();
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            execution.MarkCompleted(false, ex.Message);
            await _dbContext.SaveChangesAsync();
            LogWorkflowExecutionFailed(execution, ex);
        }
        finally
        {
            _runningExecutions.Remove(execution.Id);
            cts.Dispose();
        }
    }

    /// <summary>
    /// Creates a new workflow execution record
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created workflow execution</returns>
    private async Task<WorkflowExecution> CreateWorkflowExecutionAsync(int workflowId, CancellationToken cancellationToken)
    {
        var execution = new WorkflowExecution
        {
            WorkflowId = workflowId,
            Status = WorkflowExecutionStatus.Pending
        };

        _dbContext.WorkflowExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return execution;
    }

    /// <summary>
    /// Gets workflow statistics with optimized query
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Workflow statistics</returns>
    private async Task<(int Total, int Active)> GetWorkflowStatsAsync(CancellationToken cancellationToken)
    {
        var stats = await _dbContext.Workflows
            .AsNoTracking()
            .GroupBy(w => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(w => w.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return (stats?.Total ?? 0, stats?.Active ?? 0);
    }

    /// <summary>
    /// Gets execution statistics with optimized aggregation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution statistics</returns>
    private async Task<(int Total, int Running, int Completed, int Failed, double AverageTimeMs)> GetExecutionStatsAsync(CancellationToken cancellationToken)
    {
        var stats = await _dbContext.WorkflowExecutions
            .AsNoTracking()
            .GroupBy(we => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Running = g.Count(we => we.Status == WorkflowExecutionStatus.Running),
                Completed = g.Count(we => we.Status == WorkflowExecutionStatus.Completed),
                Failed = g.Count(we => we.Status == WorkflowExecutionStatus.Failed),
                AverageTimeMs = g.Where(we => we.DurationMs.HasValue).Average(we => (double?)we.DurationMs) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        return (stats?.Total ?? 0, stats?.Running ?? 0, stats?.Completed ?? 0, stats?.Failed ?? 0, stats?.AverageTimeMs ?? 0);
    }

    /// <summary>
    /// Gets total workflow count
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total workflow count</returns>
    private async Task<int> GetTotalWorkflowCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Workflows.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets total execution count for a specific workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total execution count</returns>
    private async Task<int> GetTotalExecutionCountForWorkflowAsync(int workflowId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkflowExecutions
            .CountAsync(we => we.WorkflowId == workflowId, cancellationToken);
    }

    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    private static void ValidatePaginationParameters(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentException("Page must be greater than 0", nameof(page));
        
        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
    }

    /// <summary>
    /// Calculates skip count for pagination
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Skip count</returns>
    private static int CalculateSkipCount(int page, int pageSize)
    {
        return (page - 1) * pageSize;
    }

    /// <summary>
    /// Creates a paginated result
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Items for current page</param>
    /// <param name="page">Current page</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="totalItems">Total items</param>
    /// <returns>Paginated result</returns>
    private static PaginatedResult<T> CreatePaginatedResult<T>(IEnumerable<T> items, int page, int pageSize, int totalItems)
    {
        return new PaginatedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    /// <summary>
    /// Calculates success rate percentage
    /// </summary>
    /// <param name="completed">Completed executions</param>
    /// <param name="total">Total executions</param>
    /// <returns>Success rate percentage</returns>
    private static double CalculateSuccessRate(int completed, int total)
    {
        return total == 0 ? 0 : Math.Round((double)completed / total * 100, 2);
    }

    // Logging methods
    private void LogWorkflowExecutionStarted(WorkflowExecution execution)
    {
        _logger.LogInformation("Started workflow execution {ExecutionId} for workflow {WorkflowId}", 
            execution.Id, execution.WorkflowId);
    }

    private void LogWorkflowExecutionCompleted(WorkflowExecution execution, bool success)
    {
        _logger.LogInformation("Completed workflow execution {ExecutionId} with result: {Success}, duration: {Duration}ms", 
            execution.Id, success, execution.DurationMs);
    }

    private void LogWorkflowExecutionCancelled(WorkflowExecution execution)
    {
        _logger.LogWarning("Cancelled workflow execution {ExecutionId}", execution.Id);
    }

    private void LogWorkflowExecutionFailed(WorkflowExecution execution, Exception ex)
    {
        _logger.LogError(ex, "Failed workflow execution {ExecutionId}", execution.Id);
    }

    private void LogWorkflowCreated(Workflow workflow)
    {
        _logger.LogInformation("Created workflow {WorkflowId}: {WorkflowName}", workflow.Id, workflow.Name);
    }

    private void LogWorkflowUpdated(Workflow workflow)
    {
        _logger.LogInformation("Updated workflow {WorkflowId}: {WorkflowName}", workflow.Id, workflow.Name);
    }

    private void LogWorkflowDeleted(Workflow workflow)
    {
        _logger.LogInformation("Deleted workflow {WorkflowId}: {WorkflowName}", workflow.Id, workflow.Name);
    }
}