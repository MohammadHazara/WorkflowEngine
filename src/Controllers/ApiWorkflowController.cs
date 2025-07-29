using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Controllers;

/// <summary>
/// Controller for managing API data workflows
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ApiWorkflowController : ControllerBase
{
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ApiWorkflowBuilder _workflowBuilder;
    private readonly ILogger<ApiWorkflowController> _logger;

    /// <summary>
    /// Initializes a new instance of the ApiWorkflowController class
    /// </summary>
    /// <param name="workflowExecutor">Service for executing workflows</param>
    /// <param name="workflowBuilder">Service for building API workflows</param>
    /// <param name="logger">Logger for controller operations</param>
    public ApiWorkflowController(
        IWorkflowExecutor workflowExecutor,
        ApiWorkflowBuilder workflowBuilder,
        ILogger<ApiWorkflowController> logger)
    {
        _workflowExecutor = workflowExecutor ?? throw new ArgumentNullException(nameof(workflowExecutor));
        _workflowBuilder = workflowBuilder ?? throw new ArgumentNullException(nameof(workflowBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a complete API-to-file-to-upload workflow
    /// </summary>
    /// <param name="request">The workflow execution request</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The result of the workflow execution</returns>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteApiWorkflow(
        [FromBody] ApiWorkflowRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting API workflow execution for: {WorkflowName}", request.WorkflowName);

            // Create workflow configurations
            var (apiConfig, fileConfig, zipConfig, uploadConfig) = _workflowBuilder.CreateDefaultConfigurations(
                request.ApiUrl,
                request.OutputDirectory ?? Path.GetTempPath(),
                request.UploadUrl,
                request.AuthToken);

            // Override default configurations with user preferences
            if (request.CustomHeaders?.Count > 0)
            {
                foreach (var header in request.CustomHeaders)
                {
                    apiConfig.Headers[header.Key] = header.Value;
                    uploadConfig.Headers[header.Key] = header.Value;
                }
            }

            if (request.TimeoutSeconds.HasValue)
            {
                apiConfig.TimeoutSeconds = request.TimeoutSeconds.Value;
                uploadConfig.TimeoutSeconds = request.TimeoutSeconds.Value;
            }

            if (!string.IsNullOrEmpty(request.FileFieldName))
            {
                uploadConfig.FileFieldName = request.FileFieldName;
            }

            if (request.FormData?.Count > 0)
            {
                foreach (var data in request.FormData)
                {
                    uploadConfig.FormData[data.Key] = data.Value;
                }
            }

            // Create and execute workflow
            var workflow = _workflowBuilder.CreateApiToFileToUploadWorkflow(
                request.WorkflowName,
                apiConfig,
                fileConfig,
                zipConfig,
                uploadConfig);

            var result = await _workflowExecutor.ExecuteWorkflowAsync(workflow, cancellationToken);

            var response = new ApiWorkflowResponse
            {
                Success = result,
                WorkflowName = request.WorkflowName,
                ExecutedAt = DateTime.UtcNow,
                JsonFilePath = fileConfig.OutputPath,
                ZipFilePath = zipConfig.ZipFilePath,
                Message = result ? "Workflow executed successfully" : "Workflow execution failed"
            };

            _logger.LogInformation("API workflow execution completed. Success: {Success}", result);

            return result ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing API workflow: {WorkflowName}", request.WorkflowName);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Executes a simplified API-to-file workflow (no compression or upload)
    /// </summary>
    /// <param name="request">The simplified workflow request</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The result of the workflow execution</returns>
    [HttpPost("execute-simple")]
    public async Task<IActionResult> ExecuteSimpleApiWorkflow(
        [FromBody] SimpleApiWorkflowRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting simple API workflow execution for: {WorkflowName}", request.WorkflowName);

            var apiConfig = new ApiDataFetchConfig
            {
                ApiUrl = request.ApiUrl,
                AuthToken = request.AuthToken,
                TimeoutSeconds = request.TimeoutSeconds ?? 30,
                MaxRetries = 3,
                Headers = request.CustomHeaders ?? new Dictionary<string, string>()
            };

            var fileConfig = new FileCreationConfig
            {
                OutputPath = request.OutputFilePath,
                OverwriteExisting = true
            };

            var workflow = _workflowBuilder.CreateApiToFileWorkflow(
                request.WorkflowName,
                apiConfig,
                fileConfig);

            var result = await _workflowExecutor.ExecuteWorkflowAsync(workflow, cancellationToken);

            var response = new SimpleApiWorkflowResponse
            {
                Success = result,
                WorkflowName = request.WorkflowName,
                ExecutedAt = DateTime.UtcNow,
                OutputFilePath = request.OutputFilePath,
                Message = result ? "Simple workflow executed successfully" : "Simple workflow execution failed"
            };

            return result ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing simple API workflow: {WorkflowName}", request.WorkflowName);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the status and capabilities of the API workflow system
    /// </summary>
    /// <returns>System status information</returns>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = new
        {
            Status = "Online",
            Version = "1.0.0",
            Capabilities = new[]
            {
                "API Data Fetching with retry logic",
                "JSON file creation with formatting",
                "ZIP compression with configurable levels",
                "File upload with multipart form data",
                "Progress tracking and cancellation support",
                "Comprehensive logging and error handling"
            },
            SupportedWorkflows = new[]
            {
                "API-to-File-to-Upload (Complete pipeline)",
                "API-to-File (Simple data fetching)",
                "File-to-Upload (Compression and upload only)"
            },
            Timestamp = DateTime.UtcNow
        };

        return Ok(status);
    }
}

/// <summary>
/// Request model for API workflow execution
/// </summary>
public sealed class ApiWorkflowRequest
{
    /// <summary>
    /// Gets or sets the workflow name
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API URL to fetch data from
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upload URL for the final file
    /// </summary>
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output directory for files
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the authentication token
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Gets or sets custom HTTP headers
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>
    /// Gets or sets the timeout in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the file field name for uploads
    /// </summary>
    public string? FileFieldName { get; set; }

    /// <summary>
    /// Gets or sets additional form data for uploads
    /// </summary>
    public Dictionary<string, string>? FormData { get; set; }
}

/// <summary>
/// Response model for API workflow execution
/// </summary>
public sealed class ApiWorkflowResponse
{
    /// <summary>
    /// Gets or sets whether the workflow was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the workflow name
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the workflow was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the path to the created JSON file
    /// </summary>
    public string JsonFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the created ZIP file
    /// </summary>
    public string ZipFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request model for simple API workflow execution
/// </summary>
public sealed class SimpleApiWorkflowRequest
{
    /// <summary>
    /// Gets or sets the workflow name
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API URL to fetch data from
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output file path
    /// </summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication token
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Gets or sets custom HTTP headers
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>
    /// Gets or sets the timeout in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Response model for simple API workflow execution
/// </summary>
public sealed class SimpleApiWorkflowResponse
{
    /// <summary>
    /// Gets or sets whether the workflow was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the workflow name
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the workflow was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the output file path
    /// </summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}