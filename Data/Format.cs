using System.Globalization;

namespace ShopRecorder.Data;

/// <summary>Thrown when a sale tries to sell more units than are in stock.</summary>
public class InsufficientStockException : Exception
{
    public InsufficientStockException(string productName, int available)
        : base($"Not enough stock for {productName} — only {available} left.")
    {
    }
}

/// <summary>Small formatting helpers shared by all pages.</summary>
public static class Fmt
{
    public static string Money(decimal v) => "GHS " + v.ToString("#,##0.00", CultureInfo.InvariantCulture);

    public static string Num(int v) => v.ToString("#,##0", CultureInfo.InvariantCulture);

    public static string Date(DateTime utc) => ShopClock.ToShop(utc).ToString("dd MMM yyyy");

    public static string DateTime(DateTime utc) => ShopClock.ToShop(utc).ToString("dd MMM yyyy, h:mm tt");

    public static string Time(DateTime utc) => ShopClock.ToShop(utc).ToString("h:mm tt");

    public static string ReceiptNo(int id) => "SP-" + id.ToString("D6");

    /// <summary>Shop name shown on the receipt (set SHOP_NAME env var to customize).</summary>
    public static string ShopName =>
        Environment.GetEnvironmentVariable("SHOP_NAME") is { Length: > 0 } s ? s : "MY SHOP";
}
