using System;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;


namespace WorkflowEngine.Services
{
    /// <summary>
    /// Service for handling step execution with performance optimization and error handling
    /// </summary>
    public sealed class StepHandler : IStepHandler
    {
        private readonly ILogger<StepHandler>? _logger;
        private const int DefaultExecutionPriority = 1;
        private const int MaxRetryAttempts = 3;

        /// <summary>
        /// Initializes a new instance of the StepHandler class
        /// </summary>
        /// <param name="logger">Optional logger for step execution logging</param>
        public StepHandler(ILogger<StepHandler>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Executes a step asynchronously with retry logic and performance monitoring
        /// </summary>
        /// <param name="step">The step to execute</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if execution was successful, false otherwise</returns>
        public async Task<bool> ExecuteStepAsync(Step step, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(step);

            if (!CanExecuteStep(step))
            {
                LogStepValidationFailure(step);
                return false;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                LogStepExecutionStart(step);
                
                var result = await ExecuteStepWithRetryAsync(step, cancellationToken);
                
                stopwatch.Stop();
                LogStepExecutionComplete(step, result, stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (OperationCanceledException)
            {
                LogStepExecutionCancelled(step);
                return false;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogStepExecutionError(step, ex);
                return false;
            }
        }

        /// <summary>
        /// Validates if a step can be executed
        /// </summary>
        /// <param name="step">The step to validate</param>
        /// <returns>True if step is valid for execution, false otherwise</returns>
        public bool CanExecuteStep(Step step)
        {
            return step is not null && 
                   !string.IsNullOrWhiteSpace(step.Name) &&
                   step.Id > 0;
        }

        /// <summary>
        /// Gets the execution priority for a step
        /// </summary>
        /// <param name="step">The step to get priority for</param>
        /// <returns>The execution priority (higher number = higher priority)</returns>
        public int GetExecutionPriority(Step step)
        {
            ArgumentNullException.ThrowIfNull(step);
            
            // Priority can be determined by step ID or other business logic
            return step.Id > 0 ? step.Id : DefaultExecutionPriority;
        }

        /// <summary>
        /// Executes a step with retry logic
        /// </summary>
        /// <param name="step">The step to execute</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if execution was successful, false otherwise</returns>
        private async Task<bool> ExecuteStepWithRetryAsync(Step step, CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var result = await step.ExecuteAsync();
                    if (result)
                    {
                        return true;
                    }

                    if (attempt < MaxRetryAttempts)
                    {
                        LogStepRetryAttempt(step, attempt);
                        await DelayBeforeRetryAsync(attempt, cancellationToken);
                    }
                }
                catch when (attempt < MaxRetryAttempts)
                {
                    LogStepRetryAttempt(step, attempt);
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates delay before retry attempt
        /// </summary>
        /// <param name="attempt">The current attempt number</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        private static async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)); // Exponential backoff
            await Task.Delay(delay, cancellationToken);
        }

        /// <summary>
        /// Logs step execution start
        /// </summary>
        /// <param name="step">The step being executed</param>
        private void LogStepExecutionStart(Step step)
        {
            _logger?.LogInformation("Starting execution of step {StepId}: {StepName}", step.Id, step.Name);
        }

        /// <summary>
        /// Logs step execution completion
        /// </summary>
        /// <param name="step">The executed step</param>
        /// <param name="result">The execution result</param>
        /// <param name="elapsedMilliseconds">The execution time in milliseconds</param>
        private void LogStepExecutionComplete(Step step, bool result, long elapsedMilliseconds)
        {
            _logger?.LogInformation("Completed execution of step {StepId}: {StepName}. Result: {Result}, Duration: {Duration}ms", 
                step.Id, step.Name, result, elapsedMilliseconds);
        }

        /// <summary>
        /// Logs step execution error
        /// </summary>
        /// <param name="step">The failed step</param>
        /// <param name="exception">The exception that occurred</param>
        private void LogStepExecutionError(Step step, Exception exception)
        {
            _logger?.LogError(exception, "Error executing step {StepId}: {StepName}", step.Id, step.Name);
        }

        /// <summary>
        /// Logs step execution cancellation
        /// </summary>
        /// <param name="step">The cancelled step</param>
        private void LogStepExecutionCancelled(Step step)
        {
            _logger?.LogWarning("Execution of step {StepId}: {StepName} was cancelled", step.Id, step.Name);
        }

        /// <summary>
        /// Logs step validation failure
        /// </summary>
        /// <param name="step">The invalid step</param>
        private void LogStepValidationFailure(Step step)
        {
            _logger?.LogWarning("Step validation failed for step {StepId}: {StepName}", step?.Id, step?.Name);
        }

        /// <summary>
        /// Logs retry attempt
        /// </summary>
        /// <param name="step">The step being retried</param>
        /// <param name="attempt">The attempt number</param>
        private void LogStepRetryAttempt(Step step, int attempt)
        {
            _logger?.LogWarning("Retrying step {StepId}: {StepName}, attempt {Attempt}/{MaxAttempts}", 
                step.Id, step.Name, attempt, MaxRetryAttempts);
        }
    }
}