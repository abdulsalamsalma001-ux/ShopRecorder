namespace ShopRecorder.Data;

/// <summary>
/// Central clock for the shop. Sales are stored in UTC, and "today" is computed in the
/// shop's local timezone (default: Accra) so daily totals are correct even when the
/// hosting server runs in UTC (e.g. Render).
/// Override with the APP_TIMEZONE environment variable (IANA id, e.g. "Africa/Accra").
/// </summary>
public static class ShopClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        var id = Environment.GetEnvironmentVariable("APP_TIMEZONE");
        if (string.IsNullOrWhiteSpace(id)) id = "Africa/Accra";
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch
        {
            return TimeZoneInfo.Local; // sensible fallback for local dev
        }
    }

    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Converts a UTC instant to shop-local wall time.</summary>
    public static DateTime ToShop(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>Start of the current shop-local day, expressed in UTC.</summary>
    public static DateTime TodayStartUtc()
    {
        var local = ToShop(DateTime.UtcNow);
        var startLocal = local.Date;
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), Zone);
    }
}
