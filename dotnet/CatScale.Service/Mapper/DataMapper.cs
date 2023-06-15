using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Model.Cat;
using CatScale.Service.Model.Cleaning;
using CatScale.Service.Model.Food;
using CatScale.Service.Model.Measurement;
using CatScale.Service.Model.ScaleEvent;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Model.User;
using Riok.Mapperly.Abstractions;

namespace CatScale.Service.Mapper;

[Mapper]
public static partial class DataMapper
{
    public static partial ToiletDto MapToilet(Toilet toilet);
    public static partial CatDto MapCat(Cat cat);
    public static partial CatWeightDto MapCatWeight(CatWeight catWeight);
    public static partial MeasurementDto MapMeasurement(Measurement measurement);
    public static partial CleaningDto MapCleaning(Cleaning cleaning);
    public static partial FoodDto MapFood(Food food);
    public static partial FeedingDto MapFeeding(Feeding feeding);
    
    // Enums
    public static partial CatTypeDto MapCatType(CatType catType);
    public static partial CatType MapCatType(CatTypeDto catType);


    //public partial ApplicationUserDto MapApplicationUser(ApplicationUser user);
    //public partial UserApiKeyDto MapUserApiKey(UserApiKey apiKey);

    //
    // Custom mappers for entities which Mapperly fails to map:
    //

    public static UserApiKeyDto MapUserApiKey(UserApiKey apiKey)
    {
        return new UserApiKeyDto(apiKey.Id, apiKey.Value, DateTime.MaxValue);
    }

    public static ScaleEventDto MapScaleEvent(ScaleEvent scaleEvent)
    {
        var measurementDto = scaleEvent.Measurement != null ? MapMeasurement(scaleEvent.Measurement) : null;
        var cleaningDto = scaleEvent.Cleaning != null ? MapCleaning(scaleEvent.Cleaning) : null; 

        return new ScaleEventDto(scaleEvent.Id, scaleEvent.ToiletId, scaleEvent.StartTime, scaleEvent.EndTime,
            scaleEvent.StablePhases?.Select(MapStablePhase).ToArray() ?? Array.Empty<StablePhaseDto>(),
            cleaningDto,
            measurementDto,
            scaleEvent.Temperature,
            scaleEvent.Humidity,
            scaleEvent.Pressure);
    }

    public static StablePhaseDto MapStablePhase(StablePhase stablePhase)
    {
        return new StablePhaseDto(stablePhase.Id, stablePhase.Timestamp, stablePhase.Length, stablePhase.Value);
    }
}
