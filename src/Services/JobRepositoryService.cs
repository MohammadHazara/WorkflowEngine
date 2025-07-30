using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WorkflowEngine.Data;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for managing job persistence operations with performance optimization
/// </summary>
public sealed class JobRepositoryService
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<JobRepositoryService>? _logger;

    /// <summary>
    /// Initializes a new instance of the JobRepositoryService class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">Optional logger for repository operations</param>
    public JobRepositoryService(WorkflowDbContext context, ILogger<JobRepositoryService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    #region JobGroup Operations

    /// <summary>
    /// Creates a new job group in the database
    /// </summary>
    /// <param name="jobGroup">The job group to create</param>
    /// <returns>The created job group with assigned ID</returns>
    public async Task<JobGroup> CreateJobGroupAsync(JobGroup jobGroup)
    {
        ArgumentNullException.ThrowIfNull(jobGroup);

        _logger?.LogInformation("Creating job group: {JobGroupName}", jobGroup.Name);

        _context.JobGroups.Add(jobGroup);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Created job group with ID: {JobGroupId}", jobGroup.Id);
        return jobGroup;
    }

    /// <summary>
    /// Gets the total count of active job groups
    /// </summary>
    /// <returns>The total count of active job groups</returns>
    public async Task<int> GetJobGroupsCountAsync()
    {
        try
        {
            _logger?.LogDebug("Getting total count of active job groups");
            
            var count = await _context.JobGroups
                .AsNoTracking()
                .Where(jg => jg.IsActive)
                .CountAsync();

            _logger?.LogDebug("Total active job groups count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting job groups count");
            throw new InvalidOperationException("Failed to get job groups count", ex);
        }
    }

    /// <summary>
    /// Retrieves job groups with pagination and total count
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="includeJobs">Whether to include jobs in the result</param>
    /// <returns>Paginated job groups result with total count</returns>
    public async Task<(List<JobGroup> JobGroups, int TotalCount)> GetJobGroupsWithCountAsync(int skip = 0, int take = 100, bool includeJobs = false)
    {
        try
        {
            _logger?.LogDebug("Retrieving job groups with skip: {Skip}, take: {Take}, includeJobs: {IncludeJobs}", 
                skip, take, includeJobs);

            // Validate input parameters
            if (skip < 0)
            {
                _logger?.LogWarning("Invalid skip parameter: {Skip}. Setting to 0.", skip);
                skip = 0;
            }

            if (take <= 0 || take > 1000)
            {
                _logger?.LogWarning("Invalid take parameter: {Take}. Setting to 100.", take);
                take = 100;
            }

            // Get total count first
            var totalCount = await GetJobGroupsCountAsync();

            var query = _context.JobGroups.AsNoTracking();

            if (includeJobs)
            {
                _logger?.LogDebug("Including jobs and tasks in the query");
                query = query.Include(jg => jg.JobsCollection)
                             .ThenInclude(j => j.TasksCollection);
            }

            var jobGroups = await query.Where(jg => jg.IsActive)
                                      .OrderBy(jg => jg.CreatedAt)
                                      .Skip(skip)
                                      .Take(take)
                                      .ToListAsync();

            _logger?.LogInformation("Successfully retrieved {Count} job groups out of {TotalCount} total", 
                jobGroups.Count, totalCount);
            
            return (jobGroups, totalCount);
        }
        catch (InvalidOperationException ex)
        {
            _logger?.LogError(ex, "Database operation failed while retrieving job groups");
            throw new InvalidOperationException("Failed to retrieve job groups due to database operation error", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error occurred while retrieving job groups");
            throw new InvalidOperationException("An unexpected error occurred while retrieving job groups", ex);
        }
    }

    /// <summary>
    /// Retrieves all job groups with optional pagination (legacy method for backward compatibility)
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="includeJobs">Whether to include jobs in the result</param>
    /// <returns>List of job groups</returns>
    public async Task<List<JobGroup>> GetJobGroupsAsync(int skip = 0, int take = 100, bool includeJobs = false)
    {
        var (jobGroups, _) = await GetJobGroupsWithCountAsync(skip, take, includeJobs);
        return jobGroups;
    }

    /// <summary>
    /// Retrieves a job group by ID with all related data
    /// </summary>
    /// <param name="jobGroupId">The job group ID</param>
    /// <returns>The job group with all jobs and tasks, or null if not found</returns>
    public async Task<JobGroup?> GetJobGroupByIdAsync(int jobGroupId)
    {
        _logger?.LogDebug("Retrieving job group by ID: {JobGroupId}", jobGroupId);

        return await _context.JobGroups
            .AsNoTracking()
            .Include(jg => jg.JobsCollection)
                .ThenInclude(j => j.TasksCollection)
            .Include(jg => jg.JobsCollection)
                .ThenInclude(j => j.Executions)
            .FirstOrDefaultAsync(jg => jg.Id == jobGroupId && jg.IsActive);
    }

    /// <summary>
    /// Updates an existing job group
    /// </summary>
    /// <param name="jobGroup">The job group to update</param>
    /// <returns>The updated job group</returns>
    public async Task<JobGroup> UpdateJobGroupAsync(JobGroup jobGroup)
    {
        ArgumentNullException.ThrowIfNull(jobGroup);

        _logger?.LogInformation("Updating job group: {JobGroupId}", jobGroup.Id);

        jobGroup.UpdateTimestamp();
        _context.JobGroups.Update(jobGroup);
        await _context.SaveChangesAsync();

        return jobGroup;
    }

    /// <summary>
    /// Soft deletes a job group by marking it as inactive
    /// </summary>
    /// <param name="jobGroupId">The job group ID to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public async Task<bool> DeleteJobGroupAsync(int jobGroupId)
    {
        _logger?.LogInformation("Soft deleting job group: {JobGroupId}", jobGroupId);

        var jobGroup = await _context.JobGroups.FindAsync(jobGroupId);
        if (jobGroup is null)
        {
            _logger?.LogWarning("Job group not found for deletion: {JobGroupId}", jobGroupId);
            return false;
        }

        jobGroup.IsActive = false;
        jobGroup.UpdateTimestamp();
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Successfully soft deleted job group: {JobGroupId}", jobGroupId);
        return true;
    }

    #endregion

    #region Job Operations

    /// <summary>
    /// Creates a new job in the database with all its tasks
    /// </summary>
    /// <param name="job">The job to create</param>
    /// <returns>The created job with assigned ID</returns>
    public async Task<Job> CreateJobAsync(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        _logger?.LogInformation("Creating job: {JobName} with {TaskCount} tasks", 
            job.Name, job.GetTaskCount());

        // Serialize task configuration data
        foreach (var task in job.TasksCollection)
        {
            await SerializeTaskConfigurationAsync(task);
        }

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Created job with ID: {JobId}", job.Id);
        return job;
    }

    /// <summary>
    /// Retrieves a job by ID with all tasks and execution history
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <returns>The job with all related data, or null if not found</returns>
    public async Task<Job?> GetJobByIdAsync(int jobId)
    {
        _logger?.LogDebug("Retrieving job by ID: {JobId}", jobId);

        var job = await _context.Jobs
            .AsNoTracking()
            .Include(j => j.TasksCollection)
            .Include(j => j.Executions)
            .Include(j => j.JobGroup)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.IsActive);

        if (job is not null)
        {
            // Deserialize task configuration data
            foreach (var task in job.TasksCollection)
            {
                await DeserializeTaskConfigurationAsync(task);
            }
        }

        return job;
    }

    /// <summary>
    /// Retrieves jobs by job group with performance optimization
    /// </summary>
    /// <param name="jobGroupId">The job group ID</param>
    /// <param name="includeExecutions">Whether to include execution history</param>
    /// <returns>List of jobs in the job group</returns>
    public async Task<List<Job>> GetJobsByGroupIdAsync(int jobGroupId, bool includeExecutions = false)
    {
        _logger?.LogDebug("Retrieving jobs for job group: {JobGroupId}", jobGroupId);

        var query = _context.Jobs
            .AsNoTracking()
            .Include(j => j.TasksCollection)
            .Where(j => j.JobGroupId == jobGroupId && j.IsActive);

        if (includeExecutions)
        {
            query = query.Include(j => j.Executions);
        }

        var jobs = await query.OrderBy(j => j.ExecutionOrder)
                             .ToListAsync();

        // Deserialize task configurations
        foreach (var job in jobs)
        {
            foreach (var task in job.TasksCollection)
            {
                await DeserializeTaskConfigurationAsync(task);
            }
        }

        return jobs;
    }

    #endregion

    #region JobExecution Operations

    /// <summary>
    /// Creates a new job execution record
    /// </summary>
    /// <param name="jobExecution">The job execution to create</param>
    /// <returns>The created job execution with assigned ID</returns>
    public async Task<JobExecution> CreateJobExecutionAsync(JobExecution jobExecution)
    {
        ArgumentNullException.ThrowIfNull(jobExecution);

        _logger?.LogInformation("Creating job execution for job: {JobId}", jobExecution.JobId);

        _context.JobExecutions.Add(jobExecution);
        await _context.SaveChangesAsync();

        return jobExecution;
    }

    /// <summary>
    /// Updates an existing job execution record
    /// </summary>
    /// <param name="jobExecution">The job execution to update</param>
    /// <returns>The updated job execution</returns>
    public async Task<JobExecution> UpdateJobExecutionAsync(JobExecution jobExecution)
    {
        ArgumentNullException.ThrowIfNull(jobExecution);

        _context.JobExecutions.Update(jobExecution);
        await _context.SaveChangesAsync();

        return jobExecution;
    }

    /// <summary>
    /// Retrieves execution history for a job with pagination
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <returns>List of job executions</returns>
    public async Task<List<JobExecution>> GetJobExecutionHistoryAsync(int jobId, int skip = 0, int take = 50)
    {
        _logger?.LogDebug("Retrieving execution history for job: {JobId}", jobId);

        return await _context.JobExecutions
            .AsNoTracking()
            .Include(je => je.Job)
            .Where(je => je.JobId == jobId)
            .OrderByDescending(je => je.StartedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>
    /// Gets currently running job executions
    /// </summary>
    /// <returns>List of running job executions</returns>
    public async Task<List<JobExecution>> GetRunningJobExecutionsAsync()
    {
        _logger?.LogDebug("Retrieving currently running job executions");

        return await _context.JobExecutions
            .AsNoTracking()
            .Include(je => je.Job)
            .Where(je => je.Status == JobExecutionStatus.Running)
            .OrderBy(je => je.StartedAt)
            .ToListAsync();
    }

    #endregion

    #region Performance and Analytics

    /// <summary>
    /// Gets job execution statistics for performance analysis
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="fromDate">Start date for analysis</param>
    /// <param name="toDate">End date for analysis</param>
    /// <returns>Job execution statistics</returns>
    public async Task<JobExecutionStatistics> GetJobExecutionStatisticsAsync(
        int jobId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        fromDate ??= DateTime.UtcNow.AddDays(-30);
        toDate ??= DateTime.UtcNow;

        _logger?.LogDebug("Getting execution statistics for job: {JobId} from {FromDate} to {ToDate}", 
            jobId, fromDate, toDate);

        var executions = await _context.JobExecutions
            .AsNoTracking()
            .Where(je => je.JobId == jobId && 
                        je.StartedAt >= fromDate && 
                        je.StartedAt <= toDate)
            .ToListAsync();

        return CalculateJobStatistics(executions);
    }

    /// <summary>
    /// Gets system-wide job execution statistics
    /// </summary>
    /// <param name="fromDate">Start date for analysis</param>
    /// <param name="toDate">End date for analysis</param>
    /// <returns>System execution statistics</returns>
    public async Task<SystemExecutionStatistics> GetSystemExecutionStatisticsAsync(
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        fromDate ??= DateTime.UtcNow.AddDays(-7);
        toDate ??= DateTime.UtcNow;

        _logger?.LogDebug("Getting system execution statistics from {FromDate} to {ToDate}", 
            fromDate, toDate);

        var executions = await _context.JobExecutions
            .AsNoTracking()
            .Include(je => je.Job)
            .Where(je => je.StartedAt >= fromDate && je.StartedAt <= toDate)
            .ToListAsync();

        return CalculateSystemStatistics(executions);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Serializes task configuration data for database storage
    /// </summary>
    /// <param name="task">The task to serialize configuration for</param>
    private async Task SerializeTaskConfigurationAsync(JobTask task)
    {
        if (task.ConfigurationData is null)
        {
            // Create configuration based on task type
            object config = task.TaskType switch
            {
                "FetchApiData" => new { TaskType = task.TaskType, ApiUrl = "" },
                "CreateFile" => new { TaskType = task.TaskType, FilePath = "" },
                "UploadSftp" => new { TaskType = task.TaskType, Host = "", LocalPath = "", RemotePath = "" },
                _ => new { TaskType = task.TaskType }
            };

            task.ConfigurationData = JsonSerializer.Serialize(config);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Deserializes task configuration data from database
    /// </summary>
    /// <param name="task">The task to deserialize configuration for</param>
    private async Task DeserializeTaskConfigurationAsync(JobTask task)
    {
        if (!string.IsNullOrEmpty(task.ConfigurationData))
        {
            try
            {
                // Configuration is already stored as JSON, can be used by execution logic
                _logger?.LogDebug("Task {TaskId} configuration loaded: {ConfigLength} characters", 
                    task.Id, task.ConfigurationData.Length);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Failed to deserialize configuration for task {TaskId}", task.Id);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Calculates job execution statistics from execution data
    /// </summary>
    /// <param name="executions">The execution data</param>
    /// <returns>Calculated statistics</returns>
    private static JobExecutionStatistics CalculateJobStatistics(List<JobExecution> executions)
    {
        return new JobExecutionStatistics
        {
            TotalExecutions = executions.Count,
            SuccessfulExecutions = executions.Count(e => e.Status == JobExecutionStatus.Completed),
            FailedExecutions = executions.Count(e => e.Status == JobExecutionStatus.Failed),
            CancelledExecutions = executions.Count(e => e.Status == JobExecutionStatus.Cancelled),
            AverageDurationMs = executions.Where(e => e.DurationMs.HasValue)
                                         .Select(e => e.DurationMs!.Value)
                                         .DefaultIfEmpty(0)
                                         .Average(),
            MaxDurationMs = executions.Where(e => e.DurationMs.HasValue)
                                     .Select(e => e.DurationMs!.Value)
                                     .DefaultIfEmpty(0)
                                     .Max(),
            MinDurationMs = executions.Where(e => e.DurationMs.HasValue)
                                     .Select(e => e.DurationMs!.Value)
                                     .DefaultIfEmpty(0)
                                     .Min()
        };
    }

    /// <summary>
    /// Calculates system-wide execution statistics
    /// </summary>
    /// <param name="executions">The execution data</param>
    /// <returns>Calculated system statistics</returns>
    private static SystemExecutionStatistics CalculateSystemStatistics(List<JobExecution> executions)
    {
        var jobGroups = executions.Select(e => e.Job?.JobType ?? "Unknown").Distinct();

        return new SystemExecutionStatistics
        {
            TotalJobs = executions.Select(e => e.JobId).Distinct().Count(),
            TotalExecutions = executions.Count,
            ActiveJobTypes = jobGroups.Count(),
            SystemSuccessRate = executions.Count > 0 
                ? (double)executions.Count(e => e.Status == JobExecutionStatus.Completed) / executions.Count * 100
                : 0,
            JobTypeStatistics = jobGroups.ToDictionary(
                jt => jt,
                jt => CalculateJobStatistics(executions.Where(e => e.Job?.JobType == jt).ToList())
            )
        };
    }

    #endregion
}

/// <summary>
/// Statistics for job execution performance analysis
/// </summary>
public sealed class JobExecutionStatistics
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int CancelledExecutions { get; set; }
    public double AverageDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;
}

/// <summary>
/// System-wide execution statistics
/// </summary>
public sealed class SystemExecutionStatistics
{
    public int TotalJobs { get; set; }
    public int TotalExecutions { get; set; }
    public int ActiveJobTypes { get; set; }
    public double SystemSuccessRate { get; set; }
    public Dictionary<string, JobExecutionStatistics> JobTypeStatistics { get; set; } = new();
}