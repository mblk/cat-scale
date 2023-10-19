namespace CatScale.Service.Tests.Utils;

public class DateTimeOffsetComparer : IEqualityComparer<DateTimeOffset>
{
    private readonly TimeSpan _maxDelta;

    public DateTimeOffsetComparer(TimeSpan maxDelta)
    {
        _maxDelta = maxDelta;
    }

    public bool Equals(DateTimeOffset x, DateTimeOffset y)
    {
        return (x - y) < _maxDelta;
    }

    public int GetHashCode(DateTimeOffset obj)
    {
        return obj.Ticks.GetHashCode();
    }
}