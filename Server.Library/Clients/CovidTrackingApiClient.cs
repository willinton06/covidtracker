using CovidTracker.Shared.Domains.DailyStats;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZiggyCreatures.Caching.Fusion;

namespace CovidTracker.Server.Library.Clients;

/* Since the data doesn't change (Api stopped updating over a year ago), 
 * we cache it at the api client level, makes it easier for any other services to fetch the already cached data,
 * if the data was dynamic the caching (if any) would happen at least a layer above this
 * 
 * Also no need to interface this since it won't be used in the client at all
 */
public class CovidTrackingApiClient
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IFusionCache _cache;
    private readonly ILogger<CovidTrackingApiClient> _logger;

    public CovidTrackingApiClient(HttpClient http, IFusionCache cache, ILogger<CovidTrackingApiClient> logger)
	{
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SingleDayStateStatsDto[]> GetLastDayStastByStateAsync()
    {
        try
        {
            return await _cache.GetOrSetAsync(nameof(GetLastDayStastByStateAsync),
                (CancellationToken c) => GetLastAsyncNoCacheAsync()!,
                TimeSpan.FromDays(365))
                ?? throw new NullReferenceException("Cache returned null, this should not be possible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Last state stats could not be fetched");

            throw;
        }

        async Task<SingleDayStateStatsDto[]> GetLastAsyncNoCacheAsync()
        {
            const string route = "https://api.covidtracking.com/v1/states/current.json";

            var results = await _http.GetFromJsonAsync<CovidTrackingSingleDayStats[]>(
                route, _serializerOptions);

            if (results is null)
                throw new Exception($"{route} returned no data");

            return results
                .Select(r => r.ToStateDto())
                .ToArray();
        }
    }

    public async Task<ILookup<string, SingleDayStateStatsDto>> GetAllStatesHistoricalAsync()
    {
        try
        {
            return await _cache.GetOrSetAsync(nameof(GetAllStatesHistoricalAsync),
                (CancellationToken c) => GetAllStatesHistoricalNoCacheAsync()!,
                TimeSpan.FromDays(365))
                ?? throw new NullReferenceException("Cache returned null, this should not be possible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "State historical stats could not be fetched");

            throw;
        }

        async Task<ILookup<string, SingleDayStateStatsDto>> GetAllStatesHistoricalNoCacheAsync()
        {
            const string route = "https://api.covidtracking.com/v1/states/daily.json";

            var results = await _http.GetFromJsonAsync<CovidTrackingSingleDayStats[]>(
                route,
                _serializerOptions);

            if (results is null)
                throw new Exception($"{route} returned no data");

            return results
                .Select(r => r.ToStateDto())
                .ToLookup(r => r.Name);
        }
    }

    internal record CovidTrackingSingleDayStats(
        string State,
        [property: JsonConverter(typeof(IntToDateConverter))] DateOnly Date,
        int? Positive,
        int? Negative,
        int? Hospitalized,
        int Total)
    {
        public SingleDayStateStatsDto ToStateDto()
            => new(State, Date, Total, Positive ?? 0, Negative ?? 0, GetHospitalizationRate(Total, Hospitalized));

        static float GetHospitalizationRate(int total, int? hospitalized)
        {
            if (hospitalized is null || total is 0)
                return 0;

            return (float)hospitalized / total;
        }
    };

    // Dates come in a numeric format like 20221114 so we have to write a little parser for that
    internal class IntToDateConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetInt32(out int value))
            {
                var year = value / 10000;
                var month = (value - year * 10000) / 100;
                var day = value - (year * 10000 + month * 100);

                return new(year, month, day);
            }
            return DateOnly.MinValue;
        }

        // This will not be used but it is necessary for the class
        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Year * 10000 + value.Month * 100 + value.Day);
        }
    }
}