using WorkflowEngine.Examples;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Examples;

/// <summary>
/// Console application demonstrating the complete API → file → zip → SFTP workflow
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🌟 WorkflowEngine - API to SFTP Pipeline Demo");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        // Example 1: Complete API → file → zip → SFTP workflow
        Console.WriteLine("Example 1: Complete API → File → ZIP → SFTP Workflow");
        Console.WriteLine("----------------------------------------------------");
        
        // Note: Replace these with your actual SFTP server details
        var sftpHost = "test.rebex.net"; // Public test SFTP server
        var sftpUsername = "demo";
        var sftpPassword = "password";
        var sftpRemoteDirectory = "/";

        try
        {
            var result = await RunApiToSftpDemo(sftpHost, sftpUsername, sftpPassword, sftpRemoteDirectory);
            
            if (result)
            {
                Console.WriteLine("✅ Demo completed successfully!");
            }
            else
            {
                Console.WriteLine("❌ Demo failed. Check SFTP credentials and connectivity.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo failed with error: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    /// <summary>
    /// Demonstrates the complete API → file → zip → SFTP workflow
    /// </summary>
    private static async Task<bool> RunApiToSftpDemo(string sftpHost, string sftpUsername, 
        string sftpPassword, string sftpRemoteDirectory)
    {
        using var httpClient = new HttpClient();
        var apiDataService = new ApiDataService(httpClient);
        var sftpService = new SftpService();
        var workflowBuilder = new ApiWorkflowBuilder(apiDataService, sftpService);

        // Create output directory
        var outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            "WorkflowEngine_SFTP_Demo");
        
        Directory.CreateDirectory(outputDirectory);

        // Configure the complete workflow
        var (apiConfig, fileConfig, zipConfig, sftpConfig) = workflowBuilder.CreateSftpConfigurations(
            apiUrl: "https://jsonplaceholder.typicode.com/posts", // Free test API
            outputDirectory: outputDirectory,
            sftpHost: sftpHost,
            sftpUsername: sftpUsername,
            sftpPassword: sftpPassword,
            sftpRemoteDirectory: sftpRemoteDirectory
        );

        Console.WriteLine($"📋 Configuration:");
        Console.WriteLine($"   API: {apiConfig.ApiUrl}");
        Console.WriteLine($"   Output: {outputDirectory}");
        Console.WriteLine($"   SFTP: {sftpHost}:{sftpConfig.Port}");
        Console.WriteLine($"   Remote: {sftpRemoteDirectory}");
        Console.WriteLine();

        // Test SFTP connection first
        Console.WriteLine("🔍 Testing SFTP connection...");
        var connectionTest = await sftpService.TestConnectionAsync(sftpConfig);
        if (!connectionTest)
        {
            Console.WriteLine("❌ SFTP connection failed");
            return false;
        }
        Console.WriteLine("✅ SFTP connection successful");
        Console.WriteLine();

        // Create and execute the workflow
        var workflow = workflowBuilder.CreateApiToFileToSftpWorkflow(
            "Demo: API → File → ZIP → SFTP",
            apiConfig,
            fileConfig,
            zipConfig,
            sftpConfig
        );

        Console.WriteLine("🚀 Executing workflow steps:");
        var steps = workflow.GetSteps();
        
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            Console.Write($"   [{i + 1}/{steps.Count}] {step.Name}... ");
            
            var stepResult = await step.ExecuteAsync();
            
            if (stepResult)
            {
                Console.WriteLine("✅");
                
                // Show file information
                if (step.Name == "Create JSON File" && File.Exists(fileConfig.OutputPath))
                {
                    var fileInfo = new FileInfo(fileConfig.OutputPath);
                    Console.WriteLine($"       📄 Created: {Path.GetFileName(fileConfig.OutputPath)} ({fileInfo.Length:N0} bytes)");
                }
                else if (step.Name == "Compress to ZIP" && File.Exists(zipConfig.ZipFilePath))
                {
                    var zipInfo = new FileInfo(zipConfig.ZipFilePath);
                    var jsonInfo = new FileInfo(fileConfig.OutputPath);
                    var compressionRatio = (1.0 - (double)zipInfo.Length / jsonInfo.Length) * 100;
                    Console.WriteLine($"       🗜️ Compressed: {Path.GetFileName(zipConfig.ZipFilePath)} ({zipInfo.Length:N0} bytes, {compressionRatio:F1}% smaller)");
                }
                else if (step.Name == "Upload via SFTP")
                {
                    Console.WriteLine($"       📤 Uploaded to: {sftpHost}{sftpRemoteDirectory}");
                }
            }
            else
            {
                Console.WriteLine("❌");
                return false;
            }
        }

        Console.WriteLine();
        Console.WriteLine("🎉 Workflow completed successfully!");
        Console.WriteLine($"📁 Local files: {outputDirectory}");
        
        return true;
    }
}