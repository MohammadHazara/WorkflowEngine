using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents a job containing multiple tasks that can be executed sequentially
/// </summary>
[Table("Jobs")]
public sealed class Job
{
    /// <summary>
    /// Gets or sets the unique identifier for the job
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the job
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the job
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the job type (e.g., "DataPipeline", "FileProcessing", "ApiIntegration")
    /// </summary>
    [MaxLength(100)]
    public string JobType { get; set; } = "General";

    /// <summary>
    /// Gets or sets the execution order within the job group
    /// </summary>
    public int ExecutionOrder { get; set; } = 1;

    /// <summary>
    /// Gets or sets the created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the job is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the job group
    /// </summary>
    [ForeignKey(nameof(JobGroup))]
    public int? JobGroupId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the job group
    /// </summary>
    public JobGroup? JobGroup { get; set; }

    private List<JobTask>? _tasks;

    /// <summary>
    /// Gets the read-only view of tasks
    /// </summary>
    [NotMapped]
    public IReadOnlyList<JobTask> Tasks => (_tasks ??= TasksCollection?.ToList() ?? new List<JobTask>()).AsReadOnly();

    /// <summary>
    /// Navigation property for Entity Framework
    /// </summary>
    public ICollection<JobTask> TasksCollection { get; set; } = new List<JobTask>();

    /// <summary>
    /// Navigation property for job executions
    /// </summary>
    public ICollection<JobExecution> Executions { get; set; } = new List<JobExecution>();

    /// <summary>
    /// Initializes a new instance of the Job class
    /// </summary>
    public Job()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Initializes a new instance of the Job class with specified parameters
    /// </summary>
    /// <param name="name">The name of the job</param>
    /// <param name="description">The description of the job</param>
    /// <param name="jobType">The type of the job</param>
    public Job(string name, string? description = null, string jobType = "General") : this()
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Description = description;
        JobType = jobType;
    }

    /// <summary>
    /// Adds a task to the job
    /// </summary>
    /// <param name="task">The task to add</param>
    /// <exception cref="ArgumentNullException">Thrown when task is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when task with same ID already exists</exception>
    public void AddTask(JobTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (ContainsTaskWithId(task.Id))
        {
            throw new InvalidOperationException($"Task with ID {task.Id} already exists in the job");
        }

        task.JobId = Id;
        TasksCollection.Add(task);
        
        // Synchronize private collection
        _tasks?.Add(task);
        
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a task from the job by ID
    /// </summary>
    /// <param name="taskId">The ID of the task to remove</param>
    /// <returns>True if task was removed, false if not found</returns>
    public bool RemoveTask(int taskId)
    {
        var taskToRemove = FindTaskById(taskId);
        if (taskToRemove is null)
        {
            return false;
        }

        var removed = TasksCollection.Remove(taskToRemove);
        if (removed)
        {
            _tasks?.Remove(taskToRemove);
            UpdateTimestamp();
        }

        return removed;
    }

    /// <summary>
    /// Gets a task by its ID
    /// </summary>
    /// <param name="taskId">The ID of the task to retrieve</param>
    /// <returns>The task if found, null otherwise</returns>
    public JobTask? GetTaskById(int taskId)
    {
        return FindTaskById(taskId);
    }

    /// <summary>
    /// Gets the total number of tasks in the job
    /// </summary>
    /// <returns>The number of tasks</returns>
    public int GetTaskCount()
    {
        return TasksCollection.Count;
    }

    /// <summary>
    /// Checks if the job contains any tasks
    /// </summary>
    /// <returns>True if job has tasks, false otherwise</returns>
    public bool HasTasks()
    {
        return TasksCollection.Count > 0;
    }

    /// <summary>
    /// Clears all tasks from the job
    /// </summary>
    public void ClearTasks()
    {
        TasksCollection.Clear();
        _tasks?.Clear();
        UpdateTimestamp();
    }

    /// <summary>
    /// Executes all tasks in the job sequentially
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if all tasks executed successfully, false otherwise</returns>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!HasTasks())
        {
            return true;
        }

        foreach (var task in TasksCollection.OrderBy(t => t.ExecutionOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskResult = await ExecuteTaskSafelyAsync(task);
            if (!taskResult)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Updates the timestamp when the job is modified
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the job configuration
    /// </summary>
    /// <returns>True if job is valid, false otherwise</returns>
    public bool ValidateJob()
    {
        return !string.IsNullOrWhiteSpace(Name) && IsActive;
    }

    /// <summary>
    /// Creates a data pipeline job with common tasks
    /// </summary>
    /// <param name="name">The name of the pipeline job</param>
    /// <param name="description">The description of the pipeline job</param>
    /// <returns>A new data pipeline job</returns>
    public static Job CreateDataPipelineJob(string name, string? description = null)
    {
        return new Job(name, description, "DataPipeline");
    }

    /// <summary>
    /// Creates an API integration job
    /// </summary>
    /// <param name="name">The name of the API job</param>
    /// <param name="description">The description of the API job</param>
    /// <returns>A new API integration job</returns>
    public static Job CreateApiIntegrationJob(string name, string? description = null)
    {
        return new Job(name, description, "ApiIntegration");
    }

    /// <summary>
    /// Creates a file processing job
    /// </summary>
    /// <param name="name">The name of the file processing job</param>
    /// <param name="description">The description of the file processing job</param>
    /// <returns>A new file processing job</returns>
    public static Job CreateFileProcessingJob(string name, string? description = null)
    {
        return new Job(name, description, "FileProcessing");
    }

    /// <summary>
    /// Finds a task by its ID
    /// </summary>
    /// <param name="taskId">The ID to search for</param>
    /// <returns>The task if found, null otherwise</returns>
    private JobTask? FindTaskById(int taskId)
    {
        return TasksCollection.FirstOrDefault(t => t.Id == taskId);
    }

    /// <summary>
    /// Checks if a task with the given ID exists
    /// </summary>
    /// <param name="taskId">The ID to check</param>
    /// <returns>True if task exists, false otherwise</returns>
    private bool ContainsTaskWithId(int taskId)
    {
        return TasksCollection.Any(t => t.Id == taskId);
    }

    /// <summary>
    /// Executes a task with error handling
    /// </summary>
    /// <param name="task">The task to execute</param>
    /// <returns>True if execution was successful, false otherwise</returns>
    private static async Task<bool> ExecuteTaskSafelyAsync(JobTask task)
    {
        try
        {
            return await task.ExecuteAsync();
        }
        catch
        {
            return false;
        }
    }
}