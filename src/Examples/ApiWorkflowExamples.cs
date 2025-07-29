using System.Text.Json;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Examples;

/// <summary>
/// Practical examples of how to use the API workflow system
/// </summary>
public static class ApiWorkflowExamples
{
    /// <summary>
    /// Example 1: Complete API-to-file-to-upload workflow
    /// This fetches data from JSONPlaceholder API, creates a JSON file, compresses it, and simulates upload
    /// </summary>
    public static async Task<bool> RunCompleteApiWorkflowExample()
    {
        Console.WriteLine("🚀 Starting Complete API Workflow Example");
        Console.WriteLine("===========================================");

        using var httpClient = new HttpClient();
        var apiDataService = new ApiDataService(httpClient);
        var sftpService = new SftpService();
        var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

        // Create configurations for a real API example
        var (apiConfig, fileConfig, zipConfig, uploadConfig) = workflowBuilder.CreateDefaultConfigurations(
            apiUrl: "https://jsonplaceholder.typicode.com/posts", // Free test API
            outputDirectory: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WorkflowEngine_Output"),
            uploadUrl: "https://httpbin.org/post", // Test upload endpoint
            authToken: null
        );

        // Create and execute the workflow
        var workflow = workflowBuilder.CreateApiToFileToUploadWorkflow(
            "API Data Processing Pipeline",
            apiConfig,
            fileConfig,
            zipConfig,
            uploadConfig
        );

        Console.WriteLine($"📋 Workflow Created: {workflow.Name}");
        Console.WriteLine($"📁 Output Directory: {Path.GetDirectoryName(fileConfig.OutputPath)}");
        Console.WriteLine($"🔗 API URL: {apiConfig.ApiUrl}");
        Console.WriteLine($"📤 Upload URL: {uploadConfig.UploadUrl}");
        Console.WriteLine();

        // Execute each step manually to show progress
        var steps = workflow.GetSteps();
        var overallSuccess = true;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            Console.WriteLine($"⏳ Executing Step {step.Id}: {step.Name}");
            
            var stepResult = await step.ExecuteAsync();
            
            if (stepResult)
            {
                Console.WriteLine($"✅ Step {step.Id} completed successfully");
                if (step.OnSuccess != null)
                {
                    await step.OnSuccess();
                }
            }
            else
            {
                Console.WriteLine($"❌ Step {step.Id} failed");
                if (step.OnFailure != null)
                {
                    await step.OnFailure();
                }
                overallSuccess = false;
                break;
            }
            
            Console.WriteLine();
        }

        if (overallSuccess)
        {
            Console.WriteLine("🎉 Complete workflow executed successfully!");
            Console.WriteLine($"📄 JSON file created: {fileConfig.OutputPath}");
            Console.WriteLine($"🗜️ ZIP file created: {zipConfig.ZipFilePath}");
            
            // Show file sizes
            if (File.Exists(fileConfig.OutputPath))
            {
                var jsonSize = new FileInfo(fileConfig.OutputPath).Length;
                Console.WriteLine($"📊 JSON file size: {jsonSize:N0} bytes");
            }
            
            if (File.Exists(zipConfig.ZipFilePath))
            {
                var zipSize = new FileInfo(zipConfig.ZipFilePath).Length;
                Console.WriteLine($"📊 ZIP file size: {zipSize:N0} bytes");
            }
        }
        else
        {
            Console.WriteLine("💥 Workflow execution failed");
        }

