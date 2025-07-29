using Microsoft.Extensions.Logging;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for building data pipeline jobs with tasks for API workflows
/// </summary>
public sealed class JobBuilder
{
    private readonly ApiDataService _apiDataService;
    private readonly SftpService _sftpService;
    private readonly ILogger<JobBuilder>? _logger;

    /// <summary>
    /// Initializes a new instance of the JobBuilder class
    /// </summary>
    /// <param name="apiDataService">The API data service</param>
    /// <param name="sftpService">The SFTP service</param>
    /// <param name="logger">Optional logger</param>
    public JobBuilder(ApiDataService apiDataService, SftpService sftpService, ILogger<JobBuilder>? logger = null)
    {
        _apiDataService = apiDataService ?? throw new ArgumentNullException(nameof(apiDataService));
        _sftpService = sftpService ?? throw new ArgumentNullException(nameof(sftpService));
        _logger = logger;
    }

    /// <summary>
    /// Creates a complete data pipeline job with API fetch, file creation, compression, and SFTP upload tasks
    /// </summary>
    /// <param name="jobName">The name of the job</param>
    /// <param name="apiConfig">Configuration for API data fetching</param>
    /// <param name="fileConfig">Configuration for file creation</param>
    /// <param name="zipConfig">Configuration for ZIP compression</param>
    /// <param name="sftpConfig">Configuration for SFTP upload</param>
    /// <returns>A configured data pipeline job</returns>
    public Job CreateDataPipelineJob(
        string jobName,
        ApiDataFetchConfig apiConfig,
        FileCreationConfig fileConfig,
        ZipCompressionConfig zipConfig,
        SftpUploadConfig sftpConfig)
    {
        ArgumentNullException.ThrowIfNull(jobName);
        ArgumentNullException.ThrowIfNull(apiConfig);
        ArgumentNullException.ThrowIfNull(fileConfig);
        ArgumentNullException.ThrowIfNull(zipConfig);
        ArgumentNullException.ThrowIfNull(sftpConfig);

        _logger?.LogInformation("Creating data pipeline job: {JobName}", jobName);

        var job = Job.CreateDataPipelineJob(jobName, "Data pipeline with API fetch, file creation, compression, and SFTP upload");

        // Shared data storage for passing data between tasks
        string? fetchedData = null;

        // Task 1: Fetch data from API
        var fetchTask = JobTask.CreateApiFetchTask(1, "Fetch API Data", apiConfig.ApiUrl,
            executeFunction: async () =>
            {
                try
                {
                    fetchedData = await _apiDataService.FetchDataAsync(apiConfig);
                    return !string.IsNullOrEmpty(fetchedData);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to fetch data from API: {ApiUrl}", apiConfig.ApiUrl);
                    return false;
                }
            });
        fetchTask.ExecutionOrder = 1;

        // Task 2: Create JSON file
        var createFileTask = JobTask.CreateFileTask(2, "Create JSON File", fileConfig.OutputPath,
            executeFunction: async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(fetchedData))
                    {
                        _logger?.LogWarning("No data available to create file");
                        return false;
                    }

                    return await _apiDataService.CreateJsonFileAsync(fetchedData, fileConfig);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create JSON file: {FilePath}", fileConfig.OutputPath);
                    return false;
                }
            });
        createFileTask.ExecutionOrder = 2;

        // Task 3: Compress to ZIP
        var compressTask = new JobTask(3, "Compress to ZIP", "CompressFile",
            executeFunction: async () =>
            {
                try
                {
                    return await _apiDataService.CompressFileAsync(zipConfig);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to compress file to ZIP: {ZipFilePath}", zipConfig.ZipFilePath);
                    return false;
                }
            });
        compressTask.ExecutionOrder = 3;

        // Task 4: Upload via SFTP
        var sftpUploadTask = JobTask.CreateSftpUploadTask(4, "Upload to SFTP", 
            sftpConfig.Host, sftpConfig.LocalFilePath, sftpConfig.RemoteDirectoryPath,
            executeFunction: async () =>
            {
                try
                {
                    return await _sftpService.UploadFileAsync(sftpConfig);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to upload file via SFTP to: {Host}", sftpConfig.Host);
                    return false;
                }
            });
        sftpUploadTask.ExecutionOrder = 4;

        // Add tasks to job
        job.AddTask(fetchTask);
        job.AddTask(createFileTask);
        job.AddTask(compressTask);
        job.AddTask(sftpUploadTask);

        _logger?.LogInformation("Created data pipeline job '{JobName}' with {TaskCount} tasks", 
            jobName, job.GetTaskCount());

        return job;
    }

    /// <summary>
    /// Creates a simple API to file job
    /// </summary>
    /// <param name="jobName">The name of the job</param>
    /// <param name="apiConfig">Configuration for API data fetching</param>
    /// <param name="fileConfig">Configuration for file creation</param>
    /// <returns>A configured API to file job</returns>
    public Job CreateApiToFileJob(
        string jobName,
        ApiDataFetchConfig apiConfig,
        FileCreationConfig fileConfig)
    {
        ArgumentNullException.ThrowIfNull(jobName);
        ArgumentNullException.ThrowIfNull(apiConfig);
        ArgumentNullException.ThrowIfNull(fileConfig);

        _logger?.LogInformation("Creating API to file job: {JobName}", jobName);

        var job = Job.CreateApiIntegrationJob(jobName, "Simple API to file job");

        // Shared data storage
        string? fetchedData = null;

        // Task 1: Fetch data from API
        var fetchTask = JobTask.CreateApiFetchTask(1, "Fetch API Data", apiConfig.ApiUrl,
            executeFunction: async () =>
            {
                try
                {
                    fetchedData = await _apiDataService.FetchDataAsync(apiConfig);
                    return !string.IsNullOrEmpty(fetchedData);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to fetch data from API: {ApiUrl}", apiConfig.ApiUrl);
                    return false;
                }
            });
        fetchTask.ExecutionOrder = 1;

        // Task 2: Create JSON file
        var createFileTask = JobTask.CreateFileTask(2, "Create JSON File", fileConfig.OutputPath,
            executeFunction: async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(fetchedData))
                    {
                        _logger?.LogWarning("No data available to create file");
                        return false;
                    }

                    return await _apiDataService.CreateJsonFileAsync(fetchedData, fileConfig);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create JSON file: {FilePath}", fileConfig.OutputPath);
                    return false;
                }
            });
        createFileTask.ExecutionOrder = 2;

        // Add tasks to job
        job.AddTask(fetchTask);
        job.AddTask(createFileTask);

        _logger?.LogInformation("Created API to file job '{JobName}' with {TaskCount} tasks", 
            jobName, job.GetTaskCount());

        return job;
    }

    /// <summary>
    /// Creates configurations for a complete data pipeline
    /// </summary>
    /// <param name="apiUrl">The API URL to fetch data from</param>
    /// <param name="outputDirectory">The output directory for files</param>
    /// <param name="sftpHost">The SFTP host</param>
    /// <param name="sftpUsername">The SFTP username</param>
    /// <param name="sftpPassword">The SFTP password</param>
    /// <param name="sftpRemoteDirectory">The SFTP remote directory</param>
    /// <returns>Tuple of configurations for the pipeline</returns>
    public (ApiDataFetchConfig apiConfig, FileCreationConfig fileConfig, 
            ZipCompressionConfig zipConfig, SftpUploadConfig sftpConfig) 
        CreatePipelineConfigurations(
            string apiUrl,
            string outputDirectory,
            string sftpHost,
            string sftpUsername,
            string sftpPassword,
            string sftpRemoteDirectory)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"data_{timestamp}.json";
        var zipFileName = $"data_{timestamp}.zip";
        var filePath = Path.Combine(outputDirectory, fileName);
        var zipPath = Path.Combine(outputDirectory, zipFileName);

        var apiConfig = new ApiDataFetchConfig
        {
            ApiUrl = apiUrl,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        var fileConfig = new FileCreationConfig
        {
            OutputPath = filePath,
            OverwriteExisting = true
        };

        var zipConfig = new ZipCompressionConfig
        {
            SourceFilePath = filePath,
            ZipFilePath = zipPath,
            CompressionLevel = System.IO.Compression.CompressionLevel.Optimal,
            DeleteSourceAfterCompression = true
        };

        var sftpConfig = new SftpUploadConfig
        {
            Host = sftpHost,
            Username = sftpUsername,
            Password = sftpPassword,
            LocalFilePath = zipPath,
            RemoteDirectoryPath = sftpRemoteDirectory,
            CreateRemoteDirectories = true
        };

        return (apiConfig, fileConfig, zipConfig, sftpConfig);
    }

    /// <summary>
    /// Creates a job group with multiple related jobs
    /// </summary>
    /// <param name="groupName">The name of the job group</param>
    /// <param name="description">The description of the job group</param>
    /// <returns>A new job group</returns>
    public JobGroup CreateJobGroup(string groupName, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(groupName);
        
        _logger?.LogInformation("Creating job group: {GroupName}", groupName);
        
        return new JobGroup(groupName, description);
    }
}