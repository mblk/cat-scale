using System.Globalization;

namespace CatScale.FilterConfigTool.Persistence;

public static class WeightDataReader
{
    public static async Task<IReadOnlyList<(DateTimeOffset, double)>> ReadFromFile(string path)
    {
        return (await File.ReadAllLinesAsync(path))
            .Select(ParseLine)
            .ToArray();
    }
    
    private static (DateTimeOffset, double) ParseLine(string line)
    {
        var parts = line.Split(new[] { ',' }, StringSplitOptions.TrimEntries);
        if (parts.Length != 2) throw new ArgumentException("Invalid line");

        var t = DateTimeOffset.ParseExact(parts[0], "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var v = Double.Parse(parts[1], NumberStyles.Float);

        return (t, v);
    }
}