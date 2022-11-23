using CovidTracker.Shared.Domains.DailyStats;

namespace CovidTracker.Shared.States;

[Registerable, GenerateApiClient]
public interface IStatesService
{
    [Get]
    Task<Result<SingleDayStateStatsDto[]>> GetLastAsync();

    [Get]
    Task<Result<SingleDayStateStatsDto[]>> GetByDayAsync(DateOnly day);
}