using System.Text.Json;
using SQLite;

namespace ShopRecorder.Data;

/// <summary>
/// A completed sale. Line items are stored as JSON in <see cref="ItemsJson"/>
/// (simple single-table design, ideal for a small shop).
/// <see cref="SaleDate"/> is always stored in UTC; display times use the shop's timezone (see <see cref="ShopClock"/>).
/// </summary>
public class Sale
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal ChangeDue { get; set; }

    public string ItemsJson { get; set; } = "[]";

    /// <summary>Deserialized line items (not stored as a column).</summary>
    [Ignore]
    public List<SaleItem> Items =>
        string.IsNullOrWhiteSpace(ItemsJson)
            ? new List<SaleItem>()
            : JsonSerializer.Deserialize<List<SaleItem>>(ItemsJson) ?? new List<SaleItem>();

    /// <summary>Total number of units sold in this sale.</summary>
    [Ignore]
    public int ItemsCount => Items.Sum(i => i.Quantity);

    public static string SerializeItems(List<SaleItem> items) => JsonSerializer.Serialize(items);
}
