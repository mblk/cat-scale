namespace CatScale.UI.BlazorServer.Utils;

public static class TimestampFormatter
{
    public static string Format(DateTimeOffset timestamp)
    {
        var t = timestamp.LocalDateTime;
        var now = DateTime.Now;
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