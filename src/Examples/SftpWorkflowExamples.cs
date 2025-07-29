using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Examples;

/// <summary>
/// Practical examples of how to use the SFTP workflow system
/// </summary>
public static class SftpWorkflowExamples
{
    /// <summary>
    /// Example: Complete API-to-file-to-ZIP-to-SFTP workflow
    /// This fetches data from an API, creates a JSON file, compresses it, and uploads via SFTP
    /// </summary>
    public static async Task<bool> RunApiToSftpWorkflowExample()
    {
        Console.WriteLine("🚀 Starting API-to-SFTP Workflow Example");
        Console.WriteLine("==========================================");

        using var httpClient = new HttpClient();
        var apiDataService = new ApiDataService(httpClient);
        var sftpService = new SftpService();
        var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

        // Create configurations for the complete workflow
        var (apiConfig, fileConfig, zipConfig, sftpConfig) = workflowBuilder.CreateSftpConfigurations(
            apiUrl: "https://jsonplaceholder.typicode.com/posts", // Free test API
            outputDirectory: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WorkflowEngine_SFTP_Output"),
            sftpHost: "sftp.example.com", // Replace with your SFTP server
            sftpUsername: "your-username", // Replace with your username
            sftpPassword: "your-password", // Replace with your password or use key auth
            sftpRemoteDirectory: "/uploads/workflow-data"
        );

        // Optional: Configure for key-based authentication instead
        // sftpConfig.Password = null;
        // sftpConfig.PrivateKeyPath = "/path/to/your/private/key";
        // sftpConfig.PrivateKeyPassphrase = "key-passphrase-if-needed";

        // Create and execute the workflow
        var workflow = workflowBuilder.CreateApiToFileToSftpWorkflow(
            "API Data to SFTP Pipeline",
            apiConfig,
            fileConfig,
            zipConfig,
            sftpConfig
        );

        Console.WriteLine($"📋 Workflow Created: {workflow.Name}");
        Console.WriteLine($"📁 Output Directory: {Path.GetDirectoryName(fileConfig.OutputPath)}");
        Console.WriteLine($"🔗 API URL: {apiConfig.ApiUrl}");
        Console.WriteLine($"📤 SFTP Server: {sftpConfig.Host}:{sftpConfig.Port}");
        Console.WriteLine($"📂 Remote Path: {sftpConfig.RemoteDirectoryPath}");
        Console.WriteLine();

        // Test SFTP connection first
        Console.WriteLine("🔍 Testing SFTP connection...");
        var connectionTest = await sftpService.TestConnectionAsync(sftpConfig);
        if (!connectionTest)
        {
            Console.WriteLine("❌ SFTP connection test failed. Please check your credentials and server details.");
            return false;
        }
        Console.WriteLine("✅ SFTP connection test successful!");
        Console.WriteLine();

        // Execute each step with progress tracking
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
                
                // Show progress information
                if (step.Name == "Create JSON File" && File.Exists(fileConfig.OutputPath))
                {
                    var fileInfo = new FileInfo(fileConfig.OutputPath);
                    Console.WriteLine($"📄 JSON file created: {fileInfo.Length:N0} bytes");
                }
                else if (step.Name == "Compress to ZIP" && File.Exists(zipConfig.ZipFilePath))
                {
                    var zipInfo = new FileInfo(zipConfig.ZipFilePath);
                    var originalInfo = new FileInfo(fileConfig.OutputPath);
                    var compressionRatio = (1.0 - (double)zipInfo.Length / originalInfo.Length) * 100;
                    Console.WriteLine($"🗜️ ZIP file created: {zipInfo.Length:N0} bytes ({compressionRatio:F1}% compression)");
                }
                else if (step.Name == "Upload via SFTP")
                {
                    Console.WriteLine($"📤 File uploaded to: {sftpConfig.Host}{sftpConfig.RemoteDirectoryPath}");
                }
            }
            else
            {
                Console.WriteLine($"❌ Step {step.Id} failed");
                overallSuccess = false;
                break;
            }
            
