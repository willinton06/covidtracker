using CovidTracker.Shared.Common.RenderLocation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CovidTracker.Components.Common;

/* There should be one of these for each project, but we can't make it internal because components
 * in blazor are all public by default
 */

public abstract class WithModule : ComponentBase, IAsyncDisposable
{
    [Inject] public required IJSRuntime JSRuntime { get; init; }
    [Inject] public required ICurrentRenderLocation CurrentRenderLocation { get; init; }


    Lazy<Task<IJSObjectReference>> _module = default!;
    IDisposable? _reference;
    
    public bool CanUseJS { get; private set; }

    protected sealed override void OnInitialized()
    {
        /* With pre rendering enabled this method will be called twice, one in the client and one in the server
         * so the property will clip when it is called on the client
         */
        
        CanUseJS = CurrentRenderLocation.IsJSRuntimeAvailableOnInitialization;

        _module = new(() => JSRuntime.InvokeAsync<IJSObjectReference>(
           "import", GetModulePath()).AsTask());

        OnInitializedAfterModuleSet();
    }

    protected virtual string GetModulePath()
    {
        var type = GetType();

        /* ! should be good here, if the type doesn't have a full name we have bigger problems than this
         * throwing a null exception
         */
        var currentModule = string.Join('/', type.FullName!.Split('.')[2..]);

        return $"./_content/CovidTracker.Components/{currentModule}.razor.js";
    }

    protected virtual void OnInitializedAfterModuleSet() { }

    protected virtual ValueTask BeforeModuleDisposalAsync() => ValueTask.CompletedTask;

    internal protected async ValueTask InvokeVoidModuleMethodAsync(string name, params object?[] parameters)
        => await (await _module.Value).InvokeVoidAsync(name, parameters);

    internal protected async ValueTask<T> InvokeModuleMethodAsync<T>(string name, params object?[] parameters)
        => await (await _module.Value).InvokeAsync<T>(name, parameters);

    /// <summary>
    /// Caches the reference to ensure disposal works properly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this"></param>
    /// <returns></returns>
    internal protected DotNetObjectReference<T> GetObjectReference<T>(T @this) where T : WithModule
        => (DotNetObjectReference<T>)(_reference ??= DotNetObjectReference.Create(@this));

    /// <summary>
    /// Disposes the imported module, unless this is done manually by the inherited class this should still be called
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        await BeforeModuleDisposalAsync();

        if (_module?.IsValueCreated is true)
        {
            var module = await _module.Value;

            await module.DisposeAsync();
        }

        _reference?.Dispose();
    }
}