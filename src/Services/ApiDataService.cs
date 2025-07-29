using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for handling API data operations with performance optimization
/// </summary>
public sealed class ApiDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiDataService>? _logger;

    /// <summary>
    /// Initializes a new instance of the ApiDataService class
    /// </summary>
    /// <param name="httpClient">HTTP client for API operations</param>
    /// <param name="logger">Optional logger for service operations</param>
    public ApiDataService(HttpClient httpClient, ILogger<ApiDataService>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    /// <summary>
    /// Fetches data from an API endpoint with retry logic
    /// </summary>
    /// <param name="config">Configuration for the API fetch operation</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The fetched data as a string</returns>
    public async Task<string> FetchDataAsync(ApiDataFetchConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        _logger?.LogInformation("Starting API data fetch from {ApiUrl}", config.ApiUrl);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

        var request = CreateHttpRequest(config);
        
        for (var attempt = 1; attempt <= config.MaxRetries; attempt++)
        {
            try
            {
                using var response = await _httpClient.SendAsync(request, cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cts.Token);
                    _logger?.LogInformation("Successfully fetched {ContentLength} characters from API", content.Length);
                    return content;
                }

                _logger?.LogWarning("API request failed with status {StatusCode}: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);

                if (attempt < config.MaxRetries)
                {
                    await DelayBeforeRetryAsync(attempt, cts.Token);
                }
            }
            catch (Exception ex) when (attempt < config.MaxRetries)
            {
                _logger?.LogWarning(ex, "API request attempt {Attempt} failed, retrying...", attempt);
                await DelayBeforeRetryAsync(attempt, cts.Token);
            }
        }

        throw new HttpRequestException($"Failed to fetch data from {config.ApiUrl} after {config.MaxRetries} attempts");
    }

    /// <summary>
    /// Creates a JSON file from the provided data
    /// </summary>
    /// <param name="data">The data to write to the file</param>
    /// <param name="config">Configuration for file creation</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if file creation was successful</returns>
    public async Task<bool> CreateJsonFileAsync(string data, FileCreationConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(config);

        _logger?.LogInformation("Creating JSON file at {OutputPath}", config.OutputPath);

        try
        {
            // Parse and re-serialize to ensure valid JSON formatting
            using var document = JsonDocument.Parse(data);
            var formattedJson = JsonSerializer.Serialize(document.RootElement, config.JsonOptions);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(config.OutputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if file exists and handle accordingly
            if (File.Exists(config.OutputPath) && !config.OverwriteExisting)
            {
                _logger?.LogWarning("File already exists and overwrite is disabled: {FilePath}", config.OutputPath);
                return false;
            }

            await File.WriteAllTextAsync(config.OutputPath, formattedJson, Encoding.UTF8, cancellationToken);
            
            var fileInfo = new FileInfo(config.OutputPath);
            _logger?.LogInformation("Successfully created JSON file: {FilePath} ({FileSize} bytes)", 
                config.OutputPath, fileInfo.Length);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create JSON file at {OutputPath}", config.OutputPath);
            return false;
        }
    }

    /// <summary>
    /// Compresses a file into a ZIP archive
    /// </summary>
    /// <param name="config">Configuration for ZIP compression</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if compression was successful</returns>
    public async Task<bool> CompressFileAsync(ZipCompressionConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        _logger?.LogInformation("Compressing file {SourceFile} to {ZipFile}", 
            config.SourceFilePath, config.ZipFilePath);

        try
        {
            if (!File.Exists(config.SourceFilePath))
            {
                _logger?.LogError("Source file not found: {SourceFilePath}", config.SourceFilePath);
                return false;
            }

            // Ensure output directory exists
            var directory = Path.GetDirectoryName(config.ZipFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Delete existing ZIP file if it exists
            if (File.Exists(config.ZipFilePath))
            {
                File.Delete(config.ZipFilePath);
            }

            await Task.Run(() =>
            {
                using var archive = ZipFile.Open(config.ZipFilePath, ZipArchiveMode.Create);
                var fileName = Path.GetFileName(config.SourceFilePath);
                archive.CreateEntryFromFile(config.SourceFilePath, fileName, config.CompressionLevel);
            }, cancellationToken);

            var originalSize = new FileInfo(config.SourceFilePath).Length;
            var compressedSize = new FileInfo(config.ZipFilePath).Length;
            var compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;

            _logger?.LogInformation("Successfully compressed file. Original: {OriginalSize} bytes, " +
                "Compressed: {CompressedSize} bytes, Ratio: {CompressionRatio:F1}%", 
                originalSize, compressedSize, compressionRatio);

            // Delete source file if configured to do so
            if (config.DeleteSourceAfterCompression)
            {
                File.Delete(config.SourceFilePath);
                _logger?.LogInformation("Deleted source file: {SourceFilePath}", config.SourceFilePath);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to compress file {SourceFile} to {ZipFile}", 
                config.SourceFilePath, config.ZipFilePath);
            return false;
        }
    }

    /// <summary>
    /// Uploads a file to an API endpoint
    /// </summary>
    /// <param name="config">Configuration for the upload operation</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if upload was successful</returns>
    public async Task<bool> UploadFileAsync(ApiUploadConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        _logger?.LogInformation("Uploading file {FilePath} to {UploadUrl}", config.FilePath, config.UploadUrl);

        try
        {
            if (!File.Exists(config.FilePath))
            {
                _logger?.LogError("File not found for upload: {FilePath}", config.FilePath);
                return false;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

            using var content = new MultipartFormDataContent();
            
            // Add form data
            foreach (var (key, value) in config.FormData)
            {
                content.Add(new StringContent(value), key);
            }

            // Add file content
            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(config.FilePath, cts.Token));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, config.FileFieldName, Path.GetFileName(config.FilePath));

            using var request = new HttpRequestMessage(HttpMethod.Post, config.UploadUrl)
            {
                Content = content
            };

            // Add headers
            foreach (var (key, value) in config.Headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }

            // Add authentication if provided
            if (!string.IsNullOrEmpty(config.AuthToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AuthToken);
            }

            using var response = await _httpClient.SendAsync(request, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var fileInfo = new FileInfo(config.FilePath);
                _logger?.LogInformation("Successfully uploaded file {FilePath} ({FileSize} bytes)", 
                    config.FilePath, fileInfo.Length);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
            _logger?.LogError("Upload failed with status {StatusCode}: {ReasonPhrase}. Response: {Response}", 
                response.StatusCode, response.ReasonPhrase, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to upload file {FilePath} to {UploadUrl}", config.FilePath, config.UploadUrl);
            return false;
        }
    }

    /// <summary>
    /// Creates an HTTP request with the specified configuration
    /// </summary>
    /// <param name="config">API fetch configuration</param>
    /// <returns>Configured HTTP request message</returns>
    private static HttpRequestMessage CreateHttpRequest(ApiDataFetchConfig config)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, config.ApiUrl);

        // Add headers
        foreach (var (key, value) in config.Headers)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }

        // Add authentication if provided
        if (!string.IsNullOrEmpty(config.AuthToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AuthToken);
        }

        return request;
    }

    /// <summary>
    /// Implements exponential backoff delay for retry attempts
    /// </summary>
    /// <param name="attempt">Current attempt number</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
        _logger?.LogInformation("Waiting {DelayMs}ms before retry attempt {Attempt}", delay.TotalMilliseconds, attempt + 1);
        await Task.Delay(delay, cancellationToken);
    }
}