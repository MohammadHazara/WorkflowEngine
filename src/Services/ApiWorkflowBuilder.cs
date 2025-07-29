using Microsoft.Extensions.Logging;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for building specialized API data workflows with performance optimization
/// </summary>
public sealed class ApiWorkflowBuilder
{
    private readonly ApiDataService _apiDataService;
    private readonly ILogger<ApiWorkflowBuilder>? _logger;

    /// <summary>
    /// Initializes a new instance of the ApiWorkflowBuilder class
    /// </summary>
    /// <param name="apiDataService">Service for API data operations</param>
    /// <param name="logger">Optional logger for workflow building operations</param>
    public ApiWorkflowBuilder(ApiDataService apiDataService, ILogger<ApiWorkflowBuilder>? logger = null)
    {
        _apiDataService = apiDataService ?? throw new ArgumentNullException(nameof(apiDataService));
        _logger = logger;
    }

    /// <summary>
    /// Creates a complete API-to-file-to-upload workflow
    /// </summary>
    /// <param name="workflowName">Name of the workflow</param>
    /// <param name="apiConfig">Configuration for API data fetching</param>
    /// <param name="fileConfig">Configuration for file creation</param>
    /// <param name="zipConfig">Configuration for ZIP compression</param>
    /// <param name="uploadConfig">Configuration for file upload</param>
    /// <returns>A configured workflow ready for execution</returns>
    public Workflow CreateApiToFileToUploadWorkflow(
        string workflowName,
        ApiDataFetchConfig apiConfig,
        FileCreationConfig fileConfig,
        ZipCompressionConfig zipConfig,
        ApiUploadConfig uploadConfig)
    {
        ArgumentNullException.ThrowIfNull(workflowName);
        ArgumentNullException.ThrowIfNull(apiConfig);
        ArgumentNullException.ThrowIfNull(fileConfig);
        ArgumentNullException.ThrowIfNull(zipConfig);
        ArgumentNullException.ThrowIfNull(uploadConfig);

        _logger?.LogInformation("Creating API-to-file-to-upload workflow: {WorkflowName}", workflowName);

        var workflow = new Workflow(workflowName, "Fetches data from API, creates JSON file, compresses to ZIP, and uploads");

        // Shared data storage for passing data between steps
        string? fetchedData = null;

        // Step 1: Fetch data from API
        var fetchStep = new Step(1, "Fetch API Data",
            executeFunction: async () =>
            {
                try
                {
                    fetchedData = await _apiDataService.FetchDataAsync(apiConfig);
                    return !string.IsNullOrEmpty(fetchedData);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to fetch API data");
                    return false;
                }
            },
            onSuccess: () =>
            {
                _logger?.LogInformation("Successfully fetched API data");
                return Task.FromResult(true);
            },
            onFailure: () =>
            {
                _logger?.LogError("Failed to fetch API data");
                return Task.FromResult(false);
            });

        // Step 2: Create JSON file
        var createFileStep = new Step(2, "Create JSON File",
            executeFunction: async () =>
            {
                if (string.IsNullOrEmpty(fetchedData))
                {
                    _logger?.LogError("No data available to create file");
                    return false;
                }

                return await _apiDataService.CreateJsonFileAsync(fetchedData, fileConfig);
            },
            onSuccess: () =>
            {
                _logger?.LogInformation("Successfully created JSON file: {FilePath}", fileConfig.OutputPath);
                return Task.FromResult(true);
            },
            onFailure: () =>
            {
                _logger?.LogError("Failed to create JSON file");
                return Task.FromResult(false);
            });

        // Step 3: Compress file to ZIP
        var compressStep = new Step(3, "Compress to ZIP",
            executeFunction: async () =>
            {
                return await _apiDataService.CompressFileAsync(zipConfig);
            },
            onSuccess: () =>
            {
                _logger?.LogInformation("Successfully compressed file to ZIP: {ZipPath}", zipConfig.ZipFilePath);
                return Task.FromResult(true);
            },
            onFailure: () =>
            {
                _logger?.LogError("Failed to compress file to ZIP");
                return Task.FromResult(false);
            });

        // Step 4: Upload ZIP file
        var uploadStep = new Step(4, "Upload ZIP File",
            executeFunction: async () =>
            {
                return await _apiDataService.UploadFileAsync(uploadConfig);
            },
            onSuccess: () =>
            {
                _logger?.LogInformation("Successfully uploaded ZIP file: {FilePath}", uploadConfig.FilePath);
                return Task.FromResult(true);
            },
            onFailure: () =>
            {
                _logger?.LogError("Failed to upload ZIP file");
                return Task.FromResult(false);
            });

        // Add steps to workflow
        workflow.AddStep(fetchStep);
        workflow.AddStep(createFileStep);
        workflow.AddStep(compressStep);
        workflow.AddStep(uploadStep);

        _logger?.LogInformation("Created workflow '{WorkflowName}' with {StepCount} steps", 
            workflowName, workflow.GetStepCount());

        return workflow;
    }

