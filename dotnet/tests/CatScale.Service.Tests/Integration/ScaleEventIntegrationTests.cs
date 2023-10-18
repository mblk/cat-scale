using CatScale.Service.Model.Cat;
using CatScale.Service.Model.ScaleEvent;

namespace CatScale.Service.Tests.Integration;

public class ScaleEventIntegrationTests : IntegrationTest
{
    public ScaleEventIntegrationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GetAll_Should_ReturnEmptyList_When_NoToiletsExist()
    {
        var scaleEvents = await GetAllScaleEvents();

        Assert.Empty(scaleEvents);
    }

    [Fact]
    public async Task GetAll_Should_ReturnEmptyList_When_NoEventsExist()
    {
        await Login();
        await CreateToilet("toilet", "desc");
        await Logout();

        var scaleEvents = await GetAllScaleEvents();

        Assert.Empty(scaleEvents);
    }

    [Fact]
    public async Task Create_Should_CreateNewEvent_When_ParametersAreValid()
    {
        // TODO don't use 'Now' (in test and implementation)
        
        //var t0 = new DateTimeOffset(2022, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
        var t0 = DateTimeOffset.Now.AddHours(-2);
        var tWeighing = t0.AddSeconds(0);
        var tStart = t0.AddHours(1);
        var tPhase1 = tStart.AddSeconds(30); // TODO end of stable phase or start?
        var tEnd = tPhase1.AddSeconds(60);

        await Login();
        var createdToilet = await CreateToilet("toilet", "desc");
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        _ = await CreateCatWeight(createdCat.Id, tWeighing, 5000.0d);

        await CreateScaleEvent(new NewScaleEvent(createdToilet.Id, tStart, tEnd,
            new NewStablePhase[]
            {
                new NewStablePhase(tPhase1, 20.0d, 5011.0d),
            },
            22.0d, 50.0d, 100000.0d));

        var scaleEvents = await GetAllScaleEvents();

        Assert.Collection(scaleEvents, e =>
        {
            Assert.Equal(tStart, e.Start, new MyDateTimeOffsetComparer());
            Assert.Equal(tEnd, e.End, new MyDateTimeOffsetComparer());

            Assert.Equal(22.0d, e.Temperature, 0.001d);
            Assert.Equal(50.0d, e.Humidity, 0.001d);
            Assert.Equal(100000.0d, e.Pressure);

            Assert.Null(e.Cleaning);
            Assert.NotNull(e.Measurement);
            Assert.Equal(createdCat.Id, e.Measurement.CatId);
            Assert.Equal(5011.0d, e.Measurement.CatWeight, 0.001d);
        });
    }
    
    private class MyDateTimeOffsetComparer : IEqualityComparer<DateTimeOffset>
    {
        public bool Equals(DateTimeOffset x, DateTimeOffset y)
        {
            return (x - y).TotalSeconds < 0.1d; // TODO figure out max delta
        }
    
        public int GetHashCode(DateTimeOffset obj)
        {
            return obj.Ticks.GetHashCode();
        }
    }
}
