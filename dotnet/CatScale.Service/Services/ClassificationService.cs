using System.Diagnostics.CodeAnalysis;
using CatScale.Domain.Model;

namespace CatScale.Service.Services;

public interface IClassificationService
{
    bool TryClassifyCatByWeight(IEnumerable<Cat> cats, DateTimeOffset timestamp, double weight, double tolerance, [NotNullWhen(true)] out Cat? cat);
    
    void ClassifyScaleEvent(IEnumerable<Cat> cats, ScaleEvent scaleEvent);
}

public class ClassificationService : IClassificationService
{
    private readonly ILogger<ClassificationService> _logger;

    public ClassificationService(ILogger<ClassificationService> logger)
    {
        _logger = logger;
    }

    public bool TryClassifyCatByWeight(IEnumerable<Cat> cats, DateTimeOffset timestamp, double weight, double tolerance, [NotNullWhen(true)] out Cat? cat)
    {
        // TODO check args

        (Cat Cat, double Weight)[] catsAndWeights = cats
            .Select(c => (Cat: c, c.Weights
                .Where(w => w.Timestamp < timestamp)
                .MaxBy(w => w.Timestamp)
                ?.Weight))
            .Where(t => t.Weight.HasValue)
            .Select(t => (t.Cat, t.Weight!.Value))
            .ToArray();

        (Cat Cat, double Diff)[] catsAndDiffs = catsAndWeights
            .Select(t => (t.Cat, Diff: Math.Abs(weight - t.Weight)))
            .Where(t => t.Diff < tolerance)
            .OrderByDescending(t => t.Diff)
            .ToArray();

        if (catsAndDiffs.Any())
        {
            cat = catsAndDiffs.First().Cat;
            return true;
        }
        
        cat = null;
        return false;
    }
    
    public void ClassifyScaleEvent(IEnumerable<Cat> cats, ScaleEvent scaleEvent)
    {
        // TODO StablePhases / Cleaning / Measurement might be null if not included in ef-query.
        // TODO check args
        
        var negativeWeightPhases = scaleEvent.StablePhases
            .Where(sp => sp.Length > 1.0 && sp.Value < -10.0)
            .OrderBy(sp => sp.Timestamp)
            .ToArray();

        var positiveWeightPhases = scaleEvent.StablePhases
            .Where(sp => sp.Length > 1.0 && sp.Value > 10.0)
            .OrderBy(sp => sp.Timestamp)
            .ToArray();

        // Clear old classification.
        scaleEvent.Cleaning = null;
        scaleEvent.Measurement = null;
        
        // Cleaning?
        if (negativeWeightPhases.Length > 0 && positiveWeightPhases.Length == 0)
        {
            _logger.LogInformation($"Looks like cleaning ...");
            
            var maxValue = negativeWeightPhases.Max(x => x.Value);
            var minValue = negativeWeightPhases.Min(x => x.Value);
            var cleaningWeight = maxValue - minValue;
            
            // TODO detect mass going down & optionally up again during refills?

            scaleEvent.Cleaning = new Cleaning()
            {
                Timestamp = scaleEvent.StartTime,
                Time = (scaleEvent.EndTime - scaleEvent.StartTime).TotalSeconds,
                ScaleEvent = scaleEvent, // Note: If a cleaning already exists for the scale-event, it is somehow automatically deleted by entity-framework.
                Weight = cleaningWeight,
            };
            
            _logger.LogInformation($"Classified scale event as cleaning");
            return;
        }
        
        // Measurement?
        if(positiveWeightPhases.Length > 0 && negativeWeightPhases.Length <= 1)
        {
            _logger.LogInformation($"Looks like measurement ...");

            var longestStablePhase = positiveWeightPhases
                .OrderByDescending(x => x.Length)
                .First();

            var catWeight = longestStablePhase.Value;

            if (!TryClassifyCatByWeight(cats, scaleEvent.StartTime, catWeight, 500d, out var cat))
            {
                _logger.LogError($"Can't classify cat by weight {catWeight}");
                return;
            }
            
            var pooStartTime = longestStablePhase.Timestamp.AddSeconds(-longestStablePhase.Length);
            var pooEndTime = longestStablePhase.Timestamp;
            var setupDuration = (pooStartTime - scaleEvent.StartTime).TotalSeconds;
            var pooDuration = (pooEndTime - pooStartTime).TotalSeconds;
            var cleanupDuration = (scaleEvent.EndTime - pooEndTime).TotalSeconds;

            var pooWeight = negativeWeightPhases.Length == 1
                ? Math.Abs(negativeWeightPhases.Single().Value)
                : 0d;
            
            scaleEvent.Measurement = new Measurement()
            {
                Timestamp = scaleEvent.StartTime,
                ScaleEvent = scaleEvent,
                SetupTime = setupDuration,
                PooTime = pooDuration,
                CleanupTime = cleanupDuration,
                CatWeight = catWeight,
                PooWeight = pooWeight,
                Cat = cat,
            };
            
            _logger.LogInformation($"Classified scale event as measurement");
            return;
        }

        _logger.LogInformation($"Unable to classify scale event");
    }
}