using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents a task within a job that can be executed
/// </summary>
[Table("JobTasks")]
public sealed class JobTask
{
    /// <summary>
    /// Gets or sets the unique identifier for the task
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the task
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the task
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the task type (e.g., "FetchApiData", "CreateFile", "UploadSftp")
    /// </summary>
    [MaxLength(100)]
    public string TaskType { get; set; } = "General";

    /// <summary>
    /// Gets or sets the execution order within the job
    /// </summary>
    public int ExecutionOrder { get; set; } = 1;

    /// <summary>
    /// Gets or sets the configuration data for the task (serialized JSON)
    /// </summary>
    [MaxLength(4000)]
    public string? ConfigurationData { get; set; }

    /// <summary>
    /// Gets or sets the retry count for failed tasks
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout in seconds for task execution
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes default

    /// <summary>
    /// Gets or sets the created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the task is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the job
    /// </summary>
    [ForeignKey(nameof(Job))]
    public int? JobId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the job
    /// </summary>
    public Job? Job { get; set; }

    /// <summary>
    /// Gets or sets the function to execute on successful completion
    /// </summary>
    [NotMapped]
    public Func<Task<bool>>? OnSuccess { get; set; }

    /// <summary>
    /// Gets or sets the function to execute on failure
    /// </summary>
    [NotMapped]
    public Func<Task<bool>>? OnFailure { get; set; }

    /// <summary>
    /// Gets or sets the task execution function
    /// </summary>
    [NotMapped]
    public Func<Task<bool>>? ExecuteFunction { get; set; }

    /// <summary>
    /// Initializes a new instance of the JobTask class
    /// </summary>
    public JobTask()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Initializes a new instance of the JobTask class with specified parameters
    /// </summary>
    /// <param name="id">The unique identifier for the task</param>
    /// <param name="name">The name of the task</param>
    /// <param name="taskType">The type of the task</param>
    /// <param name="executeFunction">The function to execute for this task</param>
    /// <param name="onSuccess">The function to execute on success</param>
    /// <param name="onFailure">The function to execute on failure</param>
    public JobTask(int id, string name, string taskType = "General", 
        Func<Task<bool>>? executeFunction = null, 
        Func<Task<bool>>? onSuccess = null, 
        Func<Task<bool>>? onFailure = null) : this()
    {
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        Name = name;
        TaskType = taskType;
        ExecuteFunction = executeFunction;
        OnSuccess = onSuccess;
        OnFailure = onFailure;
    }

    /// <summary>
    /// Executes the task and invokes appropriate callbacks based on the result
    /// </summary>
    /// <returns>True if execution was successful, false otherwise</returns>
    public async Task<bool> ExecuteAsync()
    {
        try
        {
            var executionResult = await ExecuteTaskLogicAsync();
            
            if (executionResult)
            {
                return await InvokeSuccessCallbackAsync();
            }
            else
            {
                return await InvokeFailureCallbackAsync();
            }
        }
        catch
        {
            return await InvokeFailureCallbackAsync();
        }
    }

    /// <summary>
    /// Updates the timestamp when the task is modified
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an API data fetch task
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="name">The task name</param>
    /// <param name="apiUrl">The API URL to fetch from</param>
    /// <param name="executeFunction">The execution function</param>
    /// <returns>A new API fetch task</returns>
    public static JobTask CreateApiFetchTask(int id, string name, string apiUrl, 
        Func<Task<bool>>? executeFunction = null)
    {
        var task = new JobTask(id, name, "FetchApiData", executeFunction);
        task.ConfigurationData = System.Text.Json.JsonSerializer.Serialize(new { ApiUrl = apiUrl });
        return task;
    }

    /// <summary>
    /// Creates a file creation task
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="name">The task name</param>
    /// <param name="filePath">The file path to create</param>
    /// <param name="executeFunction">The execution function</param>
    /// <returns>A new file creation task</returns>
    public static JobTask CreateFileTask(int id, string name, string filePath, 
        Func<Task<bool>>? executeFunction = null)
    {
        var task = new JobTask(id, name, "CreateFile", executeFunction);
        task.ConfigurationData = System.Text.Json.JsonSerializer.Serialize(new { FilePath = filePath });
        return task;
    }

    /// <summary>
    /// Creates an SFTP upload task
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="name">The task name</param>
    /// <param name="host">The SFTP host</param>
    /// <param name="localPath">The local file path</param>
    /// <param name="remotePath">The remote file path</param>
    /// <param name="executeFunction">The execution function</param>
    /// <returns>A new SFTP upload task</returns>
    public static JobTask CreateSftpUploadTask(int id, string name, string host, 
        string localPath, string remotePath, Func<Task<bool>>? executeFunction = null)
    {
        var task = new JobTask(id, name, "UploadSftp", executeFunction);
        task.ConfigurationData = System.Text.Json.JsonSerializer.Serialize(new 
        { 
            Host = host, 
            LocalPath = localPath, 
            RemotePath = remotePath 
        });
        return task;
    }

    /// <summary>
    /// Executes the core task logic
    /// </summary>
    /// <returns>True if execution was successful, false otherwise</returns>
    private async Task<bool> ExecuteTaskLogicAsync()
    {
        if (ExecuteFunction is not null)
        {
            return await ExecuteFunction();
        }
        
        // Default implementation returns true
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Invokes the success callback if available
    /// </summary>
    /// <returns>True if callback execution was successful, false otherwise</returns>
    private async Task<bool> InvokeSuccessCallbackAsync()
    {
        if (OnSuccess is not null)
        {
            return await OnSuccess();
        }
        
        return true;
    }

    /// <summary>
    /// Invokes the failure callback if available
    /// </summary>
    /// <returns>False to indicate failure handling</returns>
    private async Task<bool> InvokeFailureCallbackAsync()
    {
        if (OnFailure is not null)
        {
            await OnFailure();
        }
        
        return false;
    }
}