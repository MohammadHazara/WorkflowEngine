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

    private List<Job>? _jobs;

    /// <summary>
    /// Gets the read-only view of jobs
    /// </summary>
    [NotMapped]
    public IReadOnlyList<Job> Jobs => (_jobs ??= JobsCollection?.ToList() ?? new List<Job>()).AsReadOnly();

    /// <summary>
    /// Navigation property for Entity Framework
    /// </summary>
    public ICollection<Job> JobsCollection { get; set; } = new List<Job>();

    /// <summary>
    /// Initializes a new instance of the JobGroup class
    /// </summary>
    public JobGroup()
    {
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
        JobsCollection.Add(job);
        
        // Synchronize private collection
        _jobs?.Add(job);
        
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a job from the group by ID
    /// </summary>
    /// <param name="jobId">The ID of the job to remove</param>
    /// <returns>True if the job was removed, false if not found</returns>
    public bool RemoveJob(int jobId)
    {
        var job = FindJobById(jobId);
        if (job is null)
        {
            return false;
        }

        var removed = JobsCollection.Remove(job);
        if (removed)
        {
            _jobs?.Remove(job);
            UpdateTimestamp();
        }

        return removed;
    }

    /// <summary>
    /// Checks if a job with the specified ID exists in the group
    /// </summary>
    /// <param name="jobId">The job ID to check</param>
    /// <returns>True if job exists, false otherwise</returns>
    public bool ContainsJobWithId(int jobId)
    {
        return JobsCollection.Any(j => j.Id == jobId);
    }

    /// <summary>
    /// Gets the total number of jobs in the group
    /// </summary>
    /// <returns>The number of jobs</returns>
    public int GetJobCount()
    {
        return JobsCollection.Count;
    }

    /// <summary>
    /// Checks if the job group has any jobs
    /// </summary>
    /// <returns>True if the group contains at least one job, false otherwise</returns>
    public bool HasJobs()
    {
        return JobsCollection.Count > 0;
    }

    /// <summary>
    /// Gets the total number of active jobs in the group
    /// </summary>
    /// <returns>The number of active jobs</returns>
    public int GetActiveJobCount()
    {
        return JobsCollection.Count(j => j.IsActive);
    }

    /// <summary>
    /// Gets all active jobs ordered by execution order
    /// </summary>
    /// <returns>List of active jobs</returns>
    public IEnumerable<Job> GetActiveJobsOrdered()
    {
        return JobsCollection.Where(j => j.IsActive)
                           .OrderBy(j => j.ExecutionOrder)
                           .ThenBy(j => j.Name);
    }

    /// <summary>
    /// Gets jobs by type
    /// </summary>
    /// <param name="jobType">The job type to filter by</param>
    /// <returns>Jobs of the specified type</returns>
    public IEnumerable<Job> GetJobsByType(string jobType)
    {
        ArgumentNullException.ThrowIfNull(jobType);
        return JobsCollection.Where(j => j.JobType.Equals(jobType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Deactivates all jobs in the group
    /// </summary>
    public void DeactivateAllJobs()
    {
        foreach (var job in JobsCollection)
        {
            job.IsActive = false;
            job.UpdateTimestamp();
        }
        UpdateTimestamp();
    }

    /// <summary>
    /// Activates all jobs in the group
    /// </summary>
    public void ActivateAllJobs()
    {
        foreach (var job in JobsCollection)
        {
            job.IsActive = true;
            job.UpdateTimestamp();
        }
        UpdateTimestamp();
    }

    /// <summary>
    /// Reorders jobs in the group
    /// </summary>
    /// <param name="jobOrders">Dictionary of job ID to execution order</param>
    public void ReorderJobs(Dictionary<int, int> jobOrders)
    {
        ArgumentNullException.ThrowIfNull(jobOrders);

        foreach (var kvp in jobOrders)
        {
            var job = FindJobById(kvp.Key);
            if (job is not null)
            {
                job.ExecutionOrder = kvp.Value;
                job.UpdateTimestamp();
            }
        }
        UpdateTimestamp();
    }

    /// <summary>
    /// Clears all jobs from the group
    /// </summary>
    public void ClearJobs()
    {
        JobsCollection.Clear();
        _jobs?.Clear();
        UpdateTimestamp();
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
        return JobsCollection.FirstOrDefault(j => j.Id == jobId);
    }
}