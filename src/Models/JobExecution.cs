using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents the execution status and result of a job run
/// </summary>
[Table("JobExecutions")]
public sealed class JobExecution
{
    /// <summary>
    /// Gets or sets the unique identifier for the job execution
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the job
    /// </summary>
    [ForeignKey(nameof(Job))]
    public int JobId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the job
    /// </summary>
    public Job Job { get; set; } = null!;

    /// <summary>
    /// Gets or sets the execution status
    /// </summary>
    public JobExecutionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the execution completed (null if still running)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the current task being executed (by ExecutionOrder)
    /// </summary>
    public int CurrentTaskIndex { get; set; }

    /// <summary>
    /// Gets or sets the total number of tasks
    /// </summary>
    public int TotalTasks { get; set; }

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
    /// Gets or sets additional execution data (serialized JSON)
    /// </summary>
    [MaxLength(4000)]
    public string? ExecutionData { get; set; }

    /// <summary>
    /// Initializes a new instance of the JobExecution class
    /// </summary>
    public JobExecution()
    {
        Status = JobExecutionStatus.Pending;
        StartedAt = DateTime.UtcNow;
        ProgressPercentage = 0;
    }

    /// <summary>
    /// Initializes a new instance of the JobExecution class with a job
    /// </summary>
    /// <param name="jobId">The ID of the job to execute</param>
    /// <param name="totalTasks">The total number of tasks in the job</param>
    public JobExecution(int jobId, int totalTasks) : this()
    {
        JobId = jobId;
        TotalTasks = totalTasks;
    }

    /// <summary>
    /// Calculates and updates the progress percentage
    /// </summary>
    public void UpdateProgress()
    {
        if (TotalTasks == 0)
        {
            ProgressPercentage = Status == JobExecutionStatus.Completed ? 100 : 0;
            return;
        }

        ProgressPercentage = (int)Math.Round((double)CurrentTaskIndex / TotalTasks * 100);
    }

    /// <summary>
    /// Marks the execution as started
    /// </summary>
    public void Start()
    {
        Status = JobExecutionStatus.Running;
        StartedAt = DateTime.UtcNow;
        CurrentTaskIndex = 0;
        ProgressPercentage = 0;
    }

    /// <summary>
    /// Marks the execution as completed successfully
    /// </summary>
    public void Complete()
    {
        Status = JobExecutionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CurrentTaskIndex = TotalTasks;
        ProgressPercentage = 100;
        CalculateDuration();
    }

    /// <summary>
    /// Marks the execution as failed with an error message
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure</param>
    public void Fail(string errorMessage)
    {
        Status = JobExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        CalculateDuration();
    }

    /// <summary>
    /// Marks the execution as cancelled
    /// </summary>
    public void Cancel()
    {
        Status = JobExecutionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        CalculateDuration();
    }

    /// <summary>
    /// Advances to the next task
    /// </summary>
    public void AdvanceToNextTask()
    {
        if (CurrentTaskIndex < TotalTasks)
        {
            CurrentTaskIndex++;
            UpdateProgress();
        }
    }

    /// <summary>
    /// Gets the execution duration as a TimeSpan
    /// </summary>
    /// <returns>The execution duration, or null if not completed</returns>
    public TimeSpan? GetDuration()
    {
        return DurationMs.HasValue ? TimeSpan.FromMilliseconds(DurationMs.Value) : null;
    }

    /// <summary>
    /// Checks if the execution is in a terminal state
    /// </summary>
    /// <returns>True if execution is completed, failed, or cancelled</returns>
    public bool IsFinished()
    {
        return Status is JobExecutionStatus.Completed 
            or JobExecutionStatus.Failed 
            or JobExecutionStatus.Cancelled;
    }

    /// <summary>
    /// Calculates the duration in milliseconds
    /// </summary>
    private void CalculateDuration()
    {
        if (CompletedAt.HasValue)
        {
            DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        }
    }
}

/// <summary>
/// Enumeration of possible job execution statuses
/// </summary>
public enum JobExecutionStatus
{
    /// <summary>
    /// Execution is pending and has not started yet
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
    /// Execution failed with an error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Execution was cancelled by user or system
    /// </summary>
    Cancelled = 4
}