using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Diagnostics;
using WorkflowEngine.Models;

namespace WorkflowEngine.Tests.Models;

/// <summary>
/// Test class for Step model validation and behavior testing with performance optimization
/// </summary>
[TestClass]
public sealed class StepTests
{
    /// <summary>
    /// Tests that Step can be instantiated with default values
    /// </summary>
    [TestMethod]
    public void Step_DefaultConstructor_ShouldInstantiateWithDefaultValues()
    {
        // Act
        var step = new Step();

        // Assert
        Assert.IsNotNull(step);
        Assert.AreEqual(0, step.Id);
        Assert.IsNull(step.Name);
        Assert.IsNull(step.OnSuccess);
        Assert.IsNull(step.OnFailure);
        Assert.IsNull(step.ExecuteFunction);
        Assert.IsTrue(step.CreatedAt <= DateTime.UtcNow);
        Assert.IsTrue(step.UpdatedAt <= DateTime.UtcNow);
    }

    /// <summary>
    /// Tests that Step properties can be set and retrieved correctly
    /// </summary>
    [TestMethod]
    public void Step_PropertySetters_ShouldSetAndGetPropertiesCorrectly()
    {
        // Arrange
        const int expectedId = 42;
        const string expectedName = "Test Step Name";
        Func<Task<bool>> expectedOnSuccess = () => Task.FromResult(true);
        Func<Task<bool>> expectedOnFailure = () => Task.FromResult(false);
        Func<Task<bool>> expectedExecuteFunction = () => Task.FromResult(true);

        // Act
        var step = new Step
        {
            Id = expectedId,
            Name = expectedName,
            OnSuccess = expectedOnSuccess,
            OnFailure = expectedOnFailure,
            ExecuteFunction = expectedExecuteFunction
        };

        // Assert
        Assert.AreEqual(expectedId, step.Id);
        Assert.AreEqual(expectedName, step.Name);
        Assert.AreEqual(expectedOnSuccess, step.OnSuccess);
        Assert.AreEqual(expectedOnFailure, step.OnFailure);
        Assert.AreEqual(expectedExecuteFunction, step.ExecuteFunction);
    }

