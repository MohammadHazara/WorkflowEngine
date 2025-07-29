using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowEngine.Models;

/// <summary>
/// Represents a user in the workflow engine system
/// </summary>
[Table("Users")]
public sealed class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique username for authentication
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public required string Username { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    [Column(TypeName = "varchar(255)")]
    public required string Email { get; set; }

    /// <summary>
    /// Hashed password for authentication
    /// </summary>
    [Required]
    [StringLength(255)]
    [Column(TypeName = "varchar(255)")]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// User's role in the system
    /// </summary>
    [Required]
    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public required string Role { get; set; }

    /// <summary>
    /// Indicates if the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Navigation property for workflows created by this user
    /// </summary>
    public ICollection<Workflow> CreatedWorkflows { get; set; } = new List<Workflow>();

    /// <summary>
    /// Navigation property for workflow executions started by this user
    /// </summary>
    public ICollection<WorkflowExecution> StartedExecutions { get; set; } = new List<WorkflowExecution>();
}

/// <summary>
/// User roles available in the system
/// </summary>
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";

    /// <summary>
    /// Gets all available roles
    /// </summary>
    public static string[] GetAllRoles() => [Admin, Operator, Viewer];

    /// <summary>
    /// Validates if a role is valid
    /// </summary>
    public static bool IsValidRole(string role) => GetAllRoles().Contains(role);
}