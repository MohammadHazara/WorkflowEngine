using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Diagnostics;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Tests.Services;

/// <summary>
/// Test class for StepHandler service with comprehensive testing and performance validation
/// </summary>
[TestClass]
public sealed class StepHandlerTests
{
    private ILogger<StepHandler>? _mockLogger;
    private StepHandler? _stepHandler;

    /// <summary>
    /// Sets up test dependencies before each test
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _mockLogger = Substitute.For<ILogger<StepHandler>>();
        _stepHandler = new StepHandler(_mockLogger);
    }

    /// <summary>
    /// Cleans up test dependencies after each test
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _mockLogger = null;
        _stepHandler = null;
    }

    /// <summary>
    /// Tests successful step execution
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithValidStep_ShouldReturnTrue()
    {
        // Arrange
        var step = new Step(1, "Test Step")
        {
            ExecuteFunction = () => Task.FromResult(true)
        };

        // Act
        var result = await _stepHandler!.ExecuteStepAsync(step);

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests step execution with null step throws ArgumentNullException
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithNullStep_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _stepHandler!.ExecuteStepAsync(null!));
    }

    /// <summary>
    /// Tests step execution with invalid step returns false
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithInvalidStep_ShouldReturnFalse()
    {
        // Arrange
        var invalidStep = new Step(0, null); // Invalid: ID is 0 and name is null

        // Act
        var result = await _stepHandler!.ExecuteStepAsync(invalidStep);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests step execution with failing step function
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithFailingStep_ShouldReturnFalse()
    {
        // Arrange
        var step = new Step(1, "Failing Step")
        {
            ExecuteFunction = () => Task.FromResult(false)
        };

        // Act
        var result = await _stepHandler!.ExecuteStepAsync(step);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests step execution with exception thrown during execution
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        var step = new Step(1, "Exception Step")
        {
            ExecuteFunction = () => throw new InvalidOperationException("Test exception")
        };

        // Act
        var result = await _stepHandler!.ExecuteStepAsync(step);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests step execution with cancellation token
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var step = new Step(1, "Cancellable Step")
        {
            ExecuteFunction = async () =>
            {
                await Task.Delay(1000, cts.Token);
                return true;
            }
        };

        // Act
        cts.Cancel();
        var result = await _stepHandler!.ExecuteStepAsync(step, cts.Token);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests CanExecuteStep validation with valid step
    /// </summary>
    [TestMethod]
    public void CanExecuteStep_WithValidStep_ShouldReturnTrue()
    {
        // Arrange
        var validStep = new Step(1, "Valid Step");

        // Act
        var result = _stepHandler!.CanExecuteStep(validStep);

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests CanExecuteStep validation with null step
    /// </summary>
    [TestMethod]
    public void CanExecuteStep_WithNullStep_ShouldReturnFalse()
    {
        // Act
        var result = _stepHandler!.CanExecuteStep(null!);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests CanExecuteStep validation with invalid step ID
    /// </summary>
    [TestMethod]
    public void CanExecuteStep_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var invalidStep = new Step(0, "Step with invalid ID");

        // Act
        var result = _stepHandler!.CanExecuteStep(invalidStep);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests CanExecuteStep validation with null or empty name
    /// </summary>
    [TestMethod]
    public void CanExecuteStep_WithEmptyName_ShouldReturnFalse()
    {
        // Arrange
        var stepWithEmptyName = new Step(1, "");
        var stepWithNullName = new Step(2, null);

        // Act
        var resultEmpty = _stepHandler!.CanExecuteStep(stepWithEmptyName);
        var resultNull = _stepHandler!.CanExecuteStep(stepWithNullName);

        // Assert
        Assert.IsFalse(resultEmpty);
        Assert.IsFalse(resultNull);
    }

    /// <summary>
    /// Tests GetExecutionPriority with valid step
    /// </summary>
    [TestMethod]
    public void GetExecutionPriority_WithValidStep_ShouldReturnStepId()
    {
        // Arrange
        const int expectedId = 42;
        var step = new Step(expectedId, "Test Step");

        // Act
        var priority = _stepHandler!.GetExecutionPriority(step);

        // Assert
        Assert.AreEqual(expectedId, priority);
    }

    /// <summary>
    /// Tests GetExecutionPriority with null step throws ArgumentNullException
    /// </summary>
    [TestMethod]
    public void GetExecutionPriority_WithNullStep_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => _stepHandler!.GetExecutionPriority(null!));
    }

    /// <summary>
    /// Tests GetExecutionPriority with invalid step ID returns default priority
    /// </summary>
    [TestMethod]
    public void GetExecutionPriority_WithInvalidId_ShouldReturnDefaultPriority()
    {
        // Arrange
        var stepWithInvalidId = new Step(0, "Invalid ID Step");

        // Act
        var priority = _stepHandler!.GetExecutionPriority(stepWithInvalidId);

        // Assert
        Assert.AreEqual(1, priority); // Default priority
    }

    /// <summary>
    /// Tests step execution with retry logic on transient failures
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithTransientFailures_ShouldRetryAndSucceed()
    {
        // Arrange
        var attemptCount = 0;
        var step = new Step(1, "Retry Step")
        {
            ExecuteFunction = () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new InvalidOperationException("Transient failure");
                }
                return Task.FromResult(true);
            }
        };

        // Act
        var result = await _stepHandler!.ExecuteStepAsync(step);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(3, attemptCount);
    }

    /// <summary>
    /// Tests step execution performance with large number of steps
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_BulkExecution_ShouldPerformEfficiently()
    {
        // Arrange
        const int stepCount = 1000;
        var steps = new List<Step>();
        
        for (var i = 0; i < stepCount; i++)
        {
            steps.Add(new Step(i + 1, $"Performance Step {i + 1}")
            {
                ExecuteFunction = () => Task.FromResult(true)
            });
        }

        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = steps.Select(step => _stepHandler!.ExecuteStepAsync(step));
        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        Assert.AreEqual(stepCount, results.Length);
        Assert.IsTrue(results.All(r => r));
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000,
            $"Bulk execution took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    /// <summary>
    /// Tests concurrent step execution for thread safety
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        const int concurrentExecutions = 100;
        var step = new Step(1, "Concurrent Step")
        {
            ExecuteFunction = async () =>
            {
                await Task.Delay(10);
                return true;
            }
        };

        // Act
        var tasks = Enumerable.Range(0, concurrentExecutions)
            .Select(_ => _stepHandler!.ExecuteStepAsync(step));
        
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(concurrentExecutions, results.Length);
        Assert.IsTrue(results.All(r => r));
    }

    /// <summary>
    /// Tests step handler with mocked step using NSubstitute
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithMockedStep_ShouldExecuteCorrectly()
    {
        // Arrange
        var mockStep = Substitute.For<Step>();
        mockStep.Id.Returns(1);
        mockStep.Name.Returns("Mocked Step");
        mockStep.ExecuteAsync().Returns(Task.FromResult(true));

        // Configure CanExecuteStep to return true for our mock
        var stepHandler = new StepHandler();

        // Act
        var canExecute = stepHandler.CanExecuteStep(mockStep);
        var result = canExecute ? await mockStep.ExecuteAsync() : false;

        // Assert
        Assert.IsTrue(canExecute);
        Assert.IsTrue(result);
        await mockStep.Received(1).ExecuteAsync();
    }

    /// <summary>
    /// Tests step handler logging functionality
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithLogging_ShouldLogCorrectly()
    {
        // Arrange
        var step = new Step(1, "Logged Step")
        {
            ExecuteFunction = () => Task.FromResult(true)
        };

        // Act
        var result = await _stepHandler!.ExecuteStepAsync(step);

        // Assert
        Assert.IsTrue(result);
        
        // Verify logging calls were made (NSubstitute with ILogger verification)
        _mockLogger!.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Starting execution")), 
            Arg.Any<object[]>());
        
        _mockLogger!.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Completed execution")), 
            Arg.Any<object[]>());
    }

    /// <summary>
    /// Tests step handler memory usage during large-scale operations
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_MemoryUsage_ShouldBeOptimized()
    {
        // Arrange
        const int stepCount = 10000;
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var tasks = new List<Task<bool>>();
        for (var i = 0; i < stepCount; i++)
        {
            var step = new Step(i + 1, $"Memory Test Step {i + 1}")
            {
                ExecuteFunction = () => Task.FromResult(true)
            };
            tasks.Add(_stepHandler!.ExecuteStepAsync(step));
        }

        await Task.WhenAll(tasks);
        
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        Assert.IsTrue(tasks.All(t => t.Result));
        Assert.IsTrue(memoryIncrease < 50_000_000, // Less than 50MB increase
            $"Memory increase was {memoryIncrease} bytes, expected < 50MB");
    }

    /// <summary>
    /// Tests step execution timeout handling
    /// </summary>
    [TestMethod]
    public async Task ExecuteStepAsync_WithTimeout_ShouldHandleTimeoutGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var step = new Step(1, "Timeout Step")
        {
            ExecuteFunction = async () =>
            {
                await Task.Delay(1000); // Longer than timeout
                return true;
            }
        };

        // Act
        var result = await _stepHandler!.ExecuteStepAsync(step, cts.Token);

        // Assert
        Assert.IsFalse(result);
    }
}