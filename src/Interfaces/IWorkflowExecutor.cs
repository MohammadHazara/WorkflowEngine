using WorkflowEngine.Models;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowEngine.Interfaces;

/// <summary>
/// Interface for executing workflows with async support and advanced features
/// </summary>
public interface IWorkflowExecutor
{
    /// <summary>
    /// Executes a workflow asynchronously
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if execution was successful, false otherwise</returns>
    Task<bool> ExecuteWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a workflow can be executed
    /// </summary>
    /// <param name="workflow">The workflow to validate</param>
    /// <returns>True if workflow is valid for execution, false otherwise</returns>
    bool CanExecuteWorkflow(Workflow workflow);

    /// <summary>
    /// Gets the estimated execution time for a workflow
    /// </summary>
    /// <param name="workflow">The workflow to estimate</param>
    /// <returns>Estimated execution time in milliseconds</returns>
    Task<long> GetEstimatedExecutionTimeAsync(Workflow workflow);

    /// <summary>
    /// Executes a workflow with progress reporting
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="progress">Progress reporter for execution status</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if execution was successful, false otherwise</returns>
    Task<bool> ExecuteWorkflowWithProgressAsync(Workflow workflow, IProgress<int> progress, CancellationToken cancellationToken = default);
}