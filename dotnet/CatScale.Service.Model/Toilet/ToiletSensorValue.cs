using JetBrains.Annotations;

namespace CatScale.Service.Model.Toilet;

[PublicAPI]
public enum ToiletSensorValue
{
    RawWeight,
    Weight,
    Temperature,
    Humidity,
    Pressure,
    Co2,
    Tvoc,
}