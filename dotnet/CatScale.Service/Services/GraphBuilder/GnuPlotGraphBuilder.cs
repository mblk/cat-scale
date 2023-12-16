using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace CatScale.Service.Services.GraphBuilder;

public class GnuPlotGraphBuilder : IGraphBuilder
{
    private record Box(DateTimeOffset T1, DateTimeOffset T2, double V1, double V2, string Label);

    private readonly Dictionary<int, string> _axis = new();
    private readonly List<GraphDataSet> _dataSets = new();
    private readonly List<Box> _boxes = new();

    public IGraphBuilder AddAxis(int axis, string label)
    {
        _axis.Add(axis, label);
        return this;
    }

    public IGraphBuilder AddDataset(GraphDataSet dataSet)
    {
        if (!dataSet.Points.Any()) throw new ArgumentException("Dataset does not contain any points");

        _dataSets.Add(dataSet);
        return this;
    }

    public IGraphBuilder AddBox(DateTimeOffset t1, DateTimeOffset t2, double v1, double v2, string label)
    {
        DateTimeOffset tMin, tMax;
        double vMin, vMax;

        if (t1 < t2)
        {
            tMin = t1;
            tMax = t2;
        }
        else
        {
            tMin = t2;
            tMax = t1;
        }

        if (v1 < v2)
        {
            vMin = v1;
            vMax = v2;
        }
        else
        {
            vMin = v2;
            vMax = v1;
        }
        
        _boxes.Add(new Box(tMin, tMax, vMin, vMax, label));
        return this;
    }

    public async Task<byte[]> Build()
    {
        if (!_dataSets.Any())
            throw new InvalidOperationException("No datasets added");

        var axis = _dataSets.Select(x => x.Axis).Distinct().Order().ToArray();
        var axisCount = axis.Length;
        var axisMapping = new Dictionary<int, string>();

        // Common
        var config = new StringBuilder()
            .AppendLine("set terminal svg size 800,300 font 'Helvetica,12'")
            .AppendLine("set datafile separator ','")
            .AppendLine("set grid");

        // x
        var start = _dataSets.Min(x => x.Points.Min(p => p.Time));
        var end = _dataSets.Max(x => x.Points.Max(p => p.Time));
        var timeRange = end - start;
        var xFormat = timeRange.TotalDays > 1.5 ? "%d.%m" : "%H:%M";
        config
            .AppendLine("set xdata time")
            .AppendLine("set timefmt '%Y-%m-%dT%H:%M:%SZ'")
            .AppendLine($"set xrange ['{Format(start)}':'{Format(end)}']")
            .AppendLine($"set format x '{xFormat}'");

        // y1
        {
            var firstAxis = axis[0];
            config.AppendLine($"set yrange {FormatValueRange(firstAxis)}");

            if (_axis.TryGetValue(firstAxis, out var label))
                config.AppendLine($"set ylabel '{label}'");

            axisMapping.Add(firstAxis, "x1y1");
        }

        // y2
        if (axisCount > 1)
        {
            var secondAxis = axis[1];
            config.AppendLine($"set y2range {FormatValueRange(secondAxis)}");
            config.AppendLine("set ytics nomirror");
            config.AppendLine("set y2tics");

            if (_axis.TryGetValue(secondAxis, out var label))
                config.AppendLine($"set y2label '{label}'");

            axisMapping.Add(secondAxis, "x1y2");
        }

        // boxes
        if (_boxes.Any())
        {
            config.AppendLine("set style rect fc lt 3 fs solid 0.5 noborder");
            var nextObjId = 1;
            var labelOffset = false;
            foreach (var box in _boxes)
            {
                config.AppendLine($"set object {nextObjId++} rect from '{Format(box.T1)}',{Format(box.V1)} to '{Format(box.T2)}',{Format(box.V2)}");

                if (!String.IsNullOrWhiteSpace(box.Label))
                {
                    var dv = box.V2 - box.V1;

                    var vLabel = labelOffset
                        ? box.V1 - dv * 0.5d
                        : box.V1 + dv * 1.5d;

                    labelOffset = !labelOffset;
                    
                    config.AppendLine($"set label '{box.Label}' at '{Format(box.T1)}',{Format(vLabel)}");
                }
            }
        }

        // plots
        var colors = new[]
        {
            "A0A0A0",
            "000000",
            "FF0000",
            "00FF00",
            "0000FF",
        };
        var nextColor = 0;

        string getNextColor()
        {
            if (_dataSets.Count == 1)
                return "000000";
            var c = colors[nextColor];
            nextColor = (nextColor + 1) % colors.Length;
            return c;
        }

        var plots = new List<string>();

        foreach (var dataSet in _dataSets)
        {
            var p = new StringBuilder()
                .Append("'-' using 1:2")
                .Append($" title '{dataSet.Name}'")
                .Append(" with lines")
                .Append($" axes {axisMapping[dataSet.Axis]}")
                .Append($" lc rgb '#{getNextColor()}'");
            plots.Add(p.ToString());
        }

        config.AppendLine($"plot {String.Join(", ", plots)}");

        // data
        foreach (var dataSet in _dataSets)
        {
            foreach (var dataPoint in dataSet.Points)
                config.AppendLine($"{Format(dataPoint.Time)},{Format(dataPoint.Value)}");
            config.AppendLine("e");
        }

        return await ExecuteGnuplot(config);
    }

    private static string Format(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    private static string Format(double value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    private string FormatValueRange(int axis)
    {
        var matchingSets = _dataSets
            .Where(x => x.Axis == axis)
            .ToArray();

        var minValue = matchingSets.Min(x => x.Points.Min(p => p.Value));
        var maxValue = matchingSets.Max(x => x.Points.Max(p => p.Value));

        var valueRange = maxValue - minValue;
        var valueExtra = valueRange * 0.1;

        return $"[{Format(minValue - valueExtra)}:{Format(maxValue + valueExtra)}]";
    }

    private static async Task<byte[]> ExecuteGnuplot(StringBuilder config)
    {
        using var process = new Process();
        process.StartInfo.FileName = "gnuplot";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        var errorBuffer = new StringBuilder();
        var outputBuffer = new StringBuilder();

        process.ErrorDataReceived += (_, args) => errorBuffer.AppendLine(args.Data);
        process.OutputDataReceived += (_, args) => outputBuffer.AppendLine(args.Data);

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        var inputWriter = process.StandardInput;
        await inputWriter.WriteAsync(config);
        inputWriter.Close();

        await process.WaitForExitAsync();

        var error = errorBuffer.ToString();
        var output = outputBuffer.ToString();

        if (process.ExitCode != 0)
            throw new Exception($"Process exited with code {process.ExitCode}, std-error: {error}");

        var bytes = Encoding.UTF8.GetBytes(output);

        return bytes;
    }
}