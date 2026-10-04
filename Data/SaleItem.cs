namespace ShopRecorder.Data;

/// <summary>One line of a sale (product snapshot at the time of sale). Stored as JSON inside <see cref="Sale"/>.</summary>
public class SaleItem
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    /// <summary>Unit price in GHS at the time of sale.</summary>
    public decimal Price { get; set; }

    public decimal Subtotal => Price * Quantity;
}
