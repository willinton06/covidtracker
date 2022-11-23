using CovidTracker.Generators.ApiClient;

namespace CovidTracker.Tests.Integration;

/*Usually I would put these tests in different files but there's some context that applies to both so to explain it once I grouped them*/

public class StatesTests : IClassFixture<ApiWebApplicationFactory>
{
    // These Api{ServiceName} classes are generated automatically by the source generators, they always take an HTTP Client as a single parameter
    private readonly ApiStatesService _client;

    public StatesTests(ApiWebApplicationFactory factory)
        => _client = new(factory.CreateClient());
        
    [Fact]
    public async Task LastDayStatsRequestReturnsDataAsync()
    {
        var result = (await _client.GetLastAsync()).Value;

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length >= 50);
    }

    [Theory]
    [InlineData("2020-12-31")]
    [InlineData("2021-1-1")]
    [InlineData("2021-1-2")]
    public async Task StatesByDayReturnsDataAsync(string date)
    {
        var parsed = DateOnly.Parse(date);

        var result = (await _client.GetByDayAsync(parsed)).Value;

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    // Invalid because the last update was over a year ago
    [Fact]
    public async Task StatesByDayReturnsNothingOnInvalidDateAsync()
    {
        var result = (await _client.GetByDayAsync(new DateOnly(2022, 11, 14))).Value;

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

public class DailyStatsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiDailyStatsService _client;

    public DailyStatsTests(ApiWebApplicationFactory factory)
        => _client = new(factory.CreateClient());

    [Theory]
    [InlineData("FL")]
    [InlineData("AZ")]
    [InlineData("CA")]
    public async Task DailyStatsByStateReturnsDataAsync(string state)
    {
        var result = (await _client.GetByStateAsync(state)).Value;

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task DailyStatsByStateReturnsNothingOnInvalidStateAsync()
    {
        var result = (await _client.GetByStateAsync("VZ")).Value;

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
