using WorkflowEngine.Models;

namespace WorkflowEngine.Interfaces;

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their username
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="email">The email address</param>
    /// <param name="password">The password</param>
    /// <param name="role">The user role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user with assigned ID</returns>
    Task<User> CreateUserAsync(string username, string email, string password, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user with assigned ID</returns>
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated user</returns>
    Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last login time for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates user credentials
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if credentials are valid, false otherwise</returns>
    Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <param name="passwordHash">The password hash</param>
    /// <returns>True if password matches hash, false otherwise</returns>
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Hashes a password
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Changes a user's password
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="currentPassword">The current password</param>
    /// <param name="newPassword">The new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if password was changed successfully, false otherwise</returns>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    /// <param name="userId">The user ID to deactivate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deactivated successfully, false if user not found</returns>
    Task<bool> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users with pagination
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active users</returns>
    Task<List<User>> GetActiveUsersAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
}