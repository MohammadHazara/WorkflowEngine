using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

namespace WorkflowEngine.Tests;

/// <summary>
/// Tests for SftpService functionality
/// </summary>
[TestClass]
public sealed class SftpServiceTests
{
    private SftpService _sftpService;
    private ILogger<SftpService> _mockLogger;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = Substitute.For<ILogger<SftpService>>();
        _sftpService = new SftpService(_mockLogger);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _sftpService?.Dispose();
    }

    [TestMethod]
    public async Task UploadFileAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var config = new SftpUploadConfig
        {
            Host = "localhost",
            Username = "testuser",
            Password = "testpass",
            LocalFilePath = "non-existent-file.zip",
            RemoteDirectoryPath = "/upload"
        };

        // Act
        var result = await _sftpService.UploadFileAsync(config);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task UploadFileAsync_WithInvalidConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _sftpService.UploadFileAsync(null!));
    }

    [TestMethod]
    public async Task TestConnectionAsync_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _sftpService.TestConnectionAsync(null!));
    }

    [TestMethod]
    public async Task TestConnectionAsync_WithInvalidCredentials_ShouldReturnFalse()
    {
        // Arrange
        var config = new SftpUploadConfig
        {
            Host = "nonexistent.example.com",
            Username = "invaliduser",
            Password = "invalidpass",
            RemoteDirectoryPath = "/upload",
            ConnectionTimeoutSeconds = 5
        };

        // Act
        var result = await _sftpService.TestConnectionAsync(config);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void SftpUploadConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new SftpUploadConfig();

        // Assert
        Assert.AreEqual(22, config.Port);
        Assert.AreEqual(30, config.ConnectionTimeoutSeconds);
        Assert.IsTrue(config.OverwriteExisting);
        Assert.IsTrue(config.CreateRemoteDirectories);
        Assert.IsFalse(config.VerifyHostKey);
    }

    [TestMethod]
    public void SftpUploadConfig_WithKeyAuthentication_ShouldSetProperties()
    {
        // Arrange & Act
        var config = new SftpUploadConfig
        {
            Host = "sftp.example.com",
            Username = "user",
            PrivateKeyPath = "/path/to/key",
            PrivateKeyPassphrase = "passphrase",
            LocalFilePath = "/local/file.zip",
            RemoteDirectoryPath = "/remote/dir",
            VerifyHostKey = true,
            HostKeyFingerprint = "aa:bb:cc:dd:ee:ff"
        };

        // Assert
        Assert.AreEqual("sftp.example.com", config.Host);
        Assert.AreEqual("user", config.Username);
        Assert.AreEqual("/path/to/key", config.PrivateKeyPath);
        Assert.AreEqual("passphrase", config.PrivateKeyPassphrase);
        Assert.IsTrue(config.VerifyHostKey);
        Assert.AreEqual("aa:bb:cc:dd:ee:ff", config.HostKeyFingerprint);
    }
}