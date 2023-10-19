using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record NewStablePhase
(
    // Note: Types are nullable to allow for proper model binding validation.
    [Required] DateTimeOffset? Timestamp,
    [Required] double? Length,
    [Required] double? Value
);
