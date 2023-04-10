using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Model.Cat;
using CatScale.Service.Model.Cleaning;
using CatScale.Service.Model.Measurement;
using CatScale.Service.Model.ScaleEvent;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Model.User;
using Riok.Mapperly.Abstractions;

namespace CatScale.Service.Mapper;

[Mapper]
public static partial class DataMapper // TODO static extension methods?
{
    public static partial ToiletDto MapToilet(Toilet toilet);
    public static partial CatDto MapCat(Cat cat);
    public static partial MeasurementDto? MapMeasurement(Measurement measurement);
    public static partial CleaningDto MapCleaning(Cleaning cleaning);
    public static partial CatWeightDto MapCatWeight(CatWeight catWeight);

    //public partial ApplicationUserDto MapApplicationUser(ApplicationUser user);
    //public partial UserApiKeyDto MapUserApiKey(UserApiKey apiKey);

    public static UserApiKeyDto MapUserApiKey(UserApiKey apiKey)
    {
        return new UserApiKeyDto(apiKey.Id, apiKey.Value, DateTime.MaxValue);
    }

    public static ScaleEventDto MapScaleEvent(ScaleEvent scaleEvent)
    {
        var measurementDto = scaleEvent.Measurement != null ? MapMeasurement(scaleEvent.Measurement) : null;
        var cleaningDto = scaleEvent.Cleaning != null ? MapCleaning(scaleEvent.Cleaning) : null; 

        return new ScaleEventDto(scaleEvent.Id, scaleEvent.StartTime, scaleEvent.EndTime,
            scaleEvent.StablePhases.Select(MapStablePhase).ToArray(), cleaningDto, measurementDto);
    }

    public static StablePhaseDto MapStablePhase(StablePhase stablePhase)
    {
        return new StablePhaseDto(stablePhase.Id, stablePhase.Timestamp, stablePhase.Length, stablePhase.Value);
    }
}
