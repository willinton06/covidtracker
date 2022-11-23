namespace CovidTracker.Shared.Domains.DailyStats;

public record SingleDayStateStatsDto(string Name, DateOnly Day, int Total, int Positive, int Negative, float HospitalizationRate);