        return overallSuccess;
    }

    /// <summary>
    /// Example 2: Simple API-to-file workflow (just fetch and save)
    /// </summary>
    public static async Task<bool> RunSimpleApiWorkflowExample()
    {
        Console.WriteLine("📝 Starting Simple API Workflow Example");
        Console.WriteLine("=======================================");

        using var httpClient = new HttpClient();
        var apiDataService = new ApiDataService(httpClient);
        var sftpService = new SftpService();
        var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

        var apiConfig = new ApiDataFetchConfig
        {
            ApiUrl = "https://jsonplaceholder.typicode.com/users",
            TimeoutSeconds = 30,
            MaxRetries = 3,
            Headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" }
            }
        };

        var outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "WorkflowEngine_Output",
            $"users_data_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        );

        var fileConfig = new FileCreationConfig
        {
            OutputPath = outputPath,
            OverwriteExisting = true
        };

        var workflow = workflowBuilder.CreateApiToFileWorkflow(
            "Simple User Data Fetch",
            apiConfig,
            fileConfig
        );

        Console.WriteLine($"📋 Workflow: {workflow.Name}");
        Console.WriteLine($"🔗 Fetching from: {apiConfig.ApiUrl}");
        Console.WriteLine($"💾 Saving to: {outputPath}");
        Console.WriteLine();

        var steps = workflow.GetSteps();
        var success = true;

        foreach (var step in steps)
        {
            Console.WriteLine($"⏳ Executing: {step.Name}");
            var result = await step.ExecuteAsync();
            
            if (result)
            {
                Console.WriteLine($"✅ {step.Name} completed");
            }
            else
            {
                Console.WriteLine($"❌ {step.Name} failed");
                success = false;
                break;
            }
        }

        if (success && File.Exists(outputPath))
        {
            var fileSize = new FileInfo(outputPath).Length;
            Console.WriteLine($"🎉 Simple workflow completed! File size: {fileSize:N0} bytes");
            
            // Show a preview of the data
            var content = await File.ReadAllTextAsync(outputPath);
            var data = JsonSerializer.Deserialize<JsonElement[]>(content);
            Console.WriteLine($"📊 Retrieved {data?.Length ?? 0} user records");
        }

        return success;
    }

    /// <summary>
    /// Example 3: Custom workflow with your own API endpoints
    /// </summary>
    public static async Task<bool> RunCustomApiWorkflowExample(string apiUrl, string uploadUrl, string? authToken = null)
    {
        Console.WriteLine("🛠️ Starting Custom API Workflow Example");
        Console.WriteLine("=========================================");

        using var httpClient = new HttpClient();
        var apiDataService = new ApiDataService(httpClient);
        var sftpService = new SftpService();
        var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

        var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WorkflowEngine_Output");
        
        var (apiConfig, fileConfig, zipConfig, uploadConfig) = workflowBuilder.CreateDefaultConfigurations(
            apiUrl, outputDir, uploadUrl, authToken);

        // Customize configurations as needed
        apiConfig.TimeoutSeconds = 60; // Longer timeout
        zipConfig.CompressionLevel = System.IO.Compression.CompressionLevel.SmallestSize; // Maximum compression
        
        var workflow = workflowBuilder.CreateApiToFileToUploadWorkflow(
            "Custom API Processing Workflow",
            apiConfig,
            fileConfig,
            zipConfig,
            uploadConfig
        );

        Console.WriteLine($"📋 Custom Workflow: {workflow.Name}");
        Console.WriteLine($"🔗 API: {apiUrl}");
        Console.WriteLine($"📤 Upload: {uploadUrl}");
        Console.WriteLine($"🔐 Auth: {(authToken != null ? "Enabled" : "None")}");
        Console.WriteLine();

        // Execute workflow with progress tracking
        var steps = workflow.GetSteps();
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var progress = (i + 1) * 100 / steps.Count;
            
            Console.WriteLine($"[{progress}%] {step.Name}");
            
            var result = await step.ExecuteAsync();
            if (!result)
            {
                Console.WriteLine($"❌ Workflow failed at step: {step.Name}");
                return false;
            }
        }

        Console.WriteLine("🎉 Custom workflow completed successfully!");
        return true;
    }

    /// <summary>
    /// Demonstrates performance with larger datasets
    /// </summary>
    public static async Task<bool> RunPerformanceTestExample()
    {
        Console.WriteLine("⚡ Starting Performance Test Example");
        Console.WriteLine("====================================");

        var startTime = DateTime.UtcNow;
        
        using var httpClient = new HttpClient();
        var apiDataService = new ApiDataService(httpClient);
        var sftpService = new SftpService();
        var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

        // Fetch a larger dataset for performance testing
        var (apiConfig, fileConfig, zipConfig, uploadConfig) = workflowBuilder.CreateDefaultConfigurations(
            apiUrl: "https://jsonplaceholder.typicode.com/comments", // Larger dataset (~500 records)
            outputDirectory: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WorkflowEngine_Performance"),
            uploadUrl: "https://httpbin.org/post",
            authToken: null
        );

        var workflow = workflowBuilder.CreateApiToFileToUploadWorkflow(
            "Performance Test Workflow",
            apiConfig,
            fileConfig,
            zipConfig,
            uploadConfig
        );

        Console.WriteLine("📊 Testing performance with larger dataset...");
        Console.WriteLine($"⏱️ Start time: {startTime:HH:mm:ss}");

        var steps = workflow.GetSteps();
        var stepTimes = new List<(string name, TimeSpan duration)>();

        foreach (var step in steps)
        {
            var stepStart = DateTime.UtcNow;
            Console.WriteLine($"⏳ {step.Name}...");
            
            var result = await step.ExecuteAsync();
            var stepDuration = DateTime.UtcNow - stepStart;
            stepTimes.Add((step.Name, stepDuration));
            
            if (result)
            {
                Console.WriteLine($"✅ {step.Name} completed in {stepDuration.TotalMilliseconds:F0}ms");
            }
            else
            {
                Console.WriteLine($"❌ {step.Name} failed");
                return false;
            }
        }

        var totalTime = DateTime.UtcNow - startTime;
        
        Console.WriteLine();
        Console.WriteLine("📈 Performance Results:");
        Console.WriteLine($"⏱️ Total execution time: {totalTime.TotalSeconds:F2} seconds");
        
        foreach (var (name, duration) in stepTimes)
        {
            Console.WriteLine($"   {name}: {duration.TotalMilliseconds:F0}ms");
        }

        // Show compression results
        if (File.Exists(fileConfig.OutputPath) && File.Exists(zipConfig.ZipFilePath))
        {
            var originalSize = new FileInfo(fileConfig.OutputPath).Length;
            var compressedSize = new FileInfo(zipConfig.ZipFilePath).Length;
            var compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;
            
            Console.WriteLine($"🗜️ Compression: {originalSize:N0} → {compressedSize:N0} bytes ({compressionRatio:F1}% reduction)");
        }

        return true;
    }
}