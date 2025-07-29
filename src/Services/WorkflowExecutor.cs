using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services
{
    /// <summary>
    /// Service for executing workflows with performance optimization and progress reporting
    /// </summary>
    public sealed class WorkflowExecutor : IWorkflowExecutor
    {
        private readonly IStepHandler _stepHandler;
        private readonly ILogger<WorkflowExecutor>? _logger;
        private const long EstimatedStepExecutionTimeMs = 100;

        /// <summary>
        /// Initializes a new instance of the WorkflowExecutor class
        /// </summary>
        /// <param name="stepHandler">The step handler for executing individual steps</param>
        /// <param name="logger">Optional logger for workflow execution logging</param>
        public WorkflowExecutor(IStepHandler stepHandler, ILogger<WorkflowExecutor>? logger = null)
        {
            _stepHandler = stepHandler ?? throw new ArgumentNullException(nameof(stepHandler));
            _logger = logger;
        }

        /// <summary>
        /// Executes a workflow asynchronously with performance monitoring
        /// </summary>
        /// <param name="workflow">The workflow to execute</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if execution was successful, false otherwise</returns>
        public async Task<bool> ExecuteWorkflowAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            if (!CanExecuteWorkflow(workflow))
            {
                LogWorkflowValidationFailure(workflow);
                return false;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                LogWorkflowExecutionStart(workflow);

                var result = await ExecuteWorkflowStepsAsync(workflow, cancellationToken);

                stopwatch.Stop();
                LogWorkflowExecutionComplete(workflow, result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (OperationCanceledException)
            {
                LogWorkflowExecutionCancelled(workflow);
                return false;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogWorkflowExecutionError(workflow, ex);
                return false;
            }
        }

        /// <summary>
        /// Validates if a workflow can be executed
        /// </summary>
        /// <param name="workflow">The workflow to validate</param>
        /// <returns>True if workflow is valid for execution, false otherwise</returns>
        public bool CanExecuteWorkflow(Workflow workflow)
        {
            return workflow is not null &&
                   workflow.ValidateWorkflow() &&
                   workflow.HasSteps() &&
                   ValidateAllSteps(workflow);
        }

        /// <summary>
        /// Gets the estimated execution time for a workflow
        /// </summary>
        /// <param name="workflow">The workflow to estimate</param>
        /// <returns>Estimated execution time in milliseconds</returns>
        public async Task<long> GetEstimatedExecutionTimeAsync(Workflow workflow)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            if (!workflow.HasSteps())
            {
                return 0;
            }

            // Calculate estimated time based on step count and complexity
            var stepCount = workflow.GetStepCount();
            var baseTime = stepCount * EstimatedStepExecutionTimeMs;

            // Add overhead for workflow coordination
            var overheadTime = stepCount * 10; // 10ms overhead per step

            return await Task.FromResult(baseTime + overheadTime);
        }

        /// <summary>
        /// Executes a workflow with progress reporting
        /// </summary>
        /// <param name="workflow">The workflow to execute</param>
        /// <param name="progress">Progress reporter for execution status</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if execution was successful, false otherwise</returns>
        public async Task<bool> ExecuteWorkflowWithProgressAsync(Workflow workflow, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(workflow);
            ArgumentNullException.ThrowIfNull(progress);

            if (!CanExecuteWorkflow(workflow))
            {
                LogWorkflowValidationFailure(workflow);
                return false;
            }

            var totalSteps = workflow.GetStepCount();
            var completedSteps = 0;

            try
            {
                LogWorkflowExecutionStart(workflow);

                foreach (var step in workflow.Steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var stepResult = await ExecuteStepSafelyAsync(step, cancellationToken);
                    if (!stepResult)
                    {
                        LogWorkflowStepFailure(workflow, step);
                        return false;
                    }

                    completedSteps++;
                    var progressPercentage = CalculateProgressPercentage(completedSteps, totalSteps);
                    progress.Report(progressPercentage);

                    LogWorkflowStepComplete(workflow, step, completedSteps, totalSteps);
                }

                LogWorkflowExecutionComplete(workflow, true, 0);
                return true;
            }
            catch (OperationCanceledException)
            {
                LogWorkflowExecutionCancelled(workflow);
                return false;
            }
            catch (Exception ex)
            {
                LogWorkflowExecutionError(workflow, ex);
                return false;
            }
        }

        /// <summary>
        /// Executes all workflow steps sequentially
        /// </summary>
        /// <param name="workflow">The workflow to execute</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if all steps executed successfully, false otherwise</returns>
        private async Task<bool> ExecuteWorkflowStepsAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            foreach (var step in workflow.Steps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stepResult = await ExecuteStepSafelyAsync(step, cancellationToken);
                if (!stepResult)
                {
                    LogWorkflowStepFailure(workflow, step);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Executes a step with error handling
        /// </summary>
        /// <param name="step">The step to execute</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if execution was successful, false otherwise</returns>
        private async Task<bool> ExecuteStepSafelyAsync(Step step, CancellationToken cancellationToken)
        {
            try
            {
                return await _stepHandler.ExecuteStepAsync(step, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates all steps in the workflow
        /// </summary>
        /// <param name="workflow">The workflow to validate</param>
        /// <returns>True if all steps are valid, false otherwise</returns>
        private bool ValidateAllSteps(Workflow workflow)
        {
            return workflow.Steps.All(step => _stepHandler.CanExecuteStep(step));
        }

        /// <summary>
        /// Calculates progress percentage
        /// </summary>
        /// <param name="completed">Number of completed steps</param>
        /// <param name="total">Total number of steps</param>
        /// <returns>Progress percentage (0-100)</returns>
        private static int CalculateProgressPercentage(int completed, int total)
        {
            if (total == 0) return 100;
            return (int)Math.Round((double)completed / total * 100);
        }

        /// <summary>
        /// Logs workflow execution start
        /// </summary>
        /// <param name="workflow">The workflow being executed</param>
        private void LogWorkflowExecutionStart(Workflow workflow)
        {
            _logger?.LogInformation("Starting execution of workflow {WorkflowId}: {WorkflowName} with {StepCount} steps",
                workflow.Id, workflow.Name, workflow.GetStepCount());
        }

        /// <summary>
        /// Logs workflow execution completion
        /// </summary>
        /// <param name="workflow">The executed workflow</param>
        /// <param name="result">The execution result</param>
        /// <param name="elapsedMilliseconds">The execution time in milliseconds</param>
        private void LogWorkflowExecutionComplete(Workflow workflow, bool result, long elapsedMilliseconds)
        {
            _logger?.LogInformation("Completed execution of workflow {WorkflowId}: {WorkflowName}. Result: {Result}, Duration: {Duration}ms",
                workflow.Id, workflow.Name, result, elapsedMilliseconds);
        }

        /// <summary>
        /// Logs workflow execution error
        /// </summary>
        /// <param name="workflow">The failed workflow</param>
        /// <param name="exception">The exception that occurred</param>
        private void LogWorkflowExecutionError(Workflow workflow, Exception exception)
        {
            _logger?.LogError(exception, "Error executing workflow {WorkflowId}: {WorkflowName}", workflow.Id, workflow.Name);
        }

        /// <summary>
        /// Logs workflow execution cancellation
        /// </summary>
        /// <param name="workflow">The cancelled workflow</param>
        private void LogWorkflowExecutionCancelled(Workflow workflow)
        {
            _logger?.LogWarning("Execution of workflow {WorkflowId}: {WorkflowName} was cancelled", workflow.Id, workflow.Name);
        }

        /// <summary>
        /// Logs workflow validation failure
        /// </summary>
        /// <param name="workflow">The invalid workflow</param>
        private void LogWorkflowValidationFailure(Workflow workflow)
        {
            _logger?.LogWarning("Workflow validation failed for workflow {WorkflowId}: {WorkflowName}", workflow?.Id, workflow?.Name);
        }

        /// <summary>
        /// Logs workflow step failure
        /// </summary>
        /// <param name="workflow">The workflow containing the failed step</param>
        /// <param name="step">The failed step</param>
        private void LogWorkflowStepFailure(Workflow workflow, Step step)
        {
            _logger?.LogError("Step {StepId}: {StepName} failed in workflow {WorkflowId}: {WorkflowName}",
                step.Id, step.Name, workflow.Id, workflow.Name);
        }

        /// <summary>
        /// Logs workflow step completion
        /// </summary>
        /// <param name="workflow">The workflow containing the completed step</param>
        /// <param name="step">The completed step</param>
        /// <param name="completed">Number of completed steps</param>
        /// <param name="total">Total number of steps</param>
        private void LogWorkflowStepComplete(Workflow workflow, Step step, int completed, int total)
        {
            _logger?.LogDebug("Step {StepId}: {StepName} completed in workflow {WorkflowId}: {WorkflowName} ({Completed}/{Total})",
                step.Id, step.Name, workflow.Id, workflow.Name, completed, total);
        }
    }
}