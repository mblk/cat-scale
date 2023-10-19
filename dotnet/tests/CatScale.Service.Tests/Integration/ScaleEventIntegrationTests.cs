using System.Net;
using CatScale.Service.Model.Cat;
using CatScale.Service.Model.ScaleEvent;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Tests.Utils;
using JsonSubTypes;

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
    public async Task GetAll_Should_ReturnEvents_When_EventsExist()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart1 = t0.AddMinutes(-5);
        var tEnd1 = tStart1.AddSeconds(10);
        var tStart2 = t0.AddMinutes(-3);
        var tEnd2 = tStart2.AddSeconds(10);

        await CreateScaleEvent(new NewScaleEvent(toilet.Id, tStart1, tEnd1,
            Array.Empty<NewStablePhase>(), 22.0d, 50.0d, 100000.0d));
        await CreateScaleEvent(new NewScaleEvent(toilet.Id, tStart2, tEnd2,
            Array.Empty<NewStablePhase>(), 23.0d, 51.0d, 100001.0d));

        var scaleEvents = await GetAllScaleEvents();

        // Newest first
        Assert.Collection(scaleEvents, e =>
        {
            Assert.Equal(tStart2, e.Start, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
            Assert.Equal(tEnd2, e.End, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
            Assert.Empty(e.StablePhases);
            Assert.Equal(23.0d, e.Temperature, 0.001d);
            Assert.Equal(51.0d, e.Humidity, 0.001d);
            Assert.Equal(100001.0d, e.Pressure, 0.001d);
        }, e =>
        {
            Assert.Equal(tStart1, e.Start, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
            Assert.Equal(tEnd1, e.End, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
            Assert.Empty(e.StablePhases);
            Assert.Equal(22.0d, e.Temperature, 0.001d);
            Assert.Equal(50.0d, e.Humidity, 0.001d);
            Assert.Equal(100000.0d, e.Pressure, 0.001d);
        });
    }

    [Fact]
    public async Task GetOne_Should_ReturnNotFound_When_EventDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await GetScaleEvent(1));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetOne_Should_ReturnScaleEvent_When_EventExists()
    {
        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tEnd = tStart.AddSeconds(10);

        await Login();
        var toilet = await CreateToilet("toilet", "desc");
        var apiKey = (await CreateApiKey(null)).Value;
        var createdScaleEvent = await CreateScaleEventWithApiKey(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d),
            apiKey);
        await Logout();

        var returnedScaleEvent = await GetScaleEvent(createdScaleEvent.Id);
        
        Assert.Equal(createdScaleEvent.Id, returnedScaleEvent.Id);
        Assert.Equal(tStart, returnedScaleEvent.Start, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
        Assert.Equal(tEnd, returnedScaleEvent.End, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
        Assert.Empty(returnedScaleEvent.StablePhases);
        Assert.Equal(22.0d, returnedScaleEvent.Temperature, 0.001d);
        Assert.Equal(50.0d, returnedScaleEvent.Humidity, 0.001d);
        Assert.Equal(100000.0d, returnedScaleEvent.Pressure, 0.001d);
    }

    [Fact]
    public async Task Create_Should_CreateEvent_When_ParametersAreValid()
    {
        // TODO don't use 'Now' (in test and implementation) ?

        var t0 = DateTimeOffset.Now;
        var tWeighing = t0.AddDays(-1);
        var tStart = t0.AddMinutes(-5);
        var tPhase1 = tStart.AddSeconds(10);
        var tPhase2 = tStart.AddSeconds(20);
        var tPhase3 = tStart.AddSeconds(30);
        var tEnd = tPhase1.AddSeconds(60);

        await Login();
        var createdToilet = await CreateToilet("toilet", "desc");
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        _ = await CreateCatWeight(createdCat.Id, tWeighing, 5000.0d);

        await CreateScaleEvent(new NewScaleEvent(createdToilet.Id, tStart, tEnd,
            new NewStablePhase[]
            {
                new(tPhase1, 5.0d, 5009.0d),
                new(tPhase2, 10.0d, 5011.0d),
                new(tPhase3, 5.0d, 5013.0d),
            },
            22.0d, 50.0d, 100000.0d));

        var scaleEvents = await GetAllScaleEvents();

        Assert.Collection(scaleEvents, e =>
        {
            Assert.Equal(tStart, e.Start, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
            Assert.Equal(tEnd, e.End, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));

            Assert.Collection(e.StablePhases, sp =>
            {
                Assert.Equal(tPhase1, sp.Time, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
                Assert.Equal(5.0d, sp.Length, 0.001d);
                Assert.Equal(5009.0d, sp.Value, 0.001d);
            }, sp =>
            {
                Assert.Equal(tPhase2, sp.Time, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
                Assert.Equal(10.0d, sp.Length, 0.001d);
                Assert.Equal(5011.0d, sp.Value, 0.001d);
            }, sp =>
            {
                Assert.Equal(tPhase3, sp.Time, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
                Assert.Equal(5.0d, sp.Length, 0.001d);
                Assert.Equal(5013.0d, sp.Value, 0.001d);
            });

            Assert.Equal(22.0d, e.Temperature, 0.001d);
            Assert.Equal(50.0d, e.Humidity, 0.001d);
            Assert.Equal(100000.0d, e.Pressure, 0.001d);

            Assert.Null(e.Cleaning);
            Assert.NotNull(e.Measurement);
            Assert.Equal(createdCat.Id, e.Measurement.CatId);
            Assert.Equal(5011.0d, e.Measurement.CatWeight, 0.001d);
        });
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_MissingParameters()
    {
        await Login();
        ToiletDto createdToilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tPhase1 = tStart.AddSeconds(30); // TODO end of stable phase or start?
        var tEnd = tPhase1.AddSeconds(60);

        async Task requestWithMissingToiletId() => await CreateScaleEvent(
            new NewScaleEvent(null, tStart, tEnd, Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        async Task requestWithMissingStartTime() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, null, tEnd, Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        async Task requestWithMissingEndTime() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, null, Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        async Task requestWithMissingStablePhases() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, tEnd, null,
                22.0d, 50.0d, 100000.0d));

        async Task requestWithMissingTemperature() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, tEnd, Array.Empty<NewStablePhase>(),
                null, 50.0d, 100000.0d));

        async Task requestWithMissingHumidity() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, tEnd, Array.Empty<NewStablePhase>(),
                22.0d, null, 100000.0d));

        async Task requestWithMissingPressure() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, tEnd, Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, null));

        async Task requestWithMissingStablePhaseTimestamp() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, tEnd, new NewStablePhase[]
                {
                    new NewStablePhase(null, 10.0d, 5000.0d)
                },
                22.0d, 50.0d, 100000.0d));

        async Task requestWithMissingStablePhaseLength() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, tEnd, new NewStablePhase[]
                {
                    new NewStablePhase(tPhase1, null, 5000.0d)
                },
                22.0d, 50.0d, 100000.0d));

        async Task requestWithMissingStablePhaseValue() => await CreateScaleEvent(
            new NewScaleEvent(createdToilet.Id, tStart, tEnd, new NewStablePhase[]
                {
                    new NewStablePhase(tPhase1, 10.0d, null)
                },
                22.0d, 50.0d, 100000.0d));

        var requests = new List<Func<Task>>()
        {
            requestWithMissingToiletId,
            requestWithMissingStartTime,
            requestWithMissingEndTime,
            requestWithMissingStablePhases,
            requestWithMissingTemperature,
            requestWithMissingHumidity,
            requestWithMissingPressure,
            requestWithMissingStablePhaseTimestamp,
            requestWithMissingStablePhaseLength,
            requestWithMissingStablePhaseValue,
        };

        foreach (var request in requests)
        {
            var response = await Assert.ThrowsAsync<HttpRequestException>(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task Create_Should_ReturnNotFound_When_ToiletDoesNotExist()
    {
        await Login();

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tEnd = tStart.AddSeconds(60);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(123, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_EventIsTooShort()
    {
        // Minimum time: 5s

        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tEnd = tStart.AddSeconds(1);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_EventIsTooLong()
    {
        // Maximum time: 15min

        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-60);
        var tEnd = tStart.AddMinutes(30);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_EventIsInTheFuture()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(60);
        var tEnd = tStart.AddMinutes(1);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_EventIsTooOld()
    {
        // Max age: 7days

        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddDays(-14);
        var tEnd = tStart.AddMinutes(1);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_StablePhaseStartsBeforeScaleEvent()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tPhaseEnd = tStart.AddSeconds(4); // starts 1s before scale event
        var tEnd = tStart.AddSeconds(10);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                new NewStablePhase[] { new NewStablePhase(tPhaseEnd, 5.0d, 5000.0d) },
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_When_StablePhaseEndsAfterScaleEvent()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tPhaseEnd = tStart.AddSeconds(11); // ends 1s after scale event
        var tEnd = tStart.AddSeconds(10);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                new NewStablePhase[] { new NewStablePhase(tPhaseEnd, 5.0d, 5000.0d) },
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnConflict_When_SameEventAlreadyExists()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tEnd = tStart.AddSeconds(10);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        await request();

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnUnauthorized_When_NotAuthorized()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");
        await Logout();

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tEnd = tStart.AddSeconds(10);

        async Task request() => await CreateScaleEvent(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d));

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnUnauthorized_When_UsingInvalidApiKey()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");
        var apiKey = (await CreateApiKey(null)).Value;
        var invalidApiKey = "abc" + apiKey.Substring(3);
        await Logout();

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tEnd = tStart.AddSeconds(10);

        async Task request() => await CreateScaleEventWithApiKey(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d),
            invalidApiKey);

        var response = await Assert.ThrowsAsync<HttpRequestException>(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_CreateEvent_When_UsingValidApiKey()
    {
        await Login();
        var toilet = await CreateToilet("toilet", "desc");
        var apiKey = (await CreateApiKey(null)).Value;
        await Logout();

        var t0 = DateTimeOffset.Now;
        var tStart = t0.AddMinutes(-5);
        var tEnd = tStart.AddSeconds(10);

        var scaleEvent = await CreateScaleEventWithApiKey(
            new NewScaleEvent(toilet.Id, tStart, tEnd,
                Array.Empty<NewStablePhase>(),
                22.0d, 50.0d, 100000.0d),
            apiKey);

        Assert.Equal(tStart, scaleEvent.Start, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
        Assert.Equal(tEnd, scaleEvent.End, new DateTimeOffsetComparer(TimeSpan.FromSeconds(0.1d)));
    }

    // TODO:
    // GetAll + Paging
    // Cleaning
    // Measurement
    // Classification
}