using Microsoft.Extensions.Logging;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Examples;

/// <summary>
/// Examples demonstrating the new hierarchical job system with JobGroups, Jobs, and Tasks
/// </summary>
public static class JobSystemExamples
{
    /// <summary>
    /// Demonstrates creating a complete data pipeline job group with multiple jobs
    /// </summary>
    public static async Task<bool> RunDataPipelineJobGroupExample()
    {
        Console.WriteLine("🚀 Running Data Pipeline Job Group Example");
        Console.WriteLine("=====================================");

        try
        {
            // Initialize services (in a real app, these would be injected)
            var httpClient = new HttpClient();
            var apiDataService = new ApiDataService(httpClient);
            var sftpService = new SftpService();
            var jobBuilder = new JobBuilder(apiDataService, sftpService);
            var jobExecutor = new JobExecutor();

            // Create a job group for data processing workflows
            var jobGroup = jobBuilder.CreateJobGroup(
                "Data Processing Pipeline", 
                "Complete data processing pipeline with multiple jobs");

            Console.WriteLine($"📁 Created job group: {jobGroup.Name}");

            // Job 1: Customer Data Pipeline
            var customerDataJob = await CreateCustomerDataPipelineJob(jobBuilder);
            customerDataJob.ExecutionOrder = 1;
            jobGroup.AddJob(customerDataJob);

            // Job 2: Product Data Pipeline  
            var productDataJob = await CreateProductDataPipelineJob(jobBuilder);
            productDataJob.ExecutionOrder = 2;
            jobGroup.AddJob(productDataJob);

            // Job 3: Order Data Pipeline
            var orderDataJob = await CreateOrderDataPipelineJob(jobBuilder);
            orderDataJob.ExecutionOrder = 3;
            jobGroup.AddJob(orderDataJob);

            Console.WriteLine($"📋 Job group now contains {jobGroup.GetJobCount()} jobs");

            // Execute the entire job group
            Console.WriteLine();
            Console.WriteLine("⚡ Executing job group...");
            var success = await jobExecutor.ExecuteJobGroupAsync(jobGroup);

            if (success)
            {
                Console.WriteLine("🎉 Job group execution completed successfully!");
                return true;
            }
            else
            {
                Console.WriteLine("❌ Job group execution failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Demonstrates creating and executing a single data pipeline job
    /// </summary>
    public static async Task<bool> RunSingleDataPipelineJobExample()
    {
        Console.WriteLine("🔧 Running Single Data Pipeline Job Example");
        Console.WriteLine("==========================================");

        try
        {
            // Initialize services
            var httpClient = new HttpClient();
            var apiDataService = new ApiDataService(httpClient);
            var sftpService = new SftpService();
            var jobBuilder = new JobBuilder(apiDataService, sftpService);
            var jobExecutor = new JobExecutor();

            // Create pipeline configurations
            var (apiConfig, fileConfig, zipConfig, sftpConfig) = jobBuilder.CreatePipelineConfigurations(
                apiUrl: "https://jsonplaceholder.typicode.com/posts",
                outputDirectory: Path.Combine(Path.GetTempPath(), "workflow_output"),
                sftpHost: "test.rebex.net",
                sftpUsername: "demo",
                sftpPassword: "password",
                sftpRemoteDirectory: "/uploads"
            );

            // Create the data pipeline job
            var job = jobBuilder.CreateDataPipelineJob(
                "API Data Pipeline",
                apiConfig,
                fileConfig,
                zipConfig,
                sftpConfig);

            Console.WriteLine($"🔨 Created job: {job.Name} ({job.JobType})");
            Console.WriteLine($"📝 Job contains {job.GetTaskCount()} tasks:");

            foreach (var task in job.Tasks.OrderBy(t => t.ExecutionOrder))
            {
                Console.WriteLine($"  {task.ExecutionOrder}. {task.Name} ({task.TaskType})");
            }

            // Execute with progress reporting
            Console.WriteLine();
            Console.WriteLine("⚡ Executing job with progress tracking...");

            var progress = new Progress<int>(percentage =>
            {
                Console.Write($"\r[{new string('#', percentage / 5)}{new string('-', 20 - percentage / 5)}] {percentage}%");
            });

            var execution = await jobExecutor.ExecuteJobWithProgressAsync(job, progress);

            Console.WriteLine(); // New line after progress bar
            Console.WriteLine($"📊 Execution Status: {execution.Status}");
            Console.WriteLine($"⏱️  Duration: {execution.GetDuration()?.TotalSeconds:F2} seconds");

            if (execution.Status == JobExecutionStatus.Completed)
            {
                Console.WriteLine("🎉 Job execution completed successfully!");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Job execution failed: {execution.ErrorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Demonstrates creating a simple API-to-file job
    /// </summary>
    public static async Task<bool> RunSimpleApiToFileJobExample()
    {
        Console.WriteLine("📄 Running Simple API-to-File Job Example");
        Console.WriteLine("========================================");

        try
        {
            // Initialize services
            var httpClient = new HttpClient();
            var apiDataService = new ApiDataService(httpClient);
            var sftpService = new SftpService();
            var jobBuilder = new JobBuilder(apiDataService, sftpService);
            var jobExecutor = new JobExecutor();

            // Create configurations for simple API to file
            var apiConfig = new ApiDataFetchConfig
            {
                ApiUrl = "https://jsonplaceholder.typicode.com/users",
                TimeoutSeconds = 30,
                MaxRetries = 3
            };

            var outputPath = Path.Combine(Path.GetTempPath(), "workflow_output", "users.json");
            var fileConfig = new FileCreationConfig
            {
                OutputPath = outputPath,
                OverwriteExisting = true
            };

            // Create the job
            var job = jobBuilder.CreateApiToFileJob("User Data Extraction", apiConfig, fileConfig);

            Console.WriteLine($"🔨 Created job: {job.Name} ({job.JobType})");
            Console.WriteLine($"📝 Job contains {job.GetTaskCount()} tasks:");

            foreach (var task in job.Tasks.OrderBy(t => t.ExecutionOrder))
            {
                Console.WriteLine($"  {task.ExecutionOrder}. {task.Name} ({task.TaskType})");
            }

            // Execute the job
            Console.WriteLine();
            Console.WriteLine("⚡ Executing job...");

            var execution = await jobExecutor.ExecuteJobAsync(job);

            Console.WriteLine($"📊 Execution Status: {execution.Status}");
            Console.WriteLine($"⏱️  Duration: {execution.GetDuration()?.TotalSeconds:F2} seconds");

            if (execution.Status == JobExecutionStatus.Completed && File.Exists(outputPath))
            {
                var fileSize = new FileInfo(outputPath).Length;
                Console.WriteLine($"📁 Created file: {outputPath} ({fileSize:N0} bytes)");
                Console.WriteLine("🎉 Job execution completed successfully!");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Job execution failed: {execution.ErrorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a customer data pipeline job
    /// </summary>
    private static Task<Job> CreateCustomerDataPipelineJob(JobBuilder jobBuilder)
    {
        var (apiConfig, fileConfig, zipConfig, sftpConfig) = jobBuilder.CreatePipelineConfigurations(
            apiUrl: "https://jsonplaceholder.typicode.com/users",
            outputDirectory: Path.Combine(Path.GetTempPath(), "workflow_output", "customers"),
            sftpHost: "test.rebex.net",
            sftpUsername: "demo", 
            sftpPassword: "password",
            sftpRemoteDirectory: "/customers"
        );

        var job = jobBuilder.CreateDataPipelineJob(
            "Customer Data Pipeline",
            apiConfig,
            fileConfig,
            zipConfig,
            sftpConfig);

        Console.WriteLine($"  ✅ Created Customer Data Pipeline job with {job.GetTaskCount()} tasks");
        return Task.FromResult(job);
    }

    /// <summary>
    /// Creates a product data pipeline job
    /// </summary>
    private static Task<Job> CreateProductDataPipelineJob(JobBuilder jobBuilder)
    {
        var (apiConfig, fileConfig, zipConfig, sftpConfig) = jobBuilder.CreatePipelineConfigurations(
            apiUrl: "https://jsonplaceholder.typicode.com/posts",
            outputDirectory: Path.Combine(Path.GetTempPath(), "workflow_output", "products"),
            sftpHost: "test.rebex.net",
            sftpUsername: "demo",
            sftpPassword: "password", 
            sftpRemoteDirectory: "/products"
        );

        var job = jobBuilder.CreateDataPipelineJob(
            "Product Data Pipeline",
            apiConfig,
            fileConfig,
            zipConfig,
            sftpConfig);

        Console.WriteLine($"  ✅ Created Product Data Pipeline job with {job.GetTaskCount()} tasks");
        return Task.FromResult(job);
    }

    /// <summary>
    /// Creates an order data pipeline job
    /// </summary>
    private static Task<Job> CreateOrderDataPipelineJob(JobBuilder jobBuilder)
    {
        var (apiConfig, fileConfig, zipConfig, sftpConfig) = jobBuilder.CreatePipelineConfigurations(
            apiUrl: "https://jsonplaceholder.typicode.com/albums",
            outputDirectory: Path.Combine(Path.GetTempPath(), "workflow_output", "orders"),
            sftpHost: "test.rebex.net",
            sftpUsername: "demo",
            sftpPassword: "password",
            sftpRemoteDirectory: "/orders"
        );

        var job = jobBuilder.CreateDataPipelineJob(
            "Order Data Pipeline",
            apiConfig,
            fileConfig,
            zipConfig,
            sftpConfig);

        Console.WriteLine($"  ✅ Created Order Data Pipeline job with {job.GetTaskCount()} tasks");
        return Task.FromResult(job);
    }
}