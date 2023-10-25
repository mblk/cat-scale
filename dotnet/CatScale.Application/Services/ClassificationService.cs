using System.Diagnostics.CodeAnalysis;
using CatScale.Domain.Model;

namespace CatScale.Application.Services;

public interface IClassificationService // TODO should this be an Interactor instead?
{
    bool TryClassifyCatByWeight(IEnumerable<Cat> cats, DateTimeOffset timestamp, double weight, double tolerance,
        [NotNullWhen(true)] out Cat? cat);

    void ClassifyScaleEvent(IEnumerable<Cat> cats, ScaleEvent scaleEvent);
}

public class ClassificationService : IClassificationService
{
    public bool TryClassifyCatByWeight(IEnumerable<Cat> cats, DateTimeOffset timestamp, double weight, double tolerance,
        [NotNullWhen(true)] out Cat? cat)
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
            .OrderBy(t => t.Diff)
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

        var allNegativeWeightPhases = scaleEvent.StablePhases
            .Where(sp => sp is { Length: > 1.0, Value: < -10.0 })
            .OrderBy(sp => sp.Timestamp)
            .ToArray();

        var allPositiveWeightPhases = scaleEvent.StablePhases
            .Where(sp => sp is { Length: > 1.0, Value: > 10.0 })
            .OrderBy(sp => sp.Timestamp)
            .ToArray();

        var significantNegativeWeightPhases = allNegativeWeightPhases
            .Where(sp => sp.Value < -500.0)
            .ToArray();

        var significantPositiveWeightPhases = allPositiveWeightPhases
            .Where(sp => sp.Value > 5000.0)
            .ToArray();

        // Clear old classification.
        scaleEvent.Cleaning = null;
        scaleEvent.Measurement = null;

        // Cleaning?
        if (significantNegativeWeightPhases.Any())
        {
            //_logger.LogInformation($"Looks like cleaning ...");

            // Try to determine removed weight.
            double maxValue = significantNegativeWeightPhases.First().Value;
            double minValue = significantNegativeWeightPhases.Length > 1
                ? significantNegativeWeightPhases.Skip(1).Min(sp => sp.Value)
                : maxValue;
            double cleaningWeight = maxValue - minValue;
            //_logger.LogInformation($"cleaningWeight 1: {cleaningWeight}");

            // Optionally use last phase instead?
            double lastNegativePhaseValue = allNegativeWeightPhases.Last().Value;
            if (lastNegativePhaseValue > -500.0)
                cleaningWeight = lastNegativePhaseValue;
            //_logger.LogInformation($"cleaningWeight 2: {cleaningWeight}");

            scaleEvent.Cleaning = new Cleaning()
            {
                Timestamp = scaleEvent.StartTime,
                Time = (scaleEvent.EndTime - scaleEvent.StartTime).TotalSeconds,
                ScaleEventId =
                    scaleEvent.Id, // Note: If a cleaning already exists for the scale-event, it is somehow automatically deleted by entity-framework.
                Weight = cleaningWeight,
            };

            //_logger.LogInformation($"Classified scale event as cleaning");
            return;
        }

        // Measurement?
        if (significantPositiveWeightPhases.Any())
        {
            //_logger.LogInformation($"Looks like measurement ...");

            var longestStablePhase = significantPositiveWeightPhases
                .OrderByDescending(x => x.Length)
                .First();
            var catWeight = longestStablePhase.Value;

            if (!TryClassifyCatByWeight(cats, scaleEvent.StartTime, catWeight, 500d, out var cat))
            {
                //_logger.LogError($"Can't classify cat by weight {catWeight}");
                return;
            }

            var pooStartTime = longestStablePhase.Timestamp.AddSeconds(-longestStablePhase.Length);
            var pooEndTime = longestStablePhase.Timestamp;
            var setupDuration = (pooStartTime - scaleEvent.StartTime).TotalSeconds;
            var pooDuration = (pooEndTime - pooStartTime).TotalSeconds;
            var cleanupDuration = (scaleEvent.EndTime - pooEndTime).TotalSeconds;

            var pooWeight = allPositiveWeightPhases.Last().Value;
            if (pooWeight is < 0 or > 500.0) pooWeight = 0.0;

            scaleEvent.Measurement = new Measurement()
            {
                Timestamp = scaleEvent.StartTime,
                ScaleEventId = scaleEvent.Id,
                SetupTime = setupDuration,
                PooTime = pooDuration,
                CleanupTime = cleanupDuration,
                CatWeight = catWeight,
                PooWeight = pooWeight,
                CatId = cat.Id,
            };

            //_logger.LogInformation($"Classified scale event as measurement");
            return;
        }

        //_logger.LogInformation($"Unable to classify scale event");
    }
}