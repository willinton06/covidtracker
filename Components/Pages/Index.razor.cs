using Blazored.LocalStorage;
using CovidTracker.Shared.Common.RenderLocation;
using CovidTracker.Shared.Domains.DailyStats;
using CovidTracker.Shared.States;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace CovidTracker.Components.Pages;

public partial class Index
{
    [Parameter] public string? State { get; set; }

    [Inject] public required NavigationManager Navigation { get; init; }
    [Inject] public required ILocalStorageService LocalStorageService { get; init; }
    [Inject] public required IStatesService StatesService { get; init; }
    [Inject] public required ICurrentRenderLocation RenderLocation { get; init; }
    [Inject] public required IDailyStatsService DailyStatsService { get; init; }
    [Inject] public required ILogger<Index> Logger { get; init; }
    [Inject] public required PersistentComponentState PersistentState { get; init; }

    private const string _persistentStatesKey = "states";
    private const string _localStorageStatesKey = "all-states";
    private const string _localStorageDailyPostFix = "-daily";

    private IQueryable<SingleDayStateStatsDto> _states = Array.Empty<SingleDayStateStatsDto>().AsQueryable();
    private IQueryable<SingleDayStateStatsDto> _daily = Array.Empty<SingleDayStateStatsDto>().AsQueryable();
    private readonly object _emptyModel = new();

    private PersistingComponentStateSubscription _persistingSubscription;
    private DateOnly _selectedDate = new(2021, 03, 07);

    /* The idea here is to have a 3 layer check, first we check if the persitent state contains the data we need,
     * if not, we check local storage, and if that doesn't work either, we fetch it from remote, this should allow
     * the app to work offline as long as you only use states you already requested
     */
    protected override async Task OnInitializedAsync()
    {
        _persistingSubscription =
            PersistentState.RegisterOnPersisting(PersistAsync);

        if (PersistentState.TryTakeFromJson<SingleDayStateStatsDto[]>(
            _persistentStatesKey, out var states))
        {
            _states = states!.AsQueryable();
        }
        else await FetchLastAsync();

        if (string.IsNullOrWhiteSpace(State))
            return;

        if (PersistentState.TryTakeFromJson<SingleDayStateStatsDto[]>(
            State, out var daily))
        {
            _daily = daily!.AsQueryable();
        }
        else await FetchStateDailyAsync(State, false);
    }

    private async Task FetchStateDailyAsync(string state, bool navigate = true)
    {
        if (_daily.Any() && state == State)
            return;

        if ((await LoadStateDailyFromLocalStorageAsync()
            || await FetchFromRemoteAsync())
            && navigate)
            Navigation.NavigateTo($"/{state}", replace: true);

        async Task<bool> LoadStateDailyFromLocalStorageAsync()
        {
            if (RenderLocation.RenderLocation is not RenderLocations.Client)
                return false;

            /* The first use of local storage with will require blazor to expand it's buffer, this can take a sec
             * so we yield just before calling it to avoid blocking as much as possible, this happens because the data stored
             * is fairly large
             */
            await Task.Yield();

            if (await LocalStorageService.GetItemAsync<SingleDayStateStatsDto[]>(state + _localStorageDailyPostFix) is not { } daily)
                return false;

            _daily = daily.AsQueryable();

            return true;
        }

        async Task<bool> FetchFromRemoteAsync()
        {
            if ((await DailyStatsService.GetByStateAsync(state))
                .HasFailed(out var daily, Logger.LogError))
                return false;

            _daily = daily.AsQueryable();

            if (RenderLocation.RenderLocation is RenderLocations.Client)
                await LocalStorageService.SetItemAsync(state + _localStorageDailyPostFix, daily);

            return true;
        }
    }

    private async Task FetchLastAsync()
    {
        if (await LoadAllStatesFromLocalStorageAsync())
            return;

        if ((await StatesService.GetLastAsync())
            .HasSucceeded(out var states, Logger.LogError))
        {
            _states = states.AsQueryable();

            if (RenderLocation.RenderLocation is RenderLocations.Client)
                await LocalStorageService.SetItemAsync(_localStorageStatesKey, states);
        }

        async Task<bool> LoadAllStatesFromLocalStorageAsync()
        {
            if (RenderLocation.RenderLocation is not RenderLocations.Client)
                return false;

            if (await LocalStorageService.GetItemAsync<SingleDayStateStatsDto[]>(_localStorageStatesKey) is not { } states)
                return false;

            _states = states.AsQueryable();

            return true;
        }
    }

    private async Task FetchDayAsync()
    {
        // I decided not to cache this due to time constrains but it is perfectly possible to cache this too
        if ((await StatesService.GetByDayAsync(_selectedDate))
            .HasSucceeded(out var states, Logger.LogError))
        {
            _states = states.AsQueryable();
        }
    }

    private Task PersistAsync()
    {
        PersistentState.PersistAsJson(_persistentStatesKey, _states);

        if (string.IsNullOrWhiteSpace(State) is false)
            PersistentState.PersistAsJson(State, _daily);

        return Task.CompletedTask;
    }

    void IDisposable.Dispose()
    {
        _persistingSubscription.Dispose();
    }
}
