namespace SyncLoggingApp.Models;

public class SupplierModel
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Rating { get; set; }
}
