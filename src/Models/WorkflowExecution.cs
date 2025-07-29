using System.ComponentModel.DataAnnotations;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents the execution status and result of a workflow run
/// </summary>
public sealed class WorkflowExecution
{
    /// <summary>
    /// Gets or sets the unique identifier for the workflow execution
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the workflow being executed
    /// </summary>
    public int WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the workflow being executed
    /// </summary>
    public Workflow? Workflow { get; set; }

    /// <summary>
    /// Gets or sets the execution status
    /// </summary>
    public WorkflowExecutionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the start time of execution
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the completion time of execution
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the current step being executed
    /// </summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>
    /// Gets or sets the total number of steps
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the execution progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Initializes a new instance of the WorkflowExecution class
    /// </summary>
    public WorkflowExecution()
    {
        Status = WorkflowExecutionStatus.Pending;
        StartedAt = DateTime.UtcNow;
        ProgressPercentage = 0;
    }

    /// <summary>
    /// Calculates and updates the progress percentage
    /// </summary>
    public void UpdateProgress()
    {
        if (TotalSteps == 0)
        {
            ProgressPercentage = Status == WorkflowExecutionStatus.Completed ? 100 : 0;
            return;
        }

        ProgressPercentage = (int)Math.Round((double)CurrentStepIndex / TotalSteps * 100);
    }

    /// <summary>
    /// Marks the execution as completed
    /// </summary>
    /// <param name="success">Whether the execution was successful</param>
    /// <param name="errorMessage">Error message if execution failed</param>
    public void MarkCompleted(bool success, string? errorMessage = null)
    {
        CompletedAt = DateTime.UtcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        Status = success ? WorkflowExecutionStatus.Completed : WorkflowExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        ProgressPercentage = 100;
    }

    /// <summary>
    /// Marks the execution as cancelled
    /// </summary>
    public void MarkCancelled()
    {
        CompletedAt = DateTime.UtcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        Status = WorkflowExecutionStatus.Cancelled;
    }
}

/// <summary>
/// Enumeration of possible workflow execution statuses
/// </summary>
public enum WorkflowExecutionStatus
{
    /// <summary>
    /// Execution is pending/queued
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Execution is currently running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Execution completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Execution failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Execution was cancelled
    /// </summary>
    Cancelled = 4
}