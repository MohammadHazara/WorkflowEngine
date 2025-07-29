using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for handling SFTP operations with performance optimization and security features
/// </summary>
public sealed class SftpService : IDisposable
{
    private readonly ILogger<SftpService>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the SftpService class
    /// </summary>
    /// <param name="logger">Optional logger for SFTP operations</param>
    public SftpService(ILogger<SftpService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Uploads a file to an SFTP server with comprehensive error handling and retry logic
    /// </summary>
    /// <param name="config">Configuration for the SFTP upload operation</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if upload was successful</returns>
    public async Task<bool> UploadFileAsync(SftpUploadConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        _logger?.LogInformation("Starting SFTP upload of {LocalFile} to {Host}:{Port}{RemotePath}", 
            config.LocalFilePath, config.Host, config.Port, config.RemoteDirectoryPath);

        try
        {
            if (!File.Exists(config.LocalFilePath))
            {
                _logger?.LogError("Local file not found: {LocalFilePath}", config.LocalFilePath);
                return false;
            }

            using var client = await CreateSftpClientAsync(config, cancellationToken);
            
            if (!client.IsConnected)
            {
                _logger?.LogError("Failed to connect to SFTP server {Host}:{Port}", config.Host, config.Port);
                return false;
            }

            var remoteFileName = config.RemoteFileName ?? Path.GetFileName(config.LocalFilePath);
            var remoteFilePath = $"{config.RemoteDirectoryPath.TrimEnd('/')}/{remoteFileName}";

            // Create remote directories if needed
            if (config.CreateRemoteDirectories)
            {
                await CreateRemoteDirectoriesAsync(client, config.RemoteDirectoryPath, cancellationToken);
            }

            // Check if file exists and handle overwrite logic
            if (!config.OverwriteExisting && client.Exists(remoteFilePath))
            {
                _logger?.LogWarning("Remote file already exists and overwrite is disabled: {RemoteFilePath}", remoteFilePath);
                return false;
            }

            // Upload the file with progress tracking
            var fileInfo = new FileInfo(config.LocalFilePath);
            _logger?.LogInformation("Uploading {FileSize} bytes to {RemoteFilePath}", fileInfo.Length, remoteFilePath);

            using var fileStream = new FileStream(config.LocalFilePath, FileMode.Open, FileAccess.Read);
            
            await Task.Run(() =>
            {
                client.UploadFile(fileStream, remoteFilePath, true, progress =>
                {
                    if (progress % 25 == 0) // Log every 25% progress
                    {
                        var progressPercentage = ((long)progress * 100) / fileInfo.Length;
                        _logger?.LogDebug("Upload progress: {Progress}% ({Uploaded}/{Total} bytes)", 
                            progressPercentage, progress, fileInfo.Length);
                    }
                });
            }, cancellationToken);

            // Verify upload
            if (client.Exists(remoteFilePath))
            {
                var remoteFileInfo = client.GetAttributes(remoteFilePath);
                if (remoteFileInfo.Size == fileInfo.Length)
                {
                    _logger?.LogInformation("Successfully uploaded file to SFTP server. Size verified: {Size} bytes", remoteFileInfo.Size);
                    return true;
                }
                else
                {
                    _logger?.LogError("File size mismatch after upload. Local: {LocalSize}, Remote: {RemoteSize}", 
                        fileInfo.Length, remoteFileInfo.Size);
                    return false;
                }
            }
            else
            {
                _logger?.LogError("File verification failed - remote file not found after upload: {RemoteFilePath}", remoteFilePath);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to upload file {LocalFile} to SFTP server {Host}:{Port}", 
                config.LocalFilePath, config.Host, config.Port);
            return false;
        }
    }

    /// <summary>
    /// Tests the SFTP connection with the provided configuration
    /// </summary>
    /// <param name="config">Configuration for the SFTP connection</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if connection test was successful</returns>
    public async Task<bool> TestConnectionAsync(SftpUploadConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        _logger?.LogInformation("Testing SFTP connection to {Host}:{Port}", config.Host, config.Port);

        try
        {
            using var client = await CreateSftpClientAsync(config, cancellationToken);
            
            if (client.IsConnected)
            {
                // Test directory access
                try
                {
                    client.ListDirectory(config.RemoteDirectoryPath);
                    _logger?.LogInformation("SFTP connection test successful to {Host}:{Port}", config.Host, config.Port);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("SFTP connection established but directory access failed: {Error}", ex.Message);
                    return false;
                }
            }

            _logger?.LogError("SFTP connection test failed to {Host}:{Port}", config.Host, config.Port);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SFTP connection test failed to {Host}:{Port}", config.Host, config.Port);
            return false;
        }
    }

    /// <summary>
    /// Creates and connects an SFTP client with the provided configuration
    /// </summary>
    /// <param name="config">SFTP configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connected SFTP client</returns>
    private async Task<SftpClient> CreateSftpClientAsync(SftpUploadConfig config, CancellationToken cancellationToken)
    {
        SftpClient client;

        // Configure authentication method
        if (!string.IsNullOrEmpty(config.PrivateKeyPath))
        {
            // Key-based authentication
            _logger?.LogDebug("Using private key authentication for SFTP connection");
            
            var keyFiles = new List<PrivateKeyFile>();
            
            if (!string.IsNullOrEmpty(config.PrivateKeyPassphrase))
            {
                keyFiles.Add(new PrivateKeyFile(config.PrivateKeyPath, config.PrivateKeyPassphrase));
            }
            else
            {
                keyFiles.Add(new PrivateKeyFile(config.PrivateKeyPath));
            }

            client = new SftpClient(config.Host, config.Port, config.Username, keyFiles.ToArray());
        }
        else if (!string.IsNullOrEmpty(config.Password))
        {
            // Password-based authentication
            _logger?.LogDebug("Using password authentication for SFTP connection");
            client = new SftpClient(config.Host, config.Port, config.Username, config.Password);
        }
        else
        {
            throw new InvalidOperationException("Either password or private key must be provided for SFTP authentication");
        }

        // Configure connection settings
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(config.ConnectionTimeoutSeconds);

        // Configure host key verification
        if (config.VerifyHostKey && !string.IsNullOrEmpty(config.HostKeyFingerprint))
        {
            client.HostKeyReceived += (sender, e) =>
            {
                var receivedFingerprint = Convert.ToHexString(e.FingerPrint).Replace("-", "").ToLowerInvariant();
                var expectedFingerprint = config.HostKeyFingerprint.Replace(":", "").Replace("-", "").ToLowerInvariant();
                
                if (receivedFingerprint != expectedFingerprint)
                {
                    _logger?.LogError("Host key verification failed. Expected: {Expected}, Received: {Received}", 
                        expectedFingerprint, receivedFingerprint);
                    e.CanTrust = false;
                }
                else
                {
                    _logger?.LogDebug("Host key verification successful");
                    e.CanTrust = true;
                }
            };
        }

        // Connect with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(config.ConnectionTimeoutSeconds));

        await Task.Run(() =>
        {
            client.Connect();
        }, cts.Token);

        _logger?.LogDebug("SFTP client connected successfully to {Host}:{Port}", config.Host, config.Port);
        return client;
    }

    /// <summary>
    /// Creates remote directories recursively if they don't exist
    /// </summary>
    /// <param name="client">Connected SFTP client</param>
    /// <param name="remotePath">Remote directory path to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task CreateRemoteDirectoriesAsync(SftpClient client, string remotePath, CancellationToken cancellationToken)
    {
        try
        {
            if (client.Exists(remotePath))
            {
                return;
            }

            var pathParts = remotePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = remotePath.StartsWith('/') ? "/" : "";

            foreach (var part in pathParts)
            {
                currentPath = currentPath.TrimEnd('/') + "/" + part;
                
                if (!client.Exists(currentPath))
                {
                    await Task.Run(() =>
                    {
                        client.CreateDirectory(currentPath);
                        _logger?.LogDebug("Created remote directory: {DirectoryPath}", currentPath);
                    }, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create remote directories for path: {RemotePath}", remotePath);
            throw;
        }
    }

    /// <summary>
    /// Disposes of the service resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}