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

/// <summary>
/// Interface for user management services
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<Models.User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their username
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<Models.User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="email">The email address</param>
    /// <param name="password">The password</param>
    /// <param name="role">The user role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user</returns>
    Task<Models.User> CreateUserAsync(string username, string email, string password, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's last login timestamp
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a password against a user's stored hash
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <param name="hash">The stored password hash</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Hashes a password for secure storage
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);
}