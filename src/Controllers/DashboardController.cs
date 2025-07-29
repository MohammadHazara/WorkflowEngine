using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;

namespace WorkflowEngine.Controllers;

/// <summary>
/// API controller for dashboard operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    /// <summary>
    /// Initializes a new instance of the DashboardController class
    /// </summary>
    /// <param name="dashboardService">The dashboard service</param>
    /// <param name="logger">The logger instance</param>
    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets dashboard statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, "An error occurred while retrieving dashboard statistics");
        }
    }

    /// <summary>
    /// Gets recent workflow executions
    /// </summary>
    /// <param name="limit">Maximum number of executions to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recent workflow executions</returns>
    [HttpGet("recent-executions")]
    public async Task<ActionResult<IEnumerable<WorkflowExecution>>> GetRecentExecutionsAsync(
        [FromQuery] int limit = 20, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var executions = await _dashboardService.GetRecentExecutionsAsync(limit, cancellationToken);
            return Ok(executions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent executions");
            return StatusCode(500, "An error occurred while retrieving recent executions");
        }
    }
}

/// <summary>
/// API controller for workflow operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<WorkflowsController> _logger;

    /// <summary>
    /// Initializes a new instance of the WorkflowsController class
    /// </summary>
    /// <param name="dashboardService">The dashboard service</param>
    /// <param name="logger">The logger instance</param>
    public WorkflowsController(IDashboardService dashboardService, ILogger<WorkflowsController> logger)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets paginated list of workflows
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated workflows</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<Workflow>>> GetWorkflowsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workflows = await _dashboardService.GetWorkflowsAsync(page, pageSize, cancellationToken);
            return Ok(workflows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflows");
            return StatusCode(500, "An error occurred while retrieving workflows");
        }
    }

    /// <summary>
    /// Gets a specific workflow by ID
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Workflow details</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Workflow>> GetWorkflowAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await _dashboardService.GetWorkflowByIdAsync(id, cancellationToken);
            if (workflow is null)
            {
                return NotFound($"Workflow with ID {id} not found");
            }

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow {WorkflowId}", id);
            return StatusCode(500, "An error occurred while retrieving the workflow");
        }
    }

    /// <summary>
    /// Creates a new workflow
    /// </summary>
    /// <param name="workflow">Workflow to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created workflow</returns>
    [HttpPost]
    public async Task<ActionResult<Workflow>> CreateWorkflowAsync(
        [FromBody] Workflow workflow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdWorkflow = await _dashboardService.CreateWorkflowAsync(workflow, cancellationToken);
            return CreatedAtAction(nameof(GetWorkflowAsync), new { id = createdWorkflow.Id }, createdWorkflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            return StatusCode(500, "An error occurred while creating the workflow");
        }
    }

    /// <summary>
    /// Updates an existing workflow
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <param name="workflow">Updated workflow data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated workflow</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Workflow>> UpdateWorkflowAsync(
        int id,
        [FromBody] Workflow workflow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != workflow.Id)
            {
                return BadRequest("Workflow ID mismatch");
            }

            var updatedWorkflow = await _dashboardService.UpdateWorkflowAsync(workflow, cancellationToken);
            return Ok(updatedWorkflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId}", id);
            return StatusCode(500, "An error occurred while updating the workflow");
        }
    }

    /// <summary>
    /// Deletes a workflow
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteWorkflowAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _dashboardService.DeleteWorkflowAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound($"Workflow with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", id);
            return StatusCode(500, "An error occurred while deleting the workflow");
        }
    }

    /// <summary>
    /// Gets executions for a specific workflow
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated workflow executions</returns>
    [HttpGet("{id:int}/executions")]
    public async Task<ActionResult<PaginatedResult<WorkflowExecution>>> GetWorkflowExecutionsAsync(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var executions = await _dashboardService.GetWorkflowExecutionsAsync(id, page, pageSize, cancellationToken);
            return Ok(executions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving executions for workflow {WorkflowId}", id);
            return StatusCode(500, "An error occurred while retrieving workflow executions");
        }
    }

    /// <summary>
    /// Starts a workflow execution
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created workflow execution</returns>
    [HttpPost("{id:int}/execute")]
    public async Task<ActionResult<WorkflowExecution>> ExecuteWorkflowAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var execution = await _dashboardService.StartWorkflowExecutionAsync(id, cancellationToken);
            return Ok(execution);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting execution for workflow {WorkflowId}", id);
            return StatusCode(500, "An error occurred while starting the workflow execution");
        }
    }
}

/// <summary>
/// API controller for workflow execution operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ExecutionsController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<ExecutionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the ExecutionsController class
    /// </summary>
    /// <param name="dashboardService">The dashboard service</param>
    /// <param name="logger">The logger instance</param>
    public ExecutionsController(IDashboardService dashboardService, ILogger<ExecutionsController> logger)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cancels a running workflow execution
    /// </summary>
    /// <param name="id">Execution ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelExecutionAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cancelled = await _dashboardService.CancelWorkflowExecutionAsync(id, cancellationToken);
            if (!cancelled)
            {
                return NotFound($"Execution with ID {id} not found or cannot be cancelled");
            }

            return Ok(new { message = "Execution cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling execution {ExecutionId}", id);
            return StatusCode(500, "An error occurred while cancelling the execution");
        }
    }
}