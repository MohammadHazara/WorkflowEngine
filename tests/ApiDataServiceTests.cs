using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Net;
using System.Text;
using System.Text.Json;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Tests;

/// <summary>
/// Tests for ApiDataService functionality
/// </summary>
[TestClass]
public sealed class ApiDataServiceTests
{
    private HttpClient _httpClient = null!;
    private ApiDataService _apiDataService = null!;
    private ILogger<ApiDataService> _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<ApiDataService>>();
        
        // Create HttpClient with custom message handler for testing
        var handler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(handler);
        
        _apiDataService = new ApiDataService(_httpClient, _logger);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    [TestMethod]
    public async Task FetchDataAsync_WithValidConfig_ShouldReturnData()
    {
        // Arrange
        var testData = JsonSerializer.Serialize(new { message = "test data", timestamp = DateTime.UtcNow });
        var config = new ApiDataFetchConfig
        {
            ApiUrl = "https://api.example.com/data",
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        // Configure the test handler to return success
        if (_httpClient.GetHttpClientHandler() is TestHttpMessageHandler handler)
        {
            handler.SetResponse(HttpStatusCode.OK, testData);
        }

        // Act
        var result = await _apiDataService.FetchDataAsync(config);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testData, result);
    }

    [TestMethod]
    public async Task CreateJsonFileAsync_WithValidData_ShouldCreateFile()
    {
        // Arrange
        var testData = JsonSerializer.Serialize(new { message = "test", value = 123 });
        var tempPath = Path.GetTempFileName();
        var config = new FileCreationConfig
        {
            OutputPath = tempPath,
            OverwriteExisting = true
        };

        try
        {
            // Act
            var result = await _apiDataService.CreateJsonFileAsync(testData, config);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(tempPath));
            
            var fileContent = await File.ReadAllTextAsync(tempPath);
            Assert.IsNotNull(fileContent);
            
            // Verify it's valid JSON
            var parsedData = JsonDocument.Parse(fileContent);
            Assert.IsNotNull(parsedData);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [TestMethod]
    public async Task CompressFileAsync_WithValidFile_ShouldCreateZip()
    {
        // Arrange
        var tempJsonFile = Path.GetTempFileName();
        var tempZipFile = Path.ChangeExtension(tempJsonFile, ".zip");
        var testContent = JsonSerializer.Serialize(new { data = "test compression" });

        await File.WriteAllTextAsync(tempJsonFile, testContent);

        var config = new ZipCompressionConfig
        {
            SourceFilePath = tempJsonFile,
            ZipFilePath = tempZipFile,
            CompressionLevel = System.IO.Compression.CompressionLevel.Optimal,
            DeleteSourceAfterCompression = false
        };

        try
        {
            // Act
            var result = await _apiDataService.CompressFileAsync(config);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(tempZipFile));
            Assert.IsTrue(new FileInfo(tempZipFile).Length > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempJsonFile)) File.Delete(tempJsonFile);
            if (File.Exists(tempZipFile)) File.Delete(tempZipFile);
        }
    }

    [TestMethod]
    public async Task FetchDataAsync_WithInvalidUrl_ShouldThrowException()
    {
        // Arrange
        var config = new ApiDataFetchConfig
        {
            ApiUrl = "https://invalid-url-that-does-not-exist.com/api",
            TimeoutSeconds = 5,
            MaxRetries = 2
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<HttpRequestException>(
            () => _apiDataService.FetchDataAsync(config));
    }

    [TestMethod]
    public async Task CreateJsonFileAsync_WithInvalidJson_ShouldReturnFalse()
    {
        // Arrange
        var invalidJson = "{ invalid json content";
        var tempPath = Path.GetTempFileName();
        var config = new FileCreationConfig
        {
            OutputPath = tempPath,
            OverwriteExisting = true
        };

        try
        {
            // Act
            var result = await _apiDataService.CreateJsonFileAsync(invalidJson, config);

            // Assert
            Assert.IsFalse(result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [TestMethod]
    public async Task CompressFileAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var config = new ZipCompressionConfig
        {
            SourceFilePath = "non-existent-file.json",
            ZipFilePath = "output.zip",
            CompressionLevel = System.IO.Compression.CompressionLevel.Optimal
        };

        // Act
        var result = await _apiDataService.CompressFileAsync(config);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UploadFileAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var config = new ApiUploadConfig
        {
            UploadUrl = "https://api.example.com/upload",
            FilePath = "non-existent-file.zip",
            FileFieldName = "file"
        };

        // Act
        var result = await _apiDataService.UploadFileAsync(config);

        // Assert
        Assert.IsFalse(result);
    }
}

/// <summary>
/// Custom HTTP message handler for testing
/// </summary>
public sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _content = string.Empty;

    public void SetResponse(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}

/// <summary>
/// Extension method to access HttpClient's message handler for testing
/// </summary>
public static class HttpClientExtensions
{
    public static HttpMessageHandler? GetHttpClientHandler(this HttpClient httpClient)
    {
        // Note: This is a simplified approach for testing.
        // In production, you'd use a proper mocking framework setup.
        return null; // Placeholder - would need reflection or DI setup for full implementation
    }
}