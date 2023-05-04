namespace CatScale.UI.BlazorServer.Utils;

public static class TimestampFormatter
{
    public static string Format(DateTimeOffset timestamp)
    {
        // TODO: Target timezone depends on client and might be different for different sessions.
        // Use JS-interop solution to get offset from browser?

        var t = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(timestamp.UtcDateTime, "Europe/Berlin");
        var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Berlin");
        var diff = now - t;
        
        if (t.Date == now.Date)
        {
            return $"Heute {t.ToLongTimeString()} (vor {diff.TotalHours:F1} Stunden)";
        }
        else if (t.Date == now.Date.AddDays(-1))
        {
            return $"Gestern {t.ToLongTimeString()}";
        }
        else
        {
            return $"{t.ToShortDateString()} {t.ToShortTimeString()}";
        }
    }
}