            Console.WriteLine();
        }

        if (overallSuccess)
        {
            Console.WriteLine("🎉 Complete SFTP workflow executed successfully!");
            Console.WriteLine($"📄 JSON file: {fileConfig.OutputPath}");
            Console.WriteLine($"🗜️ ZIP file: {zipConfig.ZipFilePath}");
            Console.WriteLine($"📤 Uploaded to: {sftpConfig.Host}{sftpConfig.RemoteDirectoryPath}");
        }
        else
        {
            Console.WriteLine("💥 Workflow execution failed");
        }

        return overallSuccess;
    }

    /// <summary>
    /// Example: Simple file-to-SFTP workflow (compress existing file and upload)
    /// </summary>
    public static async Task<bool> RunFileToSftpExample(string sourceFilePath, string sftpHost, 
        string sftpUsername, string sftpPassword, string remoteDirectory)
    {
        Console.WriteLine("📁 Starting File-to-SFTP Workflow Example");
        Console.WriteLine("==========================================");

        if (!File.Exists(sourceFilePath))
        {
            Console.WriteLine($"❌ Source file not found: {sourceFilePath}");
            return false;
        }

        using var httpClient = new HttpClient();
        var apiDataService = new ApiDataService(httpClient);
        var sftpService = new SftpService();
        var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

        var outputDirectory = Path.GetDirectoryName(sourceFilePath) ?? Path.GetTempPath();
        var zipFilePath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(sourceFilePath)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");

        var zipConfig = new ZipCompressionConfig
        {
            SourceFilePath = sourceFilePath,
            ZipFilePath = zipFilePath,
            CompressionLevel = System.IO.Compression.CompressionLevel.Optimal,
            DeleteSourceAfterCompression = false
        };

        var sftpConfig = new SftpUploadConfig
        {
            Host = sftpHost,
            Username = sftpUsername,
            Password = sftpPassword,
            LocalFilePath = zipFilePath,
            RemoteDirectoryPath = remoteDirectory,
            OverwriteExisting = true,
            CreateRemoteDirectories = true
        };

        var workflow = workflowBuilder.CreateFileToSftpWorkflow(
            "File Compression and SFTP Upload",
            zipConfig,
            sftpConfig
        );

        Console.WriteLine($"📋 Workflow: {workflow.Name}");
        Console.WriteLine($"📄 Source: {sourceFilePath}");
        Console.WriteLine($"🗜️ ZIP: {zipFilePath}");
        Console.WriteLine($"📤 SFTP: {sftpHost}{remoteDirectory}");
        Console.WriteLine();

        // Test connection
        Console.WriteLine("🔍 Testing SFTP connection...");
        var connectionTest = await sftpService.TestConnectionAsync(sftpConfig);
        if (!connectionTest)
        {
            Console.WriteLine("❌ SFTP connection failed");
            return false;
        }
        Console.WriteLine("✅ SFTP connection successful");
        Console.WriteLine();

        // Execute workflow
        var steps = workflow.GetSteps();
        foreach (var step in steps)
        {
            Console.WriteLine($"⏳ {step.Name}...");
            var result = await step.ExecuteAsync();
            
            if (result)
            {
                Console.WriteLine($"✅ {step.Name} completed");
            }
            else
            {
                Console.WriteLine($"❌ {step.Name} failed");
                return false;
            }
        }

        var originalSize = new FileInfo(sourceFilePath).Length;
        var compressedSize = new FileInfo(zipFilePath).Length;
        var compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;

        Console.WriteLine();
        Console.WriteLine("🎉 File-to-SFTP workflow completed successfully!");
        Console.WriteLine($"📊 Original: {originalSize:N0} bytes");
        Console.WriteLine($"📊 Compressed: {compressedSize:N0} bytes ({compressionRatio:F1}% reduction)");

        return true;
    }

    /// <summary>
    /// Example: Test SFTP connection with different authentication methods
    /// </summary>
    public static async Task<bool> TestSftpConnectionExample()
    {
        Console.WriteLine("🔍 Testing SFTP Connection Examples");
        Console.WriteLine("====================================");

        var sftpService = new SftpService();

        // Test 1: Password authentication
        Console.WriteLine("Test 1: Password Authentication");
        var passwordConfig = new SftpUploadConfig
        {
            Host = "test.rebex.net", // Public test SFTP server
            Username = "demo",
            Password = "password",
            RemoteDirectoryPath = "/",
            ConnectionTimeoutSeconds = 10
        };

        var passwordResult = await sftpService.TestConnectionAsync(passwordConfig);
        Console.WriteLine($"Password auth result: {(passwordResult ? "✅ Success" : "❌ Failed")}");
        Console.WriteLine();

        // Test 2: Key authentication (example - replace with your key path)
        Console.WriteLine("Test 2: Key Authentication (Example)");
        var keyConfig = new SftpUploadConfig
        {
            Host = "your-sftp-server.com",
            Username = "your-username",
            PrivateKeyPath = "/path/to/your/private/key",
            PrivateKeyPassphrase = "your-key-passphrase", // Optional
            RemoteDirectoryPath = "/uploads",
            ConnectionTimeoutSeconds = 10,
            VerifyHostKey = true,
            HostKeyFingerprint = "aa:bb:cc:dd:ee:ff:00:11:22:33:44:55:66:77:88:99" // Your server's fingerprint
        };

        Console.WriteLine("⚠️  Key authentication test skipped (requires valid key file)");
        Console.WriteLine("   Configure keyConfig with your actual server details to test");
        Console.WriteLine();

        return passwordResult;
    }
}