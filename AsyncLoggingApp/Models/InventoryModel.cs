namespace AsyncLoggingApp.Models;

public class InventoryModel
{
    public int InventoryId { get; set; }
    public int ProductId { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public DateTime LastUpdated { get; set; }
}
