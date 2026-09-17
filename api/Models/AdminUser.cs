namespace Api.Models;

public class AdminUser
{
    public int AdminId { get; set; }
    public string Email { get; set; } = "";
    public string? PasswordHash { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = "Owner";
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
}
