using CatScale.Service.Model.Cleaning;
using CatScale.Service.Model.Measurement;

namespace CatScale.Service.Model.ScaleEvent;

public record ScaleEventDto
(
    int Id,
    DateTimeOffset Start,
    DateTimeOffset End,
    StablePhaseDto[] StablePhases,
    CleaningDto? Cleaning,
    MeasurementDto? Measurement
);