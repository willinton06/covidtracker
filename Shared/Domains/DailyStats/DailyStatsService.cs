namespace CovidTracker.Shared.Domains.DailyStats;

/* These attributes are source generated, they allow me to do this 1 interface 
 * and have the controllers and api clients generated automatically, 
 * this reduces boilerplate and it's less error prone
 * 
 * One important thing to notice here is, since the clients only consume the *interface* 
 * and not the generated classes, pre rendering works very well out of the gate
 */

[Registerable, GenerateApiClient]
public interface IDailyStatsService
{
    [Get]
    Task<Result<SingleDayStateStatsDto[]>> GetByStateAsync(string state);
}