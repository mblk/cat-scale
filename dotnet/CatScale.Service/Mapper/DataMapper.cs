using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Model.Cat;
using CatScale.Service.Model.Cleaning;
using CatScale.Service.Model.Measurement;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Model.User;
using Riok.Mapperly.Abstractions;

namespace CatScale.Service.Mapper;

[Mapper]
public partial class DataMapper // TODO static extension methods?
{
    public partial ToiletDto MapToilet(Toilet toilet);
    public partial CatDto MapCat(Cat cat);
    public partial MeasurementDto MapMeasurement(Measurement measurement);
    public partial CleaningDto MapCleaning(Cleaning cleaning);
    public partial CatWeightDto MapCatWeight(CatWeight catWeight);

    //public partial UserApiKeyDto MapUserApiKey(UserApiKey apiKey);

    public UserApiKeyDto MapUserApiKey(UserApiKey apiKey)
    {
        return new UserApiKeyDto(apiKey.Id, apiKey.Value, DateTime.MaxValue);
    }
}
