using SQLite;

namespace ShopRecorder.Data;

/// <summary>A product stocked by the shop.</summary>
public class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(120), NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>Selling price in Ghana Cedis (GHS).</summary>
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>When stock falls to or below this level the product is flagged as low stock. Default 5.</summary>
    public int LowStockThreshold { get; set; } = 5;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLowStock => StockQuantity <= LowStockThreshold;
}
