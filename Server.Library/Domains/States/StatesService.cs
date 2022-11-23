using CovidTracker.Server.Library.Clients;
using CovidTracker.Shared.Domains.DailyStats;
using CovidTracker.Shared.States;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CovidTracker.Server.Library.States;

[GenerateController]
public class StatesService : IStatesService
{
    private readonly CovidTrackingApiClient _covidTrackingApi;
    private readonly ILogger<StatesService> _logger;
    public StatesService(CovidTrackingApiClient covidTrackingApi, ILogger<StatesService> logger)
    {
        _covidTrackingApi = covidTrackingApi;
        _logger = logger;
    }

    public async Task<Result<SingleDayStateStatsDto[]>> GetLastAsync()
    {
        try
        {
            return await _covidTrackingApi.GetLastDayStastByStateAsync();
        }
        catch (Exception ex)
        {
            return new Result<SingleDayStateStatsDto[]>("Remote data could not be fetched", ex switch
            {
                HttpRequestException => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.InternalServerError
            });
        }
    }

    public async Task<Result<SingleDayStateStatsDto[]>> GetByDayAsync(DateOnly day)
    {
        try
        {
            var stateLookup = await _covidTrackingApi.GetAllStatesHistoricalAsync();

            // The data is already cached so doing the filtering each time should not cause any performance bottleneck

            return stateLookup.Select(s => s.FirstOrDefault(d => d.Day == day)).OfType<SingleDayStateStatsDto>().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Day}'s state stats could not be fetched", day);

            return new Result<SingleDayStateStatsDto[]>("Remote data could not be fetched", ex switch
            {
                HttpRequestException => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.InternalServerError
            });
        }
    }
}