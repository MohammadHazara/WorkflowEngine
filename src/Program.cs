using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Data;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine;

/// <summary>
/// Main program entry point demonstrating the refactored WorkflowEngine
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main application entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    private static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            await RunWorkflowDemoAsync(host.Services);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetService<ILogger<WorkflowApplication>>();
            logger?.LogError(ex, "An error occurred during workflow execution");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    /// <summary>
    /// Creates the application host builder with dependency injection
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure SQLite database
                services.AddDbContext<WorkflowDbContext>(options =>
                    options.UseSqlite("Data Source=workflow.db"));
                
                // Register services
                services.AddScoped<IStepHandler, StepHandler>();
                services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();
                services.AddTransient<WorkflowApplication>();
                
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });

    /// <summary>
    /// Demonstrates the refactored workflow engine functionality
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection</param>
    private static async Task RunWorkflowDemoAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        
        var application = services.GetRequiredService<WorkflowApplication>();
        
        // Initialize database
        await application.InitializeDatabaseAsync();
        
        // Create and execute workflow
        await application.CreateAndExecuteWorkflowAsync();
        
        // Demonstrate performance testing
        await application.PerformanceTestAsync();
    }
}

/// <summary>
/// Application class to handle workflow operations
/// </summary>
public sealed class WorkflowApplication
{
    private readonly WorkflowDbContext _dbContext;
    private readonly IWorkflowExecutor _executor;
    private readonly ILogger<WorkflowApplication> _logger;

    /// <summary>
    /// Initializes a new instance of the WorkflowApplication class
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="executor">The workflow executor</param>
    /// <param name="logger">The logger</param>
    public WorkflowApplication(WorkflowDbContext dbContext, IWorkflowExecutor executor, ILogger<WorkflowApplication> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the database and ensures it's created
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Initializing database...");
        
        var created = await _dbContext.EnsureDatabaseCreatedAsync();
        if (created)
        {
            _logger.LogInformation("Database created successfully");
        }
        else
        {
            _logger.LogInformation("Database already exists");
        }
    }

    /// <summary>
    /// Creates and executes a sample workflow
    /// </summary>
    public async Task CreateAndExecuteWorkflowAsync()
    {
        _logger.LogInformation("Creating sample workflow...");
        
        // Create workflow with async steps
        var workflow = new Workflow("Sample Workflow", "Demonstrates the refactored workflow engine");
        
        // Create steps with async functions
        var step1 = new Step(1, "Initialize Process", 
            executeFunction: async () =>
            {
                _logger.LogInformation("Executing step 1: Initialize Process");
                await Task.Delay(100); // Simulate work
                return true;
            },
            onSuccess: async () =>
            {
                _logger.LogInformation("Step 1 completed successfully");
                return true;
            },
            onFailure: async () =>
            {
                _logger.LogError("Step 1 failed");
                return false;
            });

        var step2 = new Step(2, "Process Data",
            executeFunction: async () =>
            {
                _logger.LogInformation("Executing step 2: Process Data");
                await Task.Delay(200); // Simulate work
                return true;
            },
            onSuccess: async () =>
            {
                _logger.LogInformation("Step 2 completed successfully");
                return true;
            });

        var step3 = new Step(3, "Finalize Process",
            executeFunction: async () =>
            {
                _logger.LogInformation("Executing step 3: Finalize Process");
                await Task.Delay(150); // Simulate work
                return true;
            });

        // Add steps to workflow
        workflow.AddStep(step1);
        workflow.AddStep(step2);
        workflow.AddStep(step3);

        // Execute workflow with progress reporting
        var progress = new Progress<int>(percentage =>
            _logger.LogInformation("Workflow progress: {Percentage}%", percentage));

        _logger.LogInformation("Executing workflow...");
        
        var result = await _executor.ExecuteWorkflowWithProgressAsync(workflow, progress);
        
        _logger.LogInformation("Workflow execution completed. Result: {Result}", result);
    }

    /// <summary>
    /// Performs performance testing with large-scale workflow execution
    /// </summary>
    public async Task PerformanceTestAsync()
    {
        _logger.LogInformation("Starting performance test...");
        
        const int stepCount = 1000;
        var workflow = new Workflow("Performance Test Workflow", "Tests performance with many steps");
        
        // Create many steps for performance testing
        for (var i = 1; i <= stepCount; i++)
        {
            var stepId = i;
            var step = new Step(stepId, $"Performance Step {stepId}",
                executeFunction: async () =>
                {
                    // Simulate very fast work
                    await Task.Delay(1);
                    return true;
                });
                
            workflow.AddStep(step);
        }

        // Get estimated execution time
        var estimatedTime = await _executor.GetEstimatedExecutionTimeAsync(workflow);
        _logger.LogInformation("Estimated execution time: {EstimatedTime}ms for {StepCount} steps", 
            estimatedTime, stepCount);

        // Execute with timing
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var result = await _executor.ExecuteWorkflowAsync(workflow);
        
        stopwatch.Stop();
        
        _logger.LogInformation("Performance test completed. Result: {Result}, Actual time: {ActualTime}ms, Steps/sec: {StepsPerSecond:F2}", 
            result, stopwatch.ElapsedMilliseconds, stepCount / (stopwatch.ElapsedMilliseconds / 1000.0));
    }
}