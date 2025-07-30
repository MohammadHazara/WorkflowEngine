using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Controllers;

/// <summary>
/// API controller for managing hierarchical job system operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class JobManagementController : ControllerBase
{
    private readonly JobRepositoryService _jobRepository;
    private readonly JobBuilder _jobBuilder;
    private readonly JobExecutor _jobExecutor;
    private readonly ILogger<JobManagementController> _logger;

    /// <summary>
    /// Initializes a new instance of the JobManagementController class
    /// </summary>
    /// <param name="jobRepository">The job repository service</param>
    /// <param name="jobBuilder">The job builder service</param>
    /// <param name="jobExecutor">The job executor service</param>
    /// <param name="logger">The logger instance</param>
    public JobManagementController(
        JobRepositoryService jobRepository,
        JobBuilder jobBuilder,
        JobExecutor jobExecutor,
        ILogger<JobManagementController> logger)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _jobBuilder = jobBuilder ?? throw new ArgumentNullException(nameof(jobBuilder));
        _jobExecutor = jobExecutor ?? throw new ArgumentNullException(nameof(jobExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Job Group Operations

    /// <summary>
    /// Retrieves all job groups with optional pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeJobs">Whether to include jobs in response</param>
    /// <returns>Paginated list of job groups</returns>
    [HttpGet("job-groups")]
    public async Task<ActionResult<JobGroupResponse>> GetJobGroupsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeJobs = true)
    {
        try
        {
            _logger.LogInformation("Retrieving job groups - Page: {Page}, PageSize: {PageSize}, IncludeJobs: {IncludeJobs}", 
                page, pageSize, includeJobs);

            // Validate pagination parameters
            if (page < 1)
            {
                page = 1;
                _logger.LogWarning("Invalid page parameter, setting to 1");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                pageSize = 20;
                _logger.LogWarning("Invalid pageSize parameter, setting to 20");
            }

            var skip = (page - 1) * pageSize;
            var (jobGroups, totalCount) = await _jobRepository.GetJobGroupsWithCountAsync(skip, pageSize, includeJobs);

            var response = new JobGroupResponse
            {
                JobGroups = jobGroups.Select(MapToJobGroupDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            _logger.LogInformation("Successfully retrieved {Count} job groups out of {TotalCount} total for page {Page}", 
                jobGroups.Count, totalCount, page);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job groups");
            return StatusCode(500, "Internal server error while retrieving job groups");
        }
    }

    /// <summary>
    /// Retrieves a specific job group by ID
    /// </summary>
    /// <param name="id">The job group ID</param>
    /// <returns>The job group with all related data</returns>
    [HttpGet("job-groups/{id:int}")]
    public async Task<ActionResult<JobGroupDto>> GetJobGroupByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving job group by ID: {JobGroupId}", id);

            var jobGroup = await _jobRepository.GetJobGroupByIdAsync(id);
            if (jobGroup is null)
            {
                return NotFound($"Job group with ID {id} not found");
            }

            return Ok(MapToJobGroupDto(jobGroup));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job group {JobGroupId}", id);
            return StatusCode(500, "Internal server error while retrieving job group");
        }
    }

    /// <summary>
    /// Creates a new job group
    /// </summary>
    /// <param name="request">The job group creation request</param>
    /// <returns>The created job group</returns>
    [HttpPost("job-groups")]
    public async Task<ActionResult<JobGroupDto>> CreateJobGroupAsync([FromBody] CreateJobGroupRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating job group: {JobGroupName}", request.Name);

            var jobGroup = _jobBuilder.CreateJobGroup(request.Name, request.Description);
            var savedJobGroup = await _jobRepository.CreateJobGroupAsync(jobGroup);

            var response = MapToJobGroupDto(savedJobGroup);
            return CreatedAtAction(nameof(GetJobGroupByIdAsync), new { id = savedJobGroup.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job group");
            return StatusCode(500, "Internal server error while creating job group");
        }
    }

    #endregion

    #region Job Operations

    /// <summary>
    /// Retrieves jobs for a specific job group
    /// </summary>
    /// <param name="jobGroupId">The job group ID</param>
    /// <param name="includeExecutions">Whether to include execution history</param>
    /// <returns>List of jobs in the job group</returns>
    [HttpGet("job-groups/{jobGroupId:int}/jobs")]
    public async Task<ActionResult<List<JobDto>>> GetJobsByGroupIdAsync(
        int jobGroupId,
        [FromQuery] bool includeExecutions = false)
    {
        try
        {
            _logger.LogInformation("Retrieving jobs for job group: {JobGroupId}", jobGroupId);

            var jobs = await _jobRepository.GetJobsByGroupIdAsync(jobGroupId, includeExecutions);
            var response = jobs.Select(MapToJobDto).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs for job group {JobGroupId}", jobGroupId);
            return StatusCode(500, "Internal server error while retrieving jobs");
        }
    }

    /// <summary>
    /// Retrieves a specific job by ID
    /// </summary>
    /// <param name="id">The job ID</param>
    /// <returns>The job with all tasks and execution history</returns>
    [HttpGet("jobs/{id:int}")]
    public async Task<ActionResult<JobDto>> GetJobByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving job by ID: {JobId}", id);

            var job = await _jobRepository.GetJobByIdAsync(id);
            if (job is null)
            {
                return NotFound($"Job with ID {id} not found");
            }

            return Ok(MapToJobDto(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job {JobId}", id);
            return StatusCode(500, "Internal server error while retrieving job");
        }
    }

    /// <summary>
    /// Creates a new job in a job group
    /// </summary>
    /// <param name="request">The job creation request</param>
    /// <returns>The created job</returns>
    [HttpPost("jobs")]
    public async Task<ActionResult<JobDto>> CreateJobAsync([FromBody] CreateJobRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating job: {JobName} in group: {JobGroupId}", request.Name, request.JobGroupId);

            var job = CreateJobFromRequest(request);
            var savedJob = await _jobRepository.CreateJobAsync(job);

            var response = MapToJobDto(savedJob);
            return CreatedAtAction(nameof(GetJobByIdAsync), new { id = savedJob.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, "Internal server error while creating job");
        }
    }

    /// <summary>
    /// Executes a job by ID
    /// </summary>
    /// <param name="id">The job ID to execute</param>
    /// <returns>The job execution result</returns>
    [HttpPost("jobs/{id:int}/execute")]
    public async Task<ActionResult<JobExecutionDto>> ExecuteJobAsync(int id)
    {
        try
        {
            _logger.LogInformation("Executing job: {JobId}", id);

            var job = await _jobRepository.GetJobByIdAsync(id);
            if (job is null)
            {
                return NotFound($"Job with ID {id} not found");
            }

            var execution = await _jobExecutor.ExecuteJobAsync(job);
            var savedExecution = await _jobRepository.CreateJobExecutionAsync(execution);

            return Ok(MapToJobExecutionDto(savedExecution));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobId}", id);
            return StatusCode(500, "Internal server error while executing job");
        }
    }

    #endregion

    #region Task Templates

    /// <summary>
    /// Gets available task templates for creating new tasks
    /// </summary>
    /// <returns>List of available task templates</returns>
    [HttpGet("task-templates")]
    public ActionResult<List<TaskTemplateDto>> GetTaskTemplatesAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving task templates");

            var templates = GetAvailableTaskTemplates();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task templates");
            return StatusCode(500, "Internal server error while retrieving task templates");
        }
    }

    #endregion

    #region Job Execution History

    /// <summary>
    /// Gets execution history for a specific job
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated execution history</returns>
    [HttpGet("jobs/{jobId:int}/executions")]
    public async Task<ActionResult<JobExecutionHistoryResponse>> GetJobExecutionHistoryAsync(
        int jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving execution history for job: {JobId}", jobId);

            var skip = (page - 1) * pageSize;
            var executions = await _jobRepository.GetJobExecutionHistoryAsync(jobId, skip, pageSize);

            var response = new JobExecutionHistoryResponse
            {
                Executions = executions.Select(MapToJobExecutionDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = executions.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving execution history for job {JobId}", jobId);
            return StatusCode(500, "Internal server error while retrieving execution history");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Maps a JobGroup entity to a JobGroupDto
    /// </summary>
    /// <param name="jobGroup">The job group entity</param>
    /// <returns>The job group DTO</returns>
    private static JobGroupDto MapToJobGroupDto(JobGroup jobGroup)
    {
        return new JobGroupDto
        {
            Id = jobGroup.Id,
            Name = jobGroup.Name,
            Description = jobGroup.Description,
            IsActive = jobGroup.IsActive,
            CreatedAt = jobGroup.CreatedAt,
            UpdatedAt = jobGroup.UpdatedAt,
            Jobs = jobGroup.JobsCollection?.Select(MapToJobDto).ToList() ?? []
        };
    }

    /// <summary>
    /// Maps a Job entity to a JobDto
    /// </summary>
    /// <param name="job">The job entity</param>
    /// <returns>The job DTO</returns>
    private static JobDto MapToJobDto(Job job)
    {
        return new JobDto
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            JobType = job.JobType,
            ExecutionOrder = job.ExecutionOrder,
            IsActive = job.IsActive,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            JobGroupId = job.JobGroupId,
            Tasks = job.TasksCollection?.Select(MapToJobTaskDto).ToList() ?? [],
            RecentExecutions = job.Executions?.OrderByDescending(e => e.StartedAt)
                                             .Take(5)
                                             .Select(MapToJobExecutionDto)
                                             .ToList() ?? []
        };
    }

    /// <summary>
    /// Maps a JobTask entity to a JobTaskDto
    /// </summary>
    /// <param name="task">The job task entity</param>
    /// <returns>The job task DTO</returns>
    private static JobTaskDto MapToJobTaskDto(JobTask task)
    {
        return new JobTaskDto
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            TaskType = task.TaskType,
            ExecutionOrder = task.ExecutionOrder,
            ConfigurationData = task.ConfigurationData,
            MaxRetries = task.MaxRetries,
            TimeoutSeconds = task.TimeoutSeconds,
            IsActive = task.IsActive,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a JobExecution entity to a JobExecutionDto
    /// </summary>
    /// <param name="execution">The job execution entity</param>
    /// <returns>The job execution DTO</returns>
    private static JobExecutionDto MapToJobExecutionDto(JobExecution execution)
    {
        return new JobExecutionDto
        {
            Id = execution.Id,
            JobId = execution.JobId,
            Status = execution.Status.ToString(),
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            DurationMs = execution.DurationMs,
            CurrentTaskIndex = execution.CurrentTaskIndex,
            TotalTasks = execution.TotalTasks,
            ProgressPercentage = execution.ProgressPercentage,
            ErrorMessage = execution.ErrorMessage
        };
    }

    /// <summary>
    /// Creates a Job entity from a CreateJobRequest
    /// </summary>
    /// <param name="request">The job creation request</param>
    /// <returns>The created job entity</returns>
    private static Job CreateJobFromRequest(CreateJobRequest request)
    {
        var job = new Job(request.Name, request.Description, request.JobType)
        {
            JobGroupId = request.JobGroupId,
            ExecutionOrder = request.ExecutionOrder
        };

        foreach (var taskRequest in request.Tasks.OrderBy(t => t.ExecutionOrder))
        {
            var task = new JobTask(
                taskRequest.Id,
                taskRequest.Name,
                taskRequest.TaskType)
            {
                Description = taskRequest.Description,
                ExecutionOrder = taskRequest.ExecutionOrder,
                ConfigurationData = taskRequest.ConfigurationData,
                MaxRetries = taskRequest.MaxRetries,
                TimeoutSeconds = taskRequest.TimeoutSeconds
            };

            job.AddTask(task);
        }

        return job;
    }

    /// <summary>
    /// Gets available task templates for the UI
    /// </summary>
    /// <returns>List of task templates</returns>
    private static List<TaskTemplateDto> GetAvailableTaskTemplates()
    {
        return
        [
            new TaskTemplateDto
            {
                TaskType = "FetchApiData",
                Name = "Fetch API Data",
                Description = "Retrieve data from a REST API endpoint",
                ConfigurationSchema = """
                {
                    "type": "object",
                    "properties": {
                        "apiUrl": { "type": "string", "title": "API URL" },
                        "timeoutSeconds": { "type": "number", "title": "Timeout (seconds)", "default": 30 },
                        "maxRetries": { "type": "number", "title": "Max Retries", "default": 3 }
                    },
                    "required": ["apiUrl"]
                }
                """
            },
            new TaskTemplateDto
            {
                TaskType = "CreateFile",
                Name = "Create File",
                Description = "Create a file from data",
                ConfigurationSchema = """
                {
                    "type": "object",
                    "properties": {
                        "filePath": { "type": "string", "title": "File Path" },
                        "overwriteExisting": { "type": "boolean", "title": "Overwrite Existing", "default": true }
                    },
                    "required": ["filePath"]
                }
                """
            },
            new TaskTemplateDto
            {
                TaskType = "CompressFile",
                Name = "Compress File",
                Description = "Compress files to ZIP format",
                ConfigurationSchema = """
                {
                    "type": "object",
                    "properties": {
                        "sourceFilePath": { "type": "string", "title": "Source File Path" },
                        "zipFilePath": { "type": "string", "title": "ZIP File Path" },
                        "deleteSourceAfterCompression": { "type": "boolean", "title": "Delete Source", "default": false }
                    },
                    "required": ["sourceFilePath", "zipFilePath"]
                }
                """
            },
            new TaskTemplateDto
            {
                TaskType = "UploadSftp",
                Name = "Upload via SFTP",
                Description = "Upload files via secure FTP",
                ConfigurationSchema = """
                {
                    "type": "object",
                    "properties": {
                        "host": { "type": "string", "title": "SFTP Host" },
                        "username": { "type": "string", "title": "Username" },
                        "password": { "type": "string", "title": "Password", "format": "password" },
                        "localFilePath": { "type": "string", "title": "Local File Path" },
                        "remoteDirectoryPath": { "type": "string", "title": "Remote Directory" }
                    },
                    "required": ["host", "username", "password", "localFilePath", "remoteDirectoryPath"]
                }
                """
            },
            new TaskTemplateDto
            {
                TaskType = "General",
                Name = "General Task",
                Description = "Custom general-purpose task",
                ConfigurationSchema = """
                {
                    "type": "object",
                    "properties": {
                        "customConfig": { "type": "string", "title": "Custom Configuration" }
                    }
                }
                """
            }
        ];
    }

    #endregion
}

#region DTOs and Request Models

/// <summary>
/// Data transfer object for job group information
/// </summary>
public sealed class JobGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<JobDto> Jobs { get; set; } = [];
}

/// <summary>
/// Data transfer object for job information
/// </summary>
public sealed class JobDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string JobType { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? JobGroupId { get; set; }
    public List<JobTaskDto> Tasks { get; set; } = [];
    public List<JobExecutionDto> RecentExecutions { get; set; } = [];
}

/// <summary>
/// Data transfer object for job task information
/// </summary>
public sealed class JobTaskDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
    public string? ConfigurationData { get; set; }
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for job execution information
/// </summary>
public sealed class JobExecutionDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public int CurrentTaskIndex { get; set; }
    public int TotalTasks { get; set; }
    public int ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Data transfer object for task templates
/// </summary>
public sealed class TaskTemplateDto
{
    public string TaskType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ConfigurationSchema { get; set; } = string.Empty;
}

/// <summary>
/// Request model for creating a new job group
/// </summary>
public sealed class CreateJobGroupRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request model for creating a new job
/// </summary>
public sealed class CreateJobRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string JobType { get; set; } = "General";
    public int JobGroupId { get; set; }
    public int ExecutionOrder { get; set; } = 1;
    public List<CreateJobTaskRequest> Tasks { get; set; } = [];
}

/// <summary>
/// Request model for creating a new job task
/// </summary>
public sealed class CreateJobTaskRequest
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string TaskType { get; set; } = "General";
    public int ExecutionOrder { get; set; } = 1;
    public string? ConfigurationData { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Response model for paginated job groups
/// </summary>
public sealed class JobGroupResponse
{
    public List<JobGroupDto> JobGroups { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// Response model for job execution history
/// </summary>
public sealed class JobExecutionHistoryResponse
{
    public List<JobExecutionDto> Executions { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

#endregion