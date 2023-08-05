using System.Globalization;
using System.Text;

namespace CatScale.FilterConfigTool.Persistence;

public static class WeightDataWriter
{
    public static async Task WriteToFile(string path, IEnumerable<(DateTimeOffset, double)> weightData)
    {
        var csvData = ConvertToCsv(weightData);
        await File.WriteAllTextAsync(path, csvData);
    }
    
    public static string ConvertToCsv(IEnumerable<(DateTimeOffset, double)> data)
    {
        var sb = new StringBuilder();

        foreach (var m in data)
        {
            sb.Append(ConvertDateTimeOffsetToString(m.Item1))
                .Append(",")
                .Append(ConvertDoubleToString(m.Item2))
                .AppendLine();
        }
        
        return sb.ToString();
    }

    private static string ConvertDateTimeOffsetToString(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
    
    private static string ConvertDoubleToString(double value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
