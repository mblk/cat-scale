using CatScale.Service.Model.Cleaning;
using CatScale.Service.Model.Measurement;
using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record ScaleEventDto
(
    int Id,
    int ToiletId,
    DateTimeOffset Start,
    DateTimeOffset End,
    StablePhaseDto[] StablePhases,
    CleaningDto? Cleaning,
    MeasurementDto? Measurement,
    double Temperature,
    double Humidity,
    double Pressure
);