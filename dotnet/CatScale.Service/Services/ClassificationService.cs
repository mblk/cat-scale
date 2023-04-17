using CatScale.Domain.Model;

namespace CatScale.Service.Services;

public interface IClassificationService
{
    public Task ClassifyScaleEvent(ScaleEvent scaleEvent);
}

public class ClassificationService : IClassificationService
{
    private readonly ILogger<ClassificationService> _logger;

    public ClassificationService(ILogger<ClassificationService> logger)
    {
        _logger = logger;
    }

    public Task ClassifyScaleEvent(ScaleEvent scaleEvent)
    {
        // TODO StablePhases / Cleaning / Measurement might be null if not included in ef-query.
        
        
        var negativeWeightPhases = scaleEvent.StablePhases
            .Where(sp => sp.Length > 5.0 && sp.Value < -100.0)
            .OrderBy(sp => sp.Timestamp)
            .ToArray();

        var positiveWeightPhases = scaleEvent.StablePhases
            .Where(sp => sp.Length > 5.0 && sp.Value > 100.0)
            .OrderBy(sp => sp.Timestamp)
            .ToArray();
        
        _logger.LogInformation($"Negative weight phases: {negativeWeightPhases.Length}");
        foreach (var sp in negativeWeightPhases)
            _logger.LogInformation($"-- {sp}");
        _logger.LogInformation($"Positive weight phases: {positiveWeightPhases.Length}");
        foreach (var sp in positiveWeightPhases)
            _logger.LogInformation($"++ {sp}");

        
        
        
        

        return Task.CompletedTask;
    }
}