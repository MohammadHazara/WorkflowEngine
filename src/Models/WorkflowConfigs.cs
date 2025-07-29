using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace WorkflowEngine.Models;

/// <summary>
/// Configuration for API data fetching step
/// </summary>
public sealed class ApiDataFetchConfig
{
    /// <summary>
    /// Gets or sets the API endpoint URL
    /// </summary>
    [Required]
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP headers for the request
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the authentication token if required
    /// </summary>
    public string? AuthToken { get; set; }
}

/// <summary>
/// Configuration for file creation step
/// </summary>
public sealed class FileCreationConfig
{
    /// <summary>
    /// Gets or sets the output file path
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON formatting options
    /// </summary>
    public JsonSerializerOptions JsonOptions { get; set; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets or sets whether to overwrite existing files
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;
}

/// <summary>
/// Configuration for ZIP compression step
/// </summary>
public sealed class ZipCompressionConfig
{
    /// <summary>
    /// Gets or sets the source file path to compress
    /// </summary>
    [Required]
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output ZIP file path
    /// </summary>
    [Required]
    public string ZipFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the compression level
    /// </summary>
    public System.IO.Compression.CompressionLevel CompressionLevel { get; set; } = 
        System.IO.Compression.CompressionLevel.Optimal;

    /// <summary>
    /// Gets or sets whether to delete the source file after compression
    /// </summary>
    public bool DeleteSourceAfterCompression { get; set; } = false;
}

/// <summary>
/// Configuration for API upload step
/// </summary>
public sealed class ApiUploadConfig
{
    /// <summary>
    /// Gets or sets the upload endpoint URL
    /// </summary>
    [Required]
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path to upload
    /// </summary>
    [Required]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the form field name for the file
    /// </summary>
    public string FileFieldName { get; set; } = "file";

    /// <summary>
    /// Gets or sets additional form data
    /// </summary>
    public Dictionary<string, string> FormData { get; set; } = new();

    /// <summary>
    /// Gets or sets the HTTP headers for the upload request
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300; // 5 minutes for uploads

    /// <summary>
    /// Gets or sets the authentication token if required
    /// </summary>
    public string? AuthToken { get; set; }
}

/// <summary>
/// Configuration for SFTP upload step
/// </summary>
public sealed class SftpUploadConfig
{
    /// <summary>
    /// Gets or sets the SFTP server hostname or IP address
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SFTP server port (default: 22)
    /// </summary>
    public int Port { get; set; } = 22;

    /// <summary>
    /// Gets or sets the username for SFTP authentication
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for SFTP authentication
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the private key file path for key-based authentication
    /// </summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>
    /// Gets or sets the private key passphrase if the key is encrypted
    /// </summary>
    public string? PrivateKeyPassphrase { get; set; }

    /// <summary>
    /// Gets or sets the local file path to upload
    /// </summary>
    [Required]
    public string LocalFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remote directory path on the SFTP server
    /// </summary>
    [Required]
    public string RemoteDirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remote file name (if different from local file name)
    /// </summary>
    public string? RemoteFileName { get; set; }

    /// <summary>
    /// Gets or sets whether to overwrite existing files on the server
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to create remote directories if they don't exist
    /// </summary>
    public bool CreateRemoteDirectories { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to verify the SSH host key
    /// </summary>
    public bool VerifyHostKey { get; set; } = false;

    /// <summary>
    /// Gets or sets the expected SSH host key fingerprint for verification
    /// </summary>
    public string? HostKeyFingerprint { get; set; }
}