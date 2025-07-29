using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WorkflowEngine.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Data;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for handling user authentication and JWT token management
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly byte[] _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

    /// <summary>
    /// Initializes a new instance of the AuthenticationService
    /// </summary>
    /// <param name="userService">User service for user operations</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger instance</param>
    public AuthenticationService(
        IUserService userService,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jwtKey = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "WorkflowEngine-Super-Secret-Key-For-Development-Only");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? "WorkflowEngine";
        _jwtAudience = _configuration["Jwt:Audience"] ?? "WorkflowEngine";
        _jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
    }

    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="password">The password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token if authentication successful, null otherwise</returns>
    public async Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Authentication failed: Empty username or password");
                return null;
            }

            var user = await _userService.GetUserByUsernameAsync(username, cancellationToken);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Authentication failed: User {Username} not found or inactive", username);
                return null;
            }

            if (!_userService.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed: Invalid password for user {Username}", username);
                return null;
            }

            await _userService.UpdateLastLoginAsync(user.Id, cancellationToken);

            var token = GenerateJwtToken(user);
            _logger.LogInformation("User {Username} authenticated successfully", username);
            
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Validates a JWT token and returns the user ID
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>User ID if token is valid, null otherwise</returns>
    public async Task<int?> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return null;

            // Verify user still exists and is active
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null || !user.IsActive)
                return null;

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Refreshes an existing JWT token
    /// </summary>
    /// <param name="token">The current token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New JWT token if refresh successful, null otherwise</returns>
    public async Task<string?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await ValidateTokenAsync(token);
            if (!userId.HasValue)
                return null;

            var user = await _userService.GetUserByIdAsync(userId.Value, cancellationToken);
            if (user == null || !user.IsActive)
                return null;

            var newToken = GenerateJwtToken(user);
            _logger.LogInformation("Token refreshed for user {Username}", user.Username);
            
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    /// <param name="user">The user to generate token for</param>
    /// <returns>JWT token string</returns>
    private string GenerateJwtToken(Models.User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(_jwtKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("user_id", user.Id.ToString()),
            new Claim("username", user.Username)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Gets token validation parameters
    /// </summary>
    /// <returns>Token validation parameters</returns>
    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_jwtKey),
            ValidateIssuer = true,
            ValidIssuer = _jwtIssuer,
            ValidateAudience = true,
            ValidAudience = _jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}