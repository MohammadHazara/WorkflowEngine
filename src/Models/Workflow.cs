using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents a workflow containing multiple steps that can be executed sequentially
/// </summary>
public class Workflow
{
    /// <summary>
    /// Gets or sets the unique identifier for the workflow
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the workflow
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the workflow
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
    /// Gets or sets whether the workflow is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets the collection of steps in this workflow
    /// </summary>
    private readonly List<Step> _steps;

    /// <summary>
    /// Gets the read-only view of steps
    /// </summary>
    public IReadOnlyList<Step> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Gets the collection of steps in this workflow
    /// </summary>
    /// <returns>Read-only collection of steps</returns>
    public IReadOnlyList<Step> GetSteps()
    {
        return Steps;
    }

    /// <summary>
    /// Initializes a new instance of the Workflow class
    /// </summary>
    public Workflow()
    {
        _steps = new List<Step>();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Initializes a new instance of the Workflow class with specified parameters
    /// </summary>
    /// <param name="name">The name of the workflow</param>
    /// <param name="description">The description of the workflow</param>
    public Workflow(string name, string? description = null) : this()
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Adds a step to the workflow
    /// </summary>
    /// <param name="step">The step to add</param>
    /// <exception cref="ArgumentNullException">Thrown when step is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when step with same ID already exists</exception>
    public void AddStep(Step step)
    {
        ArgumentNullException.ThrowIfNull(step);

        if (ContainsStepWithId(step.Id))
        {
            throw new InvalidOperationException($"Step with ID {step.Id} already exists in the workflow");
        }

        _steps.Add(step);
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a step from the workflow by ID
    /// </summary>
    /// <param name="stepId">The ID of the step to remove</param>
    /// <returns>True if step was removed, false if not found</returns>
    public bool RemoveStep(int stepId)
    {
        var stepToRemove = FindStepById(stepId);
        if (stepToRemove is null)
        {
            return false;
        }

        var removed = _steps.Remove(stepToRemove);
        if (removed)
        {
            UpdateTimestamp();
        }

        return removed;
    }

    /// <summary>
    /// Gets a step by its ID
    /// </summary>
    /// <param name="stepId">The ID of the step to retrieve</param>
    /// <returns>The step if found, null otherwise</returns>
    public Step? GetStepById(int stepId)
    {
        return FindStepById(stepId);
    }

    /// <summary>
    /// Gets the total number of steps in the workflow
    /// </summary>
    /// <returns>The number of steps</returns>
    public int GetStepCount()
    {
        return _steps.Count;
    }

    /// <summary>
    /// Checks if the workflow contains any steps
    /// </summary>
    /// <returns>True if workflow has steps, false otherwise</returns>
    public bool HasSteps()
    {
        return _steps.Count > 0;
    }

    /// <summary>
    /// Clears all steps from the workflow
    /// </summary>
    public void ClearSteps()
    {
        _steps.Clear();
        UpdateTimestamp();
    }

    /// <summary>
    /// Executes all steps in the workflow sequentially
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if all steps executed successfully, false otherwise</returns>
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!HasSteps())
        {
            return true;
        }

        foreach (var step in _steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepResult = await ExecuteStepSafelyAsync(step);
            if (!stepResult)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Updates the timestamp when the workflow is modified
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the workflow configuration
    /// </summary>
    /// <returns>True if workflow is valid, false otherwise</returns>
    public bool ValidateWorkflow()
    {
        return !string.IsNullOrWhiteSpace(Name) && IsActive;
    }

    /// <summary>
    /// Finds a step by its ID
    /// </summary>
    /// <param name="stepId">The ID to search for</param>
    /// <returns>The step if found, null otherwise</returns>
    private Step? FindStepById(int stepId)
    {
        return _steps.FirstOrDefault(s => s.Id == stepId);
    }

    /// <summary>
    /// Checks if a step with the given ID exists
    /// </summary>
    /// <param name="stepId">The ID to check</param>
    /// <returns>True if step exists, false otherwise</returns>
    private bool ContainsStepWithId(int stepId)
    {
        return _steps.Any(s => s.Id == stepId);
    }

    /// <summary>
    /// Executes a step with error handling
    /// </summary>
    /// <param name="step">The step to execute</param>
    /// <returns>True if execution was successful, false otherwise</returns>
    private static async Task<bool> ExecuteStepSafelyAsync(Step step)
    {
        try
        {
            return await step.ExecuteAsync();
        }
        catch
        {
            return false;
        }
    }
}