using WorkflowEngine.Models;

namespace WorkflowEngine.Interfaces;

/// <summary>
/// Interface for handling step execution with async support
/// </summary>
public interface IStepHandler
{
    /// <summary>
    /// Executes a step asynchronously
    /// </summary>
    /// <param name="step">The step to execute</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if execution was successful, false otherwise</returns>
    Task<bool> ExecuteStepAsync(Step step, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a step can be executed
    /// </summary>
    /// <param name="step">The step to validate</param>
    /// <returns>True if step is valid for execution, false otherwise</returns>
    bool CanExecuteStep(Step step);

    /// <summary>
    /// Gets the execution priority for a step
    /// </summary>
    /// <param name="step">The step to get priority for</param>
    /// <returns>The execution priority (higher number = higher priority)</returns>
    int GetExecutionPriority(Step step);
}