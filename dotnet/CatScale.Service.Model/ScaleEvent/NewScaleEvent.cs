using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record NewScaleEvent // TODO why are these nullable?
(
    [Required] int? ToiletId,
    [Required] DateTimeOffset? StartTime,
    [Required] DateTimeOffset? EndTime,
    NewStablePhase[] StablePhases,
    [Required] double? Temperature,
    [Required] double? Humidity,
    [Required] double? Pressure
);