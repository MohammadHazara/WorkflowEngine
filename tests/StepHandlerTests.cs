using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using WorkflowEngine.Interfaces;
using WorkflowEngine.Models;
using WorkflowEngine.Services;
using System.Threading.Tasks;

namespace WorkflowEngine.Tests
{
    /// <summary>
    /// Tests for StepHandler functionality
    /// </summary>
    [TestClass]
    public sealed class StepHandlerTests
    {
        private IStepHandler _stepHandler = null!;
        private ILogger<StepHandler> _logger = null!;

        [TestInitialize]
        public void Setup()
        {
            _logger = Substitute.For<ILogger<StepHandler>>();
            _stepHandler = new StepHandler(_logger);
        }

        [TestMethod]
        public async Task ExecuteStepAsync_ValidStep_ReturnsSuccess()
        {
            // Arrange
            var step = Substitute.For<Step>();
            step.Id.Returns(1);
            step.Name.Returns("Test Step");
            step.StepType.Returns("TestType");
            step.Configuration.Returns("{}");

            // Act
            var result = await _stepHandler.ExecuteStepAsync(step);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ExecuteStepAsync_StepWithDelay_ReturnsSuccessAfterDelay()
        {
            // Arrange
            var step = Substitute.For<Step>();
            step.Id.Returns(2);
            step.Name.Returns("Delay Step");
            step.StepType.Returns("Delay");
            step.Configuration.Returns("{\"DelayMs\": 100}");

            var startTime = DateTime.UtcNow;

            // Act
            var result = await _stepHandler.ExecuteStepAsync(step);

            // Assert
            Assert.IsTrue(result);
            var elapsed = DateTime.UtcNow - startTime;
            Assert.IsTrue(elapsed.TotalMilliseconds >= 100, "Delay should have been at least 100ms");
        }

        [TestMethod]
        public async Task ExecuteStepAsync_StepWithException_ReturnsFalse()
        {
            // Arrange
            var step = Substitute.For<Step>();
            step.Id.Returns(3);
            step.Name.Returns("Error Step");
            step.StepType.Returns("Error");
            step.Configuration.Returns("{}");

            // Act
            var result = await _stepHandler.ExecuteStepAsync(step);

            // Assert
            Assert.IsFalse(result);
        }
    }
}