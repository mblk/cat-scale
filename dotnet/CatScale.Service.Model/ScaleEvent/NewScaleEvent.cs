using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record NewScaleEvent
(
    // Note: Types are nullable to allow for proper model binding validation.
    [Required] int? ToiletId,
    [Required] DateTimeOffset? StartTime,
    [Required] DateTimeOffset? EndTime,
    [Required] NewStablePhase[]? StablePhases,
    [Required] double? Temperature,
    [Required] double? Humidity,
    [Required] double? Pressure
);