using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents a group of related jobs that can be organized together
/// </summary>
[Table("JobGroups")]
public sealed class JobGroup
{
    /// <summary>
    /// Gets or sets the unique identifier for the job group
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the job group
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the job group
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the job group is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the collection of jobs in this group
    /// </summary>
    private readonly List<Job> _jobs;

    /// <summary>
    /// Gets the read-only view of jobs
    /// </summary>
    [NotMapped]
    public IReadOnlyList<Job> Jobs => _jobs.AsReadOnly();

    /// <summary>
    /// Navigation property for Entity Framework
    /// </summary>
    public ICollection<Job> JobsCollection { get; set; } = new List<Job>();

    /// <summary>
    /// Initializes a new instance of the JobGroup class
    /// </summary>
    public JobGroup()
    {
        _jobs = new List<Job>();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Initializes a new instance of the JobGroup class with specified parameters
    /// </summary>
    /// <param name="name">The name of the job group</param>
    /// <param name="description">The description of the job group</param>
    public JobGroup(string name, string? description = null) : this()
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Adds a job to the group
    /// </summary>
    /// <param name="job">The job to add</param>
    /// <exception cref="ArgumentNullException">Thrown when job is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when job with same ID already exists</exception>
    public void AddJob(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (ContainsJobWithId(job.Id))
        {
            throw new InvalidOperationException($"Job with ID {job.Id} already exists in the job group");
        }

        job.JobGroupId = Id;
        _jobs.Add(job);
        JobsCollection.Add(job);
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a job from the group by ID
    /// </summary>
    /// <param name="jobId">The ID of the job to remove</param>
    /// <returns>True if job was removed, false if not found</returns>
    public bool RemoveJob(int jobId)
    {
        var jobToRemove = FindJobById(jobId);
        if (jobToRemove is null)
        {
            return false;
        }

        var removed = _jobs.Remove(jobToRemove) && JobsCollection.Remove(jobToRemove);
        if (removed)
        {
            UpdateTimestamp();
        }

        return removed;
    }

    /// <summary>
    /// Gets a job by its ID
    /// </summary>
    /// <param name="jobId">The ID of the job to retrieve</param>
    /// <returns>The job if found, null otherwise</returns>
    public Job? GetJobById(int jobId)
    {
        return FindJobById(jobId);
    }

    /// <summary>
    /// Gets the total number of jobs in the group
    /// </summary>
    /// <returns>The number of jobs</returns>
    public int GetJobCount()
    {
        return _jobs.Count;
    }

    /// <summary>
    /// Checks if the job group contains any jobs
    /// </summary>
    /// <returns>True if job group has jobs, false otherwise</returns>
    public bool HasJobs()
    {
        return _jobs.Count > 0;
    }

    /// <summary>
    /// Clears all jobs from the group
    /// </summary>
    public void ClearJobs()
    {
        _jobs.Clear();
        JobsCollection.Clear();
        UpdateTimestamp();
    }

    /// <summary>
    /// Executes all jobs in the group sequentially
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if all jobs executed successfully, false otherwise</returns>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!HasJobs())
        {
            return true;
        }

        foreach (var job in _jobs.OrderBy(j => j.ExecutionOrder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var jobResult = await ExecuteJobSafelyAsync(job, cancellationToken);
            if (!jobResult)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Updates the timestamp when the job group is modified
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the job group configuration
    /// </summary>
    /// <returns>True if job group is valid, false otherwise</returns>
    public bool ValidateJobGroup()
    {
        return !string.IsNullOrWhiteSpace(Name) && IsActive;
    }

    /// <summary>
    /// Finds a job by its ID
    /// </summary>
    /// <param name="jobId">The ID to search for</param>
    /// <returns>The job if found, null otherwise</returns>
    private Job? FindJobById(int jobId)
    {
        return _jobs.FirstOrDefault(j => j.Id == jobId);
    }

    /// <summary>
    /// Checks if a job with the given ID exists
    /// </summary>
    /// <param name="jobId">The ID to check</param>
    /// <returns>True if job exists, false otherwise</returns>
    private bool ContainsJobWithId(int jobId)
    {
        return _jobs.Any(j => j.Id == jobId);
    }

    /// <summary>
    /// Executes a job with error handling
    /// </summary>
    /// <param name="job">The job to execute</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if execution was successful, false otherwise</returns>
    private static async Task<bool> ExecuteJobSafelyAsync(Job job, CancellationToken cancellationToken)
    {
        try
        {
            return await job.ExecuteAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}