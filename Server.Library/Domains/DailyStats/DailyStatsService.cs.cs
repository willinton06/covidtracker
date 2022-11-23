using CovidTracker.Server.Library.Clients;
using CovidTracker.Shared.Domains.DailyStats;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CovidTracker.Server.Library.Domains.DailyStats;

/* The GenerateController attribute is very useful, it works alongside the other generators to reduce
 * boilerplate to a minium
 */

[GenerateController]
public class DailyStatsService : IDailyStatsService
{
    private readonly CovidTrackingApiClient _covidTrackingApi;
    private readonly ILogger<DailyStatsService> _logger;

    public DailyStatsService(CovidTrackingApiClient covidTrackingApi, ILogger<DailyStatsService> logger)
    {
        _covidTrackingApi = covidTrackingApi;
        _logger = logger;
    }

    public async Task<Result<SingleDayStateStatsDto[]>> GetByStateAsync(string state)
    {
        try
        {
            var stateLookup = await _covidTrackingApi.GetAllStatesHistoricalAsync();

            return stateLookup[state].ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{StateName}'s daily stats could not be fetched", state);

            /* If we get a HttpRequestException is because the api is down and the request failed so we push this
             * knowledge to the client
             */

            return new Result<SingleDayStateStatsDto[]>("Remote data could not be fetched", ex switch
            {
                HttpRequestException => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.InternalServerError
            });
        }
    }
}