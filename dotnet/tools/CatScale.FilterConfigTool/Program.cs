using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CatScale.FilterConfigTool.Persistence;
using CatScale.FilterConfigTool.Simulation;
using CatScale.FilterConfigTool.Visualization;

namespace CatScale.FilterConfigTool;

public static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            if (args.Length < 1)
            {
                await Console.Error.WriteLineAsync($"Missing action");
                return;
            }

            var action = args[0];
            var actionArgs = args.Skip(1).ToArray();
            
            switch (action)
            {
                case "download": await Download(actionArgs); break;
                case "show": await Show(actionArgs); break;
                case "simulate": await Simulate(actionArgs); break;
                case "simulate_all": await SimulateAll(actionArgs); break;
                default: await Console.Error.WriteLineAsync($"Invalid action: '{action}'"); break;
            }
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Error: {e.Message}");
        }
    }

    private static async Task Download(string[] args)
    {
        var configFileName = args[0];
        var outputFileName = args[1];
        
        var startTime = new DateTimeOffset(DateTime.ParseExact(args[2], "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture), TimeSpan.Zero);
        var length = TimeSpan.ParseExact(args[3], "c", CultureInfo.InvariantCulture);
        var endTime = startTime + length;

        Console.WriteLine($"Downloading weight data ..."); 
        Console.WriteLine($"Start:   '{startTime:u}'");
        Console.WriteLine($"Length:  '{length}'");
        Console.WriteLine($"End:     '{endTime:u}'");

        var config = JsonSerializer.Deserialize<DownloadConfig>(File.Open(configFileName, FileMode.Open))
                     ?? throw new Exception("Invalid config");
        var influxReader = new InfluxReader(config.InfluxDb);
        var weightData = await influxReader.GetWeightDataFromInflux(startTime, endTime);

        await WeightDataWriter.WriteToFile(outputFileName, weightData);
        
        await Show(new[] { outputFileName });
    }

    private static async Task Show(string[] args)
    {
        var fileName = args[0];
        
        await GnuPlot.ShowFromFile(fileName);
    }

    private static async Task Simulate(string[] args)
    {
        var inputFileName = args[0];
        
        var weightData = await WeightDataReader.ReadFromFile(inputFileName);
        var simResult = new Simulator().Simulate(weightData);

        simResult.EventBuffer.Dump();
        
        await GnuPlot.ShowSimulationResult(simResult);
    }

    private static async Task SimulateAll(string[] args)
    {
        var inputDirectoryName = args[0];
        var outputDirectoryName = args[1];

        if (!Directory.Exists(outputDirectoryName))
            Directory.CreateDirectory(outputDirectoryName);

        var directory = new DirectoryInfo(inputDirectoryName);
        var inputFiles = directory.GetFiles().OrderBy(fi => fi.Name).ToArray();

        var htmlBuilder = new StringBuilder()
            .AppendLine("<html style=\"filter: invert(1)\"><body style=\"background: white\">");

        foreach (var file in inputFiles)
        {
            var weightData = await WeightDataReader.ReadFromFile(file.FullName);
            var simResult = new Simulator().Simulate(weightData);
            
            var outputFileName = $"{file.Name[..^file.Extension.Length]}.svg";
            var outputFilePath = $"{outputDirectoryName}/{outputFileName}";
            
            await GnuPlot.RenderSimulationResult(simResult, outputFilePath);

            htmlBuilder.AppendLine($"<p>{file.Name}</p><img src=\"{outputFileName}\">");
            GenerateScaleEventTables(htmlBuilder, simResult.EventBuffer);

            Console.WriteLine($"----- {file.Name} -----");
            simResult.EventBuffer.Dump();
        }

        htmlBuilder.AppendLine("</body></html>");

        var htmlFilePath = $"{outputDirectoryName}/index.html";
        await File.WriteAllTextAsync(htmlFilePath, htmlBuilder.ToString());
        Process.Start("firefox", htmlFilePath);
    }

    private static void GenerateScaleEventTables(StringBuilder sb, EventBuffer eventBuffer)
    {
        foreach (var scaleEvent in eventBuffer.ScaleEvents)
        {
            sb.AppendLine($"<table>");
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<th>Time</td>");
            sb.AppendLine($"<th>Length</td>");
            sb.AppendLine($"<th>Weight</td>");
            sb.AppendLine($"</tr>");
                
            foreach (var stablePhase in scaleEvent.StablePhases)
            {
                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td>{stablePhase.Timestamp.UtcDateTime.ToString("HH:mm:ss")}</td>");
                sb.AppendLine($"<td>{stablePhase.Length:F1}s</td>");
                sb.AppendLine($"<td>{stablePhase.Weight:F1}g</td>");
                sb.AppendLine($"</tr>");
            }
            sb.AppendLine($"<table><br />");
        }
    }
}
