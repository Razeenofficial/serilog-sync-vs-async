namespace AsyncLoggingApp.Models;

public class OrderModel
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
