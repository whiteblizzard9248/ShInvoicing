using System;

namespace ShInvoicing.Models;

public class ApplicationUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}