namespace CovidTracker.Shared.Common.Extensions;

// These save a lot of time on Blazor

public static class BoolExtensions
{
    public static string If(this bool condition, string @true, string @false) => condition ? @true : @false;
    public static string IfTrue(this bool condition, string @true) => condition ? @true : string.Empty;
    public static string IfFalse(this bool condition, string @false) => condition ? string.Empty : @false;
}
