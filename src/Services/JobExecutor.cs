using Microsoft.Extensions.Logging;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for executing jobs with performance optimization and progress reporting
/// </summary>
public sealed class JobExecutor
{
    private readonly ILogger<JobExecutor>? _logger;
    private const long EstimatedTaskExecutionTimeMs = 100;

    /// <summary>
    /// Initializes a new instance of the JobExecutor class
    /// </summary>
    /// <param name="logger">Optional logger for job execution logging</param>
    public JobExecutor(ILogger<JobExecutor>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a job asynchronously with performance monitoring
    /// </summary>
    /// <param name="job">The job to execute</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A job execution result</returns>
    public async Task<JobExecution> ExecuteJobAsync(Job job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var execution = new JobExecution(job.Id, job.GetTaskCount());
        
        if (!CanExecuteJob(job))
        {
            execution.Fail("Job validation failed - job is not valid for execution");
            LogJobValidationFailure(job);
            return execution;
        }

        try
        {
            LogJobExecutionStart(job);
            execution.Start();

            var success = await ExecuteJobTasksAsync(job, execution, cancellationToken);
            
            if (success)
            {
                execution.Complete();
                LogJobExecutionComplete(job, true, execution.DurationMs ?? 0);
            }
            else
            {
                execution.Fail("One or more tasks failed during execution");
                LogJobExecutionComplete(job, false, execution.DurationMs ?? 0);
            }

            return execution;
        }
        catch (OperationCanceledException)
        {
            execution.Cancel();
            LogJobExecutionCancelled(job);
            return execution;
        }
        catch (Exception ex)
        {
            execution.Fail($"Job execution failed with exception: {ex.Message}");
            LogJobExecutionError(job, ex);
            return execution;
        }
    }

    /// <summary>
    /// Executes a job with progress reporting
    /// </summary>
    /// <param name="job">The job to execute</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A job execution result</returns>
    public async Task<JobExecution> ExecuteJobWithProgressAsync(Job job, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(progress);

        var execution = new JobExecution(job.Id, job.GetTaskCount());
        
        if (!CanExecuteJob(job))
        {
            execution.Fail("Job validation failed - job is not valid for execution");
            LogJobValidationFailure(job);
            return execution;
        }

        try
        {
            LogJobExecutionStart(job);
            execution.Start();
            progress.Report(0);

            foreach (var task in job.Tasks.OrderBy(t => t.ExecutionOrder))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var taskResult = await ExecuteTaskSafelyAsync(task);
                if (!taskResult)
                {
                    execution.Fail($"Task '{task.Name}' failed during execution");
                    LogJobTaskFailure(job, task);
                    return execution;
                }

                execution.AdvanceToNextTask();
                progress.Report(execution.ProgressPercentage);
                LogJobTaskComplete(job, task, execution.CurrentTaskIndex, execution.TotalTasks);
            }

            execution.Complete();
            progress.Report(100);
            LogJobExecutionComplete(job, true, execution.DurationMs ?? 0);
            return execution;
        }
        catch (OperationCanceledException)
        {
            execution.Cancel();
            LogJobExecutionCancelled(job);
            return execution;
        }
        catch (Exception ex)
        {
            execution.Fail($"Job execution failed with exception: {ex.Message}");
            LogJobExecutionError(job, ex);
            return execution;
        }
    }

    /// <summary>
    /// Executes a job group asynchronously
    /// </summary>
    /// <param name="jobGroup">The job group to execute</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if all jobs executed successfully, false otherwise</returns>
    public async Task<bool> ExecuteJobGroupAsync(JobGroup jobGroup, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobGroup);

        if (!jobGroup.HasJobs())
        {
            _logger?.LogInformation("Job group {GroupName} has no jobs to execute", jobGroup.Name);
            return true;
        }

        _logger?.LogInformation("Starting execution of job group {GroupName} with {JobCount} jobs", 
            jobGroup.Name, jobGroup.GetJobCount());

        foreach (var job in jobGroup.Jobs.OrderBy(j => j.ExecutionOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var execution = await ExecuteJobAsync(job, cancellationToken);
            if (!execution.IsFinished() || execution.Status != JobExecutionStatus.Completed)
            {
                _logger?.LogError("Job {JobName} in group {GroupName} failed with status: {Status}", 
                    job.Name, jobGroup.Name, execution.Status);
                return false;
            }

            _logger?.LogInformation("Completed job {JobName} in group {GroupName}", job.Name, jobGroup.Name);
        }

        _logger?.LogInformation("Successfully completed job group {GroupName}", jobGroup.Name);
        return true;
    }

    /// <summary>
    /// Validates if a job can be executed
    /// </summary>
    /// <param name="job">The job to validate</param>
    /// <returns>True if job is valid for execution, false otherwise</returns>
    public bool CanExecuteJob(Job job)
    {
        return job is not null &&
               job.ValidateJob() &&
               job.HasTasks() &&
               ValidateAllTasks(job);
    }

