using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Data;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Examples;

/// <summary>
/// Examples demonstrating job persistence and database operations
/// </summary>
public static class JobPersistenceExamples
{
    /// <summary>
    /// Demonstrates complete job persistence workflow from creation to execution tracking
    /// </summary>
    public static async Task<bool> RunJobPersistenceExample()
    {
        Console.WriteLine("💾 Running Job Persistence Example");
        Console.WriteLine("=================================");

        try
        {
            // Setup database context and services
            var options = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlite("Data Source=job_persistence_demo.db")
                .Options;

            await using var context = new WorkflowDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var repository = new JobRepositoryService(context);
            var httpClient = new HttpClient();
            var apiDataService = new ApiDataService(httpClient);
            var sftpService = new SftpService();
            var jobBuilder = new JobBuilder(apiDataService, sftpService);
            var jobExecutor = new JobExecutor();

            Console.WriteLine("📋 Step 1: Creating and persisting a job group");
            
            // Create job group
            var jobGroup = jobBuilder.CreateJobGroup(
                "E-commerce Data Processing",
                "Complete e-commerce data pipeline with customer, product, and order processing");

            // Persist job group to database
            var savedJobGroup = await repository.CreateJobGroupAsync(jobGroup);
            Console.WriteLine($"✅ Saved job group with ID: {savedJobGroup.Id}");

            Console.WriteLine("\n📋 Step 2: Creating and persisting jobs with tasks");

            // Create customer data pipeline job
            var (apiConfig, fileConfig, zipConfig, sftpConfig) = jobBuilder.CreatePipelineConfigurations(
                apiUrl: "https://jsonplaceholder.typicode.com/users",
                outputDirectory: Path.Combine(Path.GetTempPath(), "workflow_output", "customers"),
                sftpHost: "test.rebex.net",
                sftpUsername: "demo",
                sftpPassword: "password",
                sftpRemoteDirectory: "/customers"
            );

            var customerJob = jobBuilder.CreateDataPipelineJob(
                "Customer Data Pipeline",
                apiConfig, fileConfig, zipConfig, sftpConfig);

            customerJob.JobGroupId = savedJobGroup.Id;
            customerJob.ExecutionOrder = 1;

            // Persist job to database
            var savedCustomerJob = await repository.CreateJobAsync(customerJob);
            Console.WriteLine($"✅ Saved customer job with ID: {savedCustomerJob.Id} and {savedCustomerJob.TasksCollection.Count} tasks");

            // Show task configuration persistence
            foreach (var task in savedCustomerJob.TasksCollection.OrderBy(t => t.ExecutionOrder))
            {
                Console.WriteLine($"   Task {task.ExecutionOrder}: {task.Name} ({task.TaskType})");
                Console.WriteLine($"   Config: {task.ConfigurationData?[..Math.Min(100, task.ConfigurationData?.Length ?? 0)]}...");
            }

            Console.WriteLine("\n📋 Step 3: Executing job and tracking execution in database");

            // Execute job and track execution
            var execution = await jobExecutor.ExecuteJobAsync(savedCustomerJob);
            
            // Persist execution to database
            var savedExecution = await repository.CreateJobExecutionAsync(execution);
            Console.WriteLine($"✅ Saved job execution with ID: {savedExecution.Id}");
            Console.WriteLine($"   Status: {savedExecution.Status}");
            Console.WriteLine($"   Progress: {savedExecution.ProgressPercentage}%");
            Console.WriteLine($"   Duration: {savedExecution.GetDuration()?.TotalSeconds:F2} seconds");

            Console.WriteLine("\n📋 Step 4: Retrieving persisted data from database");

            // Retrieve job group with all related data
            var retrievedJobGroup = await repository.GetJobGroupByIdAsync(savedJobGroup.Id);
            if (retrievedJobGroup is not null)
            {
                Console.WriteLine($"📁 Retrieved job group: {retrievedJobGroup.Name}");
                Console.WriteLine($"   Contains {retrievedJobGroup.JobsCollection.Count} jobs");
                
                foreach (var job in retrievedJobGroup.JobsCollection.OrderBy(j => j.ExecutionOrder))
                {
                    Console.WriteLine($"   - Job: {job.Name} ({job.JobType}) with {job.TasksCollection.Count} tasks");
                }
            }

            // Retrieve execution history
            var executionHistory = await repository.GetJobExecutionHistoryAsync(savedCustomerJob.Id);
            Console.WriteLine($"📊 Found {executionHistory.Count} execution records for the job");

            Console.WriteLine("\n📋 Step 5: Performance analytics from persisted data");

            // Get job statistics
            var jobStats = await repository.GetJobExecutionStatisticsAsync(savedCustomerJob.Id);
            Console.WriteLine($"📈 Job Statistics:");
            Console.WriteLine($"   Total Executions: {jobStats.TotalExecutions}");
            Console.WriteLine($"   Success Rate: {jobStats.SuccessRate:F1}%");
            Console.WriteLine($"   Average Duration: {jobStats.AverageDurationMs:F0}ms");

            // Get system statistics
            var systemStats = await repository.GetSystemExecutionStatisticsAsync();
            Console.WriteLine($"🖥️  System Statistics:");
            Console.WriteLine($"   Total Jobs: {systemStats.TotalJobs}");
            Console.WriteLine($"   Total Executions: {systemStats.TotalExecutions}");
            Console.WriteLine($"   System Success Rate: {systemStats.SystemSuccessRate:F1}%");

            Console.WriteLine("\n📋 Step 6: Demonstrating job retrieval and re-execution");

            // Retrieve job by ID and re-execute
            var retrievedJob = await repository.GetJobByIdAsync(savedCustomerJob.Id);
            if (retrievedJob is not null)
            {
                Console.WriteLine($"🔄 Retrieved job: {retrievedJob.Name}");
                Console.WriteLine($"   Job contains {retrievedJob.TasksCollection.Count} tasks with configuration data");
                
                // The job can be re-executed using the persisted configuration
                Console.WriteLine("   ✅ Job is ready for re-execution with persisted configuration");
            }

            Console.WriteLine("\n🎉 Job persistence demonstration completed successfully!");
            Console.WriteLine("\nKey Persistence Features Demonstrated:");
            Console.WriteLine("✅ Job Groups, Jobs, and Tasks are fully persisted");
            Console.WriteLine("✅ Task configuration parameters stored as JSON");
            Console.WriteLine("✅ Execution history and progress tracking");
            Console.WriteLine("✅ Performance analytics and statistics");
            Console.WriteLine("✅ Complete audit trail of all operations");
            Console.WriteLine("✅ Jobs can be retrieved and re-executed from database");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Demonstrates bulk job operations and performance optimization
    /// </summary>
    public static async Task<bool> RunBulkJobPersistenceExample()
    {
        Console.WriteLine("\n⚡ Running Bulk Job Persistence Example");
        Console.WriteLine("======================================");

        try
        {
            var options = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlite("Data Source=bulk_job_demo.db")
                .Options;

            await using var context = new WorkflowDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var repository = new JobRepositoryService(context);
            var httpClient = new HttpClient();
            var apiDataService = new ApiDataService(httpClient);
            var sftpService = new SftpService();
            var jobBuilder = new JobBuilder(apiDataService, sftpService);

            Console.WriteLine("📋 Creating multiple job groups for performance testing");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Create multiple job groups
            var jobGroups = new List<JobGroup>();
            for (int i = 1; i <= 5; i++)
            {
                var jobGroup = jobBuilder.CreateJobGroup(
                    $"Data Pipeline Group {i}",
                    $"Automated data pipeline group #{i}");

                var savedJobGroup = await repository.CreateJobGroupAsync(jobGroup);
                jobGroups.Add(savedJobGroup);

                // Create multiple jobs per group
                for (int j = 1; j <= 3; j++)
                {
                    var (apiConfig, fileConfig, zipConfig, sftpConfig) = jobBuilder.CreatePipelineConfigurations(
                        apiUrl: $"https://jsonplaceholder.typicode.com/posts/{j}",
                        outputDirectory: Path.Combine(Path.GetTempPath(), "bulk_test", $"group_{i}"),
                        sftpHost: "test.rebex.net",
                        sftpUsername: "demo",
                        sftpPassword: "password",
                        sftpRemoteDirectory: $"/group_{i}/job_{j}"
                    );

                    var job = jobBuilder.CreateDataPipelineJob(
                        $"Pipeline Job {i}-{j}",
                        apiConfig, fileConfig, zipConfig, sftpConfig);

                    job.JobGroupId = savedJobGroup.Id;
                    job.ExecutionOrder = j;

                    await repository.CreateJobAsync(job);
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"✅ Created {jobGroups.Count} job groups with {jobGroups.Count * 3} jobs in {stopwatch.ElapsedMilliseconds}ms");

            // Demonstrate bulk retrieval with performance optimization
            stopwatch.Restart();
            var allJobGroups = await repository.GetJobGroupsAsync(skip: 0, take: 100, includeJobs: true);
            stopwatch.Stop();

            Console.WriteLine($"📊 Retrieved {allJobGroups.Count} job groups with all jobs in {stopwatch.ElapsedMilliseconds}ms");

            var totalJobs = allJobGroups.Sum(jg => jg.JobsCollection.Count);
            var totalTasks = allJobGroups.SelectMany(jg => jg.JobsCollection)
                                        .Sum(j => j.TasksCollection.Count);

            Console.WriteLine($"📈 Performance Summary:");
            Console.WriteLine($"   Total Job Groups: {allJobGroups.Count}");
            Console.WriteLine($"   Total Jobs: {totalJobs}");
            Console.WriteLine($"   Total Tasks: {totalTasks}");
            Console.WriteLine($"   Average Tasks per Job: {(double)totalTasks / totalJobs:F1}");

            // Demonstrate pagination performance
            Console.WriteLine("\n📋 Testing pagination performance");
            stopwatch.Restart();
            var pagedJobGroups = await repository.GetJobGroupsAsync(skip: 0, take: 3, includeJobs: false);
            stopwatch.Stop();

            Console.WriteLine($"✅ Retrieved {pagedJobGroups.Count} job groups (paginated) in {stopwatch.ElapsedMilliseconds}ms");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Demonstrates the database schema and table structure
    /// </summary>
    public static async Task ShowDatabaseSchema()
    {
        Console.WriteLine("\n🗃️  Database Schema Overview");
        Console.WriteLine("===========================");

        Console.WriteLine("📋 Hierarchical Job System Tables:");
        Console.WriteLine();
        
        Console.WriteLine("1. JobGroups Table:");
        Console.WriteLine("   ├── Id (Primary Key)");
        Console.WriteLine("   ├── Name (Required, Max 255 chars)");
        Console.WriteLine("   ├── Description (Max 1000 chars)");
        Console.WriteLine("   ├── CreatedAt, UpdatedAt");
        Console.WriteLine("   └── IsActive (Soft delete flag)");
        Console.WriteLine();

        Console.WriteLine("2. Jobs Table:");
        Console.WriteLine("   ├── Id (Primary Key)");
        Console.WriteLine("   ├── Name (Required, Max 255 chars)");
        Console.WriteLine("   ├── Description (Max 1000 chars)");
        Console.WriteLine("   ├── JobType (DataPipeline, ApiIntegration, etc.)");
        Console.WriteLine("   ├── ExecutionOrder (Order within job group)");
        Console.WriteLine("   ├── JobGroupId (Foreign Key → JobGroups.Id)");
        Console.WriteLine("   ├── CreatedAt, UpdatedAt");
        Console.WriteLine("   └── IsActive");
        Console.WriteLine();

        Console.WriteLine("3. JobTasks Table:");
        Console.WriteLine("   ├── Id (Primary Key)");
        Console.WriteLine("   ├── Name (Required, Max 255 chars)");
        Console.WriteLine("   ├── Description (Max 1000 chars)");
        Console.WriteLine("   ├── TaskType (FetchApiData, CreateFile, UploadSftp, etc.)");
        Console.WriteLine("   ├── ExecutionOrder (Order within job)");
        Console.WriteLine("   ├── ConfigurationData (JSON, Max 4000 chars) ⭐");
        Console.WriteLine("   ├── MaxRetries, TimeoutSeconds");
        Console.WriteLine("   ├── JobId (Foreign Key → Jobs.Id)");
        Console.WriteLine("   ├── CreatedAt, UpdatedAt");
        Console.WriteLine("   └── IsActive");
        Console.WriteLine();

        Console.WriteLine("4. JobExecutions Table:");
        Console.WriteLine("   ├── Id (Primary Key)");
        Console.WriteLine("   ├── JobId (Foreign Key → Jobs.Id)");
        Console.WriteLine("   ├── Status (Pending, Running, Completed, Failed, Cancelled)");
        Console.WriteLine("   ├── StartedAt, CompletedAt, DurationMs");
        Console.WriteLine("   ├── CurrentTaskIndex, TotalTasks");
        Console.WriteLine("   ├── ProgressPercentage (0-100)");
        Console.WriteLine("   ├── ErrorMessage (Max 2000 chars)");
        Console.WriteLine("   └── ExecutionData (JSON, Max 4000 chars)");
        Console.WriteLine();

        Console.WriteLine("🔗 Relationships:");
        Console.WriteLine("   JobGroups → Jobs (One-to-Many, Cascade Delete)");
        Console.WriteLine("   Jobs → JobTasks (One-to-Many, Cascade Delete)");
        Console.WriteLine("   Jobs → JobExecutions (One-to-Many, Cascade Delete)");
        Console.WriteLine();

        Console.WriteLine("⭐ Key Features:");
        Console.WriteLine("   ✅ ConfigurationData stores task parameters as JSON");
        Console.WriteLine("   ✅ Complete execution history and audit trail");
        Console.WriteLine("   ✅ Soft delete support (IsActive flags)");
        Console.WriteLine("   ✅ Performance optimized with indexes and pagination");
        Console.WriteLine("   ✅ Entity Framework migrations support");

        await Task.CompletedTask;
    }
}