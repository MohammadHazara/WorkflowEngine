using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Diagnostics;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Tests.Services;

/// <summary>
/// Test class for WorkflowExecutor service with comprehensive testing and performance validation
/// </summary>
[TestClass]
public sealed class WorkflowExecutorTests
{
    private IStepHandler? _mockStepHandler;
    private ILogger<WorkflowExecutor>? _mockLogger;
    private WorkflowExecutor? _workflowExecutor;

    /// <summary>
    /// Sets up test dependencies before each test
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _mockStepHandler = Substitute.For<IStepHandler>();
        _mockLogger = Substitute.For<ILogger<WorkflowExecutor>>();
        _workflowExecutor = new WorkflowExecutor(_mockStepHandler, _mockLogger);
    }

    /// <summary>
    /// Cleans up test dependencies after each test
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _mockStepHandler = null;
        _mockLogger = null;
        _workflowExecutor = null;
    }

    /// <summary>
    /// Tests successful workflow execution
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_WithValidWorkflow_ShouldReturnTrue()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _workflowExecutor!.ExecuteWorkflowAsync(workflow);

        // Assert
        Assert.IsTrue(result);
        await _mockStepHandler!.Received(2).ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests workflow execution with null workflow throws ArgumentNullException
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_WithNullWorkflow_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _workflowExecutor!.ExecuteWorkflowAsync(null!));
    }

    /// <summary>
    /// Tests workflow execution with invalid workflow returns false
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_WithInvalidWorkflow_ShouldReturnFalse()
    {
        // Arrange
        var invalidWorkflow = new Workflow(); // No name, no steps

        // Act
        var result = await _workflowExecutor!.ExecuteWorkflowAsync(invalidWorkflow);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests workflow execution stops on first step failure
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_WithFailingStep_ShouldStopOnFailure()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false)); // First step fails

        // Act
        var result = await _workflowExecutor!.ExecuteWorkflowAsync(workflow);

        // Assert
        Assert.IsFalse(result);
        await _mockStepHandler!.Received(1).ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests workflow execution with cancellation token
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_WithCancellation_ShouldHandleCancellation()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        using var cts = new CancellationTokenSource();
        
        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(async (info) =>
            {
                var token = info.ArgAt<CancellationToken>(1);
                await Task.Delay(1000, token);
                return true;
            });

        // Act
        cts.Cancel();
        var result = await _workflowExecutor!.ExecuteWorkflowAsync(workflow, cts.Token);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests CanExecuteWorkflow validation with valid workflow
    /// </summary>
    [TestMethod]
    public void CanExecuteWorkflow_WithValidWorkflow_ShouldReturnTrue()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);

        // Act
        var result = _workflowExecutor!.CanExecuteWorkflow(workflow);

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests CanExecuteWorkflow validation with null workflow
    /// </summary>
    [TestMethod]
    public void CanExecuteWorkflow_WithNullWorkflow_ShouldReturnFalse()
    {
        // Act
        var result = _workflowExecutor!.CanExecuteWorkflow(null!);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests CanExecuteWorkflow validation with workflow containing invalid steps
    /// </summary>
    [TestMethod]
    public void CanExecuteWorkflow_WithInvalidSteps_ShouldReturnFalse()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(false);

        // Act
        var result = _workflowExecutor!.CanExecuteWorkflow(workflow);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests GetEstimatedExecutionTimeAsync with valid workflow
    /// </summary>
    [TestMethod]
    public async Task GetEstimatedExecutionTimeAsync_WithValidWorkflow_ShouldReturnEstimatedTime()
    {
        // Arrange
        var workflow = CreateValidWorkflow(); // 2 steps
        const long expectedBaseTime = 2 * 100; // 2 steps * 100ms
        const long expectedOverheadTime = 2 * 10; // 2 steps * 10ms overhead

        // Act
        var estimatedTime = await _workflowExecutor!.GetEstimatedExecutionTimeAsync(workflow);

        // Assert
        Assert.AreEqual(expectedBaseTime + expectedOverheadTime, estimatedTime);
    }

    /// <summary>
    /// Tests GetEstimatedExecutionTimeAsync with empty workflow
    /// </summary>
    [TestMethod]
    public async Task GetEstimatedExecutionTimeAsync_WithEmptyWorkflow_ShouldReturnZero()
    {
        // Arrange
        var emptyWorkflow = new Workflow("Empty Workflow");

        // Act
        var estimatedTime = await _workflowExecutor!.GetEstimatedExecutionTimeAsync(emptyWorkflow);

        // Assert
        Assert.AreEqual(0, estimatedTime);
    }

    /// <summary>
    /// Tests ExecuteWorkflowWithProgressAsync with progress reporting
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowWithProgressAsync_ShouldReportProgress()
    {
        // Arrange
        var workflow = CreateValidWorkflow(); // 2 steps
        var progressValues = new List<int>();
        var progress = new Progress<int>(value => progressValues.Add(value));

        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _workflowExecutor!.ExecuteWorkflowWithProgressAsync(workflow, progress);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(progressValues.Count >= 2);
        Assert.AreEqual(50, progressValues[0]); // After first step (1/2 = 50%)
        Assert.AreEqual(100, progressValues[1]); // After second step (2/2 = 100%)
    }

    /// <summary>
    /// Tests ExecuteWorkflowWithProgressAsync with null progress throws ArgumentNullException
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowWithProgressAsync_WithNullProgress_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workflow = CreateValidWorkflow();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _workflowExecutor!.ExecuteWorkflowWithProgressAsync(workflow, null!));
    }

    /// <summary>
    /// Tests workflow execution performance with large number of steps
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_BulkExecution_ShouldPerformEfficiently()
    {
        // Arrange
        const int stepCount = 1000;
        var workflow = CreateLargeWorkflow(stepCount);
        
        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _workflowExecutor!.ExecuteWorkflowAsync(workflow);

        stopwatch.Stop();

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000,
            $"Bulk execution took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        
        await _mockStepHandler!.Received(stepCount)
            .ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests concurrent workflow execution for thread safety
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        const int concurrentWorkflows = 50;
        var workflows = Enumerable.Range(0, concurrentWorkflows)
            .Select(i => CreateValidWorkflow($"Concurrent Workflow {i}"))
            .ToList();

        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(async (info) =>
            {
                await Task.Delay(10);
                return true;
            });

        // Act
        var tasks = workflows.Select(w => _workflowExecutor!.ExecuteWorkflowAsync(w));
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(concurrentWorkflows, results.Length);
        Assert.IsTrue(results.All(r => r));
    }

    /// <summary>
    /// Tests workflow executor constructor with null step handler throws ArgumentNullException
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullStepHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => new WorkflowExecutor(null!, _mockLogger));
    }

    /// <summary>
    /// Tests workflow executor with mocked dependencies using NSubstitute
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_WithMockedDependencies_ShouldExecuteCorrectly()
    {
        // Arrange
        var mockWorkflow = Substitute.For<Workflow>();
        var mockSteps = new List<Step> { new Step(1, "Mock Step") };
        
        mockWorkflow.ValidateWorkflow().Returns(true);
        mockWorkflow.HasSteps().Returns(true);
        mockWorkflow.Steps.Returns(mockSteps);
        mockWorkflow.GetStepCount().Returns(1);

        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var canExecute = _workflowExecutor!.CanExecuteWorkflow(mockWorkflow);
        var result = await _workflowExecutor!.ExecuteWorkflowAsync(mockWorkflow);

        // Assert
        Assert.IsTrue(canExecute);
        Assert.IsTrue(result);
        
        mockWorkflow.Received(1).ValidateWorkflow();
        mockWorkflow.Received().HasSteps();
        await _mockStepHandler!.Received(1).ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests workflow executor memory usage during large-scale operations
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_MemoryUsage_ShouldBeOptimized()
    {
        // Arrange
        const int workflowCount = 100;
        var initialMemory = GC.GetTotalMemory(true);

        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var tasks = new List<Task<bool>>();
        for (var i = 0; i < workflowCount; i++)
        {
            var workflow = CreateValidWorkflow($"Memory Test Workflow {i}");
            tasks.Add(_workflowExecutor!.ExecuteWorkflowAsync(workflow));
        }

        await Task.WhenAll(tasks);
        
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        Assert.IsTrue(tasks.All(t => t.Result));
        Assert.IsTrue(memoryIncrease < 100_000_000, // Less than 100MB increase
            $"Memory increase was {memoryIncrease} bytes, expected < 100MB");
    }

    /// <summary>
    /// Tests workflow execution exception handling
    /// </summary>
    [TestMethod]
    public async Task ExecuteWorkflowAsync_WithStepHandlerException_ShouldReturnFalse()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        _mockStepHandler!.CanExecuteStep(Arg.Any<Step>()).Returns(true);
        _mockStepHandler!.ExecuteStepAsync(Arg.Any<Step>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Test exception")));

        // Act
        var result = await _workflowExecutor!.ExecuteWorkflowAsync(workflow);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Creates a valid workflow for testing
    /// </summary>
    /// <param name="name">Optional workflow name</param>
    /// <returns>A valid workflow with steps</returns>
    private static Workflow CreateValidWorkflow(string name = "Test Workflow")
    {
        var workflow = new Workflow(name, "Test workflow description");
        
        workflow.AddStep(new Step(1, "Step 1"));
        workflow.AddStep(new Step(2, "Step 2"));
        
        return workflow;
    }

    /// <summary>
    /// Creates a large workflow for performance testing
    /// </summary>
    /// <param name="stepCount">Number of steps to create</param>
    /// <returns>A workflow with many steps</returns>
    private static Workflow CreateLargeWorkflow(int stepCount)
    {
        var workflow = new Workflow("Large Test Workflow", "Performance testing workflow");
        
        for (var i = 1; i <= stepCount; i++)
        {
            workflow.AddStep(new Step(i, $"Performance Step {i}"));
        }
        
        return workflow;
    }
}