    /// <summary>
    /// Gets the estimated execution time for a job
    /// </summary>
    /// <param name="job">The job to estimate</param>
    /// <returns>Estimated execution time in milliseconds</returns>
    public long GetEstimatedExecutionTime(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        return job.GetTaskCount() * EstimatedTaskExecutionTimeMs;
    }

    /// <summary>
    /// Executes all tasks in the job sequentially
    /// </summary>
    /// <param name="job">The job to execute</param>
    /// <param name="execution">The execution tracking object</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if all tasks executed successfully, false otherwise</returns>
    private async Task<bool> ExecuteJobTasksAsync(Job job, JobExecution execution, CancellationToken cancellationToken)
    {
        foreach (var task in job.Tasks.OrderBy(t => t.ExecutionOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskResult = await ExecuteTaskSafelyAsync(task);
            if (!taskResult)
            {
                LogJobTaskFailure(job, task);
                return false;
            }

            execution.AdvanceToNextTask();
        }

        return true;
    }

    /// <summary>
    /// Executes a task with error handling
    /// </summary>
    /// <param name="task">The task to execute</param>
    /// <returns>True if execution was successful, false otherwise</returns>
    private async Task<bool> ExecuteTaskSafelyAsync(JobTask task)
    {
        try
        {
            return await task.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Task {TaskId}: {TaskName} failed with exception", task.Id, task.Name);
            return false;
        }
    }

    /// <summary>
    /// Validates all tasks in the job
    /// </summary>
    /// <param name="job">The job to validate</param>
    /// <returns>True if all tasks are valid, false otherwise</returns>
    private bool ValidateAllTasks(Job job)
    {
        return job.Tasks.All(task => 
            !string.IsNullOrWhiteSpace(task.Name) && 
            task.Id > 0 && 
            task.IsActive);
    }

    /// <summary>
    /// Logs job execution start
    /// </summary>
    /// <param name="job">The job being executed</param>
    private void LogJobExecutionStart(Job job)
    {
        _logger?.LogInformation("Starting execution of job {JobId}: {JobName} ({JobType}) with {TaskCount} tasks",
            job.Id, job.Name, job.JobType, job.GetTaskCount());
    }

    /// <summary>
    /// Logs job execution completion
    /// </summary>
    /// <param name="job">The executed job</param>
    /// <param name="success">Whether execution was successful</param>
    /// <param name="durationMs">Execution duration in milliseconds</param>
    private void LogJobExecutionComplete(Job job, bool success, long durationMs)
    {
        _logger?.LogInformation("Completed execution of job {JobId}: {JobName}. Success: {Success}, Duration: {Duration}ms",
            job.Id, job.Name, success, durationMs);
    }

    /// <summary>
    /// Logs job execution cancellation
    /// </summary>
    /// <param name="job">The cancelled job</param>
    private void LogJobExecutionCancelled(Job job)
    {
        _logger?.LogWarning("Execution of job {JobId}: {JobName} was cancelled", job.Id, job.Name);
    }

    /// <summary>
    /// Logs job execution error
    /// </summary>
    /// <param name="job">The failed job</param>
    /// <param name="exception">The exception that occurred</param>
    private void LogJobExecutionError(Job job, Exception exception)
    {
        _logger?.LogError(exception, "Error executing job {JobId}: {JobName}", job.Id, job.Name);
    }

    /// <summary>
    /// Logs job validation failure
    /// </summary>
    /// <param name="job">The invalid job</param>
    private void LogJobValidationFailure(Job job)
    {
        _logger?.LogWarning("Job validation failed for job {JobId}: {JobName}", job?.Id, job?.Name);
    }

    /// <summary>
    /// Logs job task failure
    /// </summary>
    /// <param name="job">The job containing the failed task</param>
    /// <param name="task">The failed task</param>
    private void LogJobTaskFailure(Job job, JobTask task)
    {
        _logger?.LogError("Task {TaskId}: {TaskName} failed in job {JobId}: {JobName}",
            task.Id, task.Name, job.Id, job.Name);
    }

    /// <summary>
    /// Logs job task completion
    /// </summary>
    /// <param name="job">The job containing the completed task</param>
    /// <param name="task">The completed task</param>
    /// <param name="completed">Number of completed tasks</param>
    /// <param name="total">Total number of tasks</param>
    private void LogJobTaskComplete(Job job, JobTask task, int completed, int total)
    {
        _logger?.LogDebug("Task {TaskId}: {TaskName} completed in job {JobId}: {JobName} ({Completed}/{Total})",
            task.Id, task.Name, job.Id, job.Name, completed, total);
    }
}