namespace WorkflowEngine.Interfaces;

/// <summary>
/// Interface for user authentication services
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token if authentication successful, null otherwise</returns>
    Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token and returns the user ID
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>User ID if token is valid, null otherwise</returns>
    Task<int?> ValidateTokenAsync(string token);

    /// <summary>
    /// Refreshes an existing JWT token
    /// </summary>
    /// <param name="token">The current token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New JWT token if refresh successful, null otherwise</returns>
    Task<string?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default);
}