using System;
using System.Threading.Tasks;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents a workflow step with execution delegates and metadata
/// </summary>
public class Step
{
    /// <summary>
    /// Gets or sets the unique identifier for the step
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the step
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the function to execute on successful completion
    /// </summary>
    public Func<Task<bool>>? OnSuccess { get; set; }

    /// <summary>
    /// Gets or sets the function to execute on failure
    /// </summary>
    public Func<Task<bool>>? OnFailure { get; set; }

    /// <summary>
    /// Gets or sets the step execution function
    /// </summary>
    public Func<Task<bool>>? ExecuteFunction { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the Step class with default values
    /// </summary>
    public Step()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the Step class with specified parameters
    /// </summary>
    /// <param name="id">The unique identifier for the step</param>
    /// <param name="name">The name of the step</param>
    /// <param name="executeFunction">The function to execute for this step</param>
    /// <param name="onSuccess">The function to execute on success</param>
    /// <param name="onFailure">The function to execute on failure</param>
    public Step(int id, string? name, Func<Task<bool>>? executeFunction = null, Func<Task<bool>>? onSuccess = null, Func<Task<bool>>? onFailure = null)
    {
        Id = id;
        Name = name;
        ExecuteFunction = executeFunction;
        OnSuccess = onSuccess;
        OnFailure = onFailure;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Executes the step and invokes appropriate callbacks based on the result
    /// </summary>
    /// <returns>True if execution was successful, false otherwise</returns>
    public virtual async Task<bool> ExecuteAsync()
    {
        try
        {
            var executionResult = await ExecuteStepLogicAsync();
            
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
    /// Executes the core step logic
    /// </summary>
    /// <returns>True if execution was successful, false otherwise</returns>
    private async Task<bool> ExecuteStepLogicAsync()
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

    /// <summary>
    /// Updates the timestamp when the step is modified
    /// </summary>
    public virtual void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}