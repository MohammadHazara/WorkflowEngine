using WorkflowEngine.Models;

namespace WorkflowEngine.Interfaces;

/// <summary>
/// Interface for dashboard services to monitor and manage workflows
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets a paginated list of all workflows
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of workflows</returns>
    Task<PaginatedResult<Workflow>> GetWorkflowsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific workflow by ID
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow if found, null otherwise</returns>
    Task<Workflow?> GetWorkflowByIdAsync(int workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent workflow executions
    /// </summary>
    /// <param name="limit">Maximum number of executions to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent workflow executions</returns>
    Task<IEnumerable<WorkflowExecution>> GetRecentExecutionsAsync(int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets executions for a specific workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of workflow executions</returns>
    Task<PaginatedResult<WorkflowExecution>> GetWorkflowExecutionsAsync(int workflowId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dashboard statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics</returns>
    Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a workflow execution
    /// </summary>
    /// <param name="workflowId">The workflow ID to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created workflow execution</returns>
    Task<WorkflowExecution> StartWorkflowExecutionAsync(int workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow execution
    /// </summary>
    /// <param name="executionId">The execution ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelWorkflowExecutionAsync(int executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new workflow
    /// </summary>
    /// <param name="workflow">The workflow to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created workflow</returns>
    Task<Workflow> CreateWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing workflow
    /// </summary>
    /// <param name="workflow">The workflow to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated workflow</returns>
    Task<Workflow> UpdateWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteWorkflowAsync(int workflowId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a paginated result
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public sealed class PaginatedResult<T>
{
    /// <summary>
    /// Gets or sets the items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets the total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    /// <summary>
    /// Gets whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Represents dashboard statistics
/// </summary>
public sealed class DashboardStats
{
    /// <summary>
    /// Gets or sets the total number of workflows
    /// </summary>
    public int TotalWorkflows { get; set; }

    /// <summary>
    /// Gets or sets the number of active workflows
    /// </summary>
    public int ActiveWorkflows { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of running executions
    /// </summary>
    public int RunningExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of completed executions
    /// </summary>
    public int CompletedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }
}