    /// <summary>
    /// Tests OnSuccess delegate execution returns expected result
    /// </summary>
    [TestMethod]
    public async Task Step_OnSuccessExecution_ShouldReturnTrue()
    {
        // Arrange
        var step = new Step
        {
            OnSuccess = () => Task.FromResult(true)
        };

        // Act
        var result = await step.OnSuccess!.Invoke();

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests OnFailure delegate execution returns expected result
    /// </summary>
    [TestMethod]
    public async Task Step_OnFailureExecution_ShouldReturnFalse()
    {
        // Arrange
        var step = new Step
        {
            OnFailure = () => Task.FromResult(false)
        };

        // Act
        var result = await step.OnFailure!.Invoke();

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that Step handles null delegates gracefully
    /// </summary>
    [TestMethod]
    public void Step_WithNullDelegates_ShouldHandleGracefully()
    {
        // Arrange & Act
        var step = new Step
        {
            Id = 1,
            Name = "Test",
            OnSuccess = null,
            OnFailure = null,
            ExecuteFunction = null
        };

        // Assert
        Assert.IsNull(step.OnSuccess);
        Assert.IsNull(step.OnFailure);
        Assert.IsNull(step.ExecuteFunction);
    }

    /// <summary>
    /// Tests Step execution with successful outcome
    /// </summary>
    [TestMethod]
    public async Task Step_ExecuteAsync_WithSuccessDelegate_ShouldReturnTrue()
    {
        // Arrange
        var step = new Step
        {
            ExecuteFunction = () => Task.FromResult(true),
            OnSuccess = () => Task.FromResult(true)
        };

        // Act
        var result = await step.ExecuteAsync();

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests Step execution with failure outcome
    /// </summary>
    [TestMethod]
    public async Task Step_ExecuteAsync_WithFailureDelegate_ShouldReturnFalse()
    {
        // Arrange
        var step = new Step
        {
            ExecuteFunction = () => Task.FromResult(false),
            OnFailure = () => Task.FromResult(false)
        };

        // Act
        var result = await step.ExecuteAsync();

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests Step execution with null execute function
    /// </summary>
    [TestMethod]
    public async Task Step_ExecuteAsync_WithNullExecuteFunction_ShouldReturnTrue()
    {
        // Arrange
        var step = new Step
        {
            ExecuteFunction = null,
            OnSuccess = () => Task.FromResult(true)
        };

        // Act
        var result = await step.ExecuteAsync();

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests Step execution with exception handling
    /// </summary>
    [TestMethod]
    public async Task Step_ExecuteAsync_WithException_ShouldInvokeFailureCallback()
    {
        // Arrange
        var failureCalled = false;
        var step = new Step
        {
            ExecuteFunction = () => throw new InvalidOperationException("Test exception"),
            OnFailure = () =>
            {
                failureCalled = true;
                return Task.FromResult(false);
            }
        };

        // Act
        var result = await step.ExecuteAsync();

        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(failureCalled);
    }

    /// <summary>
    /// Tests Step with parameterized constructor
    /// </summary>
    [TestMethod]
    public void Step_ParameterizedConstructor_ShouldInitializeCorrectly()
    {
        // Arrange
        const int expectedId = 123;
        const string expectedName = "Test Step";
        Func<Task<bool>> executeFunction = () => Task.FromResult(true);
        Func<Task<bool>> onSuccess = () => Task.FromResult(true);
        Func<Task<bool>> onFailure = () => Task.FromResult(false);

        // Act
        var step = new Step(expectedId, expectedName, executeFunction, onSuccess, onFailure);

        // Assert
        Assert.AreEqual(expectedId, step.Id);
        Assert.AreEqual(expectedName, step.Name);
        Assert.AreEqual(executeFunction, step.ExecuteFunction);
        Assert.AreEqual(onSuccess, step.OnSuccess);
        Assert.AreEqual(onFailure, step.OnFailure);
    }

    /// <summary>
    /// Tests UpdateTimestamp method updates the timestamp
    /// </summary>
    [TestMethod]
    public void Step_UpdateTimestamp_ShouldUpdateTimestamp()
    {
        // Arrange
        var step = new Step();
        var originalTimestamp = step.UpdatedAt;

        // Act
        Thread.Sleep(10); // Small delay to ensure timestamp difference
        step.UpdateTimestamp();

        // Assert
        Assert.IsTrue(step.UpdatedAt > originalTimestamp);
    }

    /// <summary>
    /// Performance test for Step creation at scale
    /// </summary>
    [TestMethod]
    public void Step_BulkCreation_ShouldPerformEfficiently()
    {
        // Arrange
        const int stepCount = 100_000;
        var steps = new List<Step>(stepCount);
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (var i = 0; i < stepCount; i++)
        {
            steps.Add(new Step
            {
                Id = i,
                Name = $"Step_{i}",
                ExecuteFunction = () => Task.FromResult(true),
                OnSuccess = () => Task.FromResult(true),
                OnFailure = () => Task.FromResult(false)
            });
        }
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(stepCount, steps.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Creation took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    /// <summary>
    /// Performance test for Step execution at scale
    /// </summary>
    [TestMethod]
    public async Task Step_BulkExecution_ShouldPerformEfficiently()
    {
        // Arrange
        const int stepCount = 1000;
        var steps = new List<Step>();
        
        for (var i = 0; i < stepCount; i++)
        {
            steps.Add(new Step
            {
                Id = i,
                Name = $"Step_{i}",
                ExecuteFunction = () => Task.FromResult(true)
            });
        }

        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = steps.Select(step => step.ExecuteAsync());
        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Assert
        Assert.AreEqual(stepCount, results.Length);
        Assert.IsTrue(results.All(r => r));
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000,
            $"Execution took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    /// <summary>
    /// Tests Step execution with mocked dependencies using NSubstitute
    /// </summary>
    [TestMethod]
    public async Task Step_ExecuteAsync_WithMockedDependencies_ShouldWork()
    {
        // Arrange
        var mockExecuteFunction = Substitute.For<Func<Task<bool>>>();
        var mockOnSuccess = Substitute.For<Func<Task<bool>>>();
        
        mockExecuteFunction.Invoke().Returns(Task.FromResult(true));
        mockOnSuccess.Invoke().Returns(Task.FromResult(true));

        var step = new Step
        {
            ExecuteFunction = mockExecuteFunction,
            OnSuccess = mockOnSuccess
        };

        // Act
        var result = await step.ExecuteAsync();

        // Assert
        Assert.IsTrue(result);
        await mockExecuteFunction.Received(1).Invoke();
        await mockOnSuccess.Received(1).Invoke();
    }

    /// <summary>
    /// Tests concurrent Step execution for thread safety
    /// </summary>
    [TestMethod]
    public async Task Step_ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        const int concurrentExecutions = 100;
        var step = new Step
        {
            ExecuteFunction = async () =>
            {
                await Task.Delay(10);
                return true;
            }
        };

        // Act
        var tasks = Enumerable.Range(0, concurrentExecutions)
            .Select(_ => step.ExecuteAsync());
        
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(concurrentExecutions, results.Length);
        Assert.IsTrue(results.All(r => r));
    }
}