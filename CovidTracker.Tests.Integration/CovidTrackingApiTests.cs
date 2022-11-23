using CovidTracker.Server.Library.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace CovidTracker.Tests.Integration;

public class CovidTrackingApiTests : IClassFixture<ApiWebApplicationFactory>, IDisposable
{
    private readonly CovidTrackingApiClient _client;
    private readonly IServiceScope _scope;

    public CovidTrackingApiTests(ApiWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _client = _scope.ServiceProvider.GetRequiredService<CovidTrackingApiClient>();
    }

    [Fact]
    public async Task LastDayStatsRequestReturnsDataAsync()
    {
        var result = await _client.GetLastDayStastByStateAsync();

        Assert.NotEmpty(result);
        Assert.True(result.Length >= 50);
    }
    
    [Fact]
    public async Task AllStatesHistoricalRequestReturnsDataAsync()
    {
        var result = await _client.GetAllStatesHistoricalAsync();

        Assert.NotEmpty(result);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}