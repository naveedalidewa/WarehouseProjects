namespace Api.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public string? AccountNumber { get; set; }
    public string BusinessName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PasswordHash { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; }
}
