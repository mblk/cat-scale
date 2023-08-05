using System.Diagnostics;
using System.Text;
using CatScale.FilterConfigTool.Persistence;
using CatScale.FilterConfigTool.Simulation;

namespace CatScale.FilterConfigTool.Visualization;

public class GnuPlot
{
    public static async Task ShowFromFile(string path)
    {
        var config = new StringBuilder()
            .AppendLine($"set xdata time")
            .AppendLine($"set timefmt \"%Y-%m-%dT%H:%M:%SZ\"")
            .AppendLine($"set format x \"%H:%M:%S\"")
            .AppendLine($"set datafile separator \",\"")
            .AppendLine($"set grid")
            .AppendLine($"plot \"{path}\" using 1:2 title '' with lines")
            .AppendLine($"pause mouse close");

        using (var process = new Process())
        {
            process.StartInfo.FileName = "gnuplot";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;

            process.Start();

            var inputWriter = process.StandardInput;
            await inputWriter.WriteAsync(config);
            inputWriter.Close();

            //await process.WaitForExitAsync();
        }
    }

    public static async Task ShowSimulationResult(SimulationResult result)
    {
        var config = new StringBuilder()
            .AppendLine("set xdata time")
            .AppendLine("set timefmt '%Y-%m-%dT%H:%M:%SZ'")
            .AppendLine("set format x '%H:%M:%S'")
            .AppendLine("set datafile separator ','")
            .AppendLine("set grid");
        
        config.Append("plot '-' using 1:2 title 'filtered' with lines");
        foreach (var (id, _) in result.DebugData)
            config.Append($", '-' using 1:2 title '{id}' with lines");
        config.AppendLine();

        config.Append(WeightDataWriter.ConvertToCsv(result.Output)).AppendLine("e");
        foreach (var (id, values) in result.DebugData)
            config.Append(WeightDataWriter.ConvertToCsv(values)).AppendLine("e");
        
        config.AppendLine("pause mouse close");

        using (var process = new Process())
        {
            process.StartInfo.FileName = "gnuplot";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;

            process.Start();

            var inputWriter = process.StandardInput;
            await inputWriter.WriteAsync(config);
            inputWriter.Close();

            //await process.WaitForExitAsync();
        }
    }
    
    public static async Task RenderSimulationResult(SimulationResult result, string outputPath)
    {
        var config = new StringBuilder()
            .AppendLine($"set terminal svg size 1400,500 font 'Helvetica,12'")
            .AppendLine($"set output '{outputPath}'")
            .AppendLine("set xdata time")
            .AppendLine("set timefmt '%Y-%m-%dT%H:%M:%SZ'")
            .AppendLine("set format x '%H:%M:%S'")
            .AppendLine("set datafile separator ','")
            .AppendLine("set grid");
        
        config.Append("plot '-' using 1:2 title 'filtered' with lines");
        foreach (var (id, _) in result.DebugData)
            config.Append($", '-' using 1:2 title '{id}' with lines");
        config.AppendLine();

        config.Append(WeightDataWriter.ConvertToCsv(result.Output)).AppendLine("e");
        foreach (var (id, values) in result.DebugData)
            config.Append(WeightDataWriter.ConvertToCsv(values)).AppendLine("e");

        using (var process = new Process())
        {
            process.StartInfo.FileName = "gnuplot";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;

            process.Start();

            var inputWriter = process.StandardInput;
            await inputWriter.WriteAsync(config);
            inputWriter.Close();

            await process.WaitForExitAsync();
        }
    }
}