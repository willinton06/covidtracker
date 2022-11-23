namespace CovidTracker.Shared.Common.RenderLocation;

/* The idea behind this is simple, we need a clean way to know if trying to use IJSRuntime will work
 * We register one in the server and one in the client, and then inject it as needed
 */

public interface ICurrentRenderLocation
{
    RenderLocations RenderLocation { get; }
    /// <summary>
    /// JSRuntime works in both Blazor Server and WASM, but it's only available on initialization on WASM
    /// </summary>
    bool IsJSRuntimeAvailableOnInitialization => RenderLocation is RenderLocations.Client;

    bool IsInteractiveOnStartup { get; }
}

public class PreRenderServerRenderLocation : ICurrentRenderLocation
{
    public RenderLocations RenderLocation => RenderLocations.Server;

    public bool IsInteractiveOnStartup => false;
}

public class ServerRenderLocation : ICurrentRenderLocation
{
    public RenderLocations RenderLocation => RenderLocations.Server;

    public bool IsInteractiveOnStartup => true;
}

public class ClientRenderLocation : ICurrentRenderLocation
{
    public RenderLocations RenderLocation => RenderLocations.Client;

    public bool IsInteractiveOnStartup => true;
}

public enum RenderLocations
{
    None,
    Client,
    Server
}
