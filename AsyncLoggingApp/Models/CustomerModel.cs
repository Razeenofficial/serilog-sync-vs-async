namespace AsyncLoggingApp.Models;

public class CustomerModel
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime RegisteredAt { get; set; }
}