    /// <summary>
    /// Creates a simplified API-to-file workflow (without compression and upload)
    /// </summary>
    /// <param name="workflowName">Name of the workflow</param>
    /// <param name="apiConfig">Configuration for API data fetching</param>
    /// <param name="fileConfig">Configuration for file creation</param>
    /// <returns>A configured workflow ready for execution</returns>
    public Workflow CreateApiToFileWorkflow(
        string workflowName,
        ApiDataFetchConfig apiConfig,
        FileCreationConfig fileConfig)
    {
        ArgumentNullException.ThrowIfNull(workflowName);
        ArgumentNullException.ThrowIfNull(apiConfig);
        ArgumentNullException.ThrowIfNull(fileConfig);

        _logger?.LogInformation("Creating API-to-file workflow: {WorkflowName}", workflowName);

        var workflow = new Workflow(workflowName, "Fetches data from API and creates JSON file");

        // Shared data storage
        string? fetchedData = null;

        // Step 1: Fetch data from API
        var fetchStep = new Step(1, "Fetch API Data",
            executeFunction: async () =>
            {
                try
                {
                    fetchedData = await _apiDataService.FetchDataAsync(apiConfig);
                    return !string.IsNullOrEmpty(fetchedData);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to fetch API data");
                    return false;
                }
            });

        // Step 2: Create JSON file
        var createFileStep = new Step(2, "Create JSON File",
            executeFunction: async () =>
            {
                if (string.IsNullOrEmpty(fetchedData))
                {
                    return false;
                }

                return await _apiDataService.CreateJsonFileAsync(fetchedData, fileConfig);
            });

        workflow.AddStep(fetchStep);
        workflow.AddStep(createFileStep);

        return workflow;
    }

    /// <summary>
    /// Creates a file compression and upload workflow
    /// </summary>
    /// <param name="workflowName">Name of the workflow</param>
    /// <param name="zipConfig">Configuration for ZIP compression</param>
    /// <param name="uploadConfig">Configuration for file upload</param>
    /// <returns>A configured workflow ready for execution</returns>
    public Workflow CreateFileToUploadWorkflow(
        string workflowName,
        ZipCompressionConfig zipConfig,
        ApiUploadConfig uploadConfig)
    {
        ArgumentNullException.ThrowIfNull(workflowName);
        ArgumentNullException.ThrowIfNull(zipConfig);
        ArgumentNullException.ThrowIfNull(uploadConfig);

        _logger?.LogInformation("Creating file-to-upload workflow: {WorkflowName}", workflowName);

        var workflow = new Workflow(workflowName, "Compresses file to ZIP and uploads to API");

        // Step 1: Compress file
        var compressStep = new Step(1, "Compress to ZIP",
            executeFunction: async () => await _apiDataService.CompressFileAsync(zipConfig));

        // Step 2: Upload file
        var uploadStep = new Step(2, "Upload ZIP File",
            executeFunction: async () => await _apiDataService.UploadFileAsync(uploadConfig));

        workflow.AddStep(compressStep);
        workflow.AddStep(uploadStep);

        return workflow;
    }

    /// <summary>
    /// Creates a configuration template for common API scenarios
    /// </summary>
    /// <param name="apiUrl">The API endpoint URL</param>
    /// <param name="outputDirectory">Directory where files will be created</param>
    /// <param name="uploadUrl">The upload endpoint URL</param>
    /// <param name="authToken">Optional authentication token</param>
    /// <returns>A complete set of configurations for the workflow</returns>
    public (ApiDataFetchConfig apiConfig, FileCreationConfig fileConfig, ZipCompressionConfig zipConfig, ApiUploadConfig uploadConfig) 
        CreateDefaultConfigurations(string apiUrl, string outputDirectory, string uploadUrl, string? authToken = null)
    {
        ArgumentNullException.ThrowIfNull(apiUrl);
        ArgumentNullException.ThrowIfNull(outputDirectory);
        ArgumentNullException.ThrowIfNull(uploadUrl);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var jsonFileName = $"api_data_{timestamp}.json";
        var zipFileName = $"api_data_{timestamp}.zip";
        
        var jsonFilePath = Path.Combine(outputDirectory, jsonFileName);
        var zipFilePath = Path.Combine(outputDirectory, zipFileName);

        var apiConfig = new ApiDataFetchConfig
        {
            ApiUrl = apiUrl,
            AuthToken = authToken,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            Headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "User-Agent", "WorkflowEngine/1.0" }
            }
        };

        var fileConfig = new FileCreationConfig
        {
            OutputPath = jsonFilePath,
            OverwriteExisting = true,
            JsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }
        };

        var zipConfig = new ZipCompressionConfig
        {
            SourceFilePath = jsonFilePath,
            ZipFilePath = zipFilePath,
            CompressionLevel = System.IO.Compression.CompressionLevel.Optimal,
            DeleteSourceAfterCompression = false
        };

        var uploadConfig = new ApiUploadConfig
        {
            UploadUrl = uploadUrl,
            FilePath = zipFilePath,
            FileFieldName = "file",
            AuthToken = authToken,
            TimeoutSeconds = 300,
            Headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" }
            },
            FormData = new Dictionary<string, string>
            {
                { "timestamp", timestamp },
                { "source", "WorkflowEngine" }
            }
        };

        _logger?.LogInformation("Created default configurations for API workflow");

        return (apiConfig, fileConfig, zipConfig, uploadConfig);
    }
}