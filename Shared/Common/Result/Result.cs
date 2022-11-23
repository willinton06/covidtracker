using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace CovidTracker.Shared.Common.Result;

/* This is a heavily stripped down version of the one I usually use, just kept the basics
 */

public readonly struct Result<T>
{
    public Result(string errorMessage, HttpStatusCode statusCode)
    {
        Message = errorMessage;
        Code = statusCode;
        Value = default;
        Succeeded = false;
    }

    public Result(T value, HttpStatusCode statusCode)
    {
        Message = null;
        Code = statusCode;
        Value = value;
        Succeeded = true;
    }

    private Result(Exception ex)
    {
        Message = ex.Message;
        Value = default;
        Code = HttpStatusCode.InternalServerError;
        Succeeded = false;
    }

    public HttpStatusCode Code { get; init; }
    public string? Message { get; init; }
    public bool Succeeded { get; init; }
    public T? Value { get; init; }

    public static implicit operator Result<T>(T t)
        => new(t, HttpStatusCode.OK);

    // This one is not being used but I kept it to show a possible use of this class
    public static implicit operator Result<T>(Exception e)
        => new(e);

    // Action<string, object?[]>? because that's the overload for ILogger.LogError/ILogger.LogWarning
    public bool HasSucceeded([NotNullWhen(true)] out T? value, Action<string, object?[]>? onError)
    {
        value = Value;
        if (Succeeded is false && TryGetErrorMessage(Message, Code, out var error))
        {
            onError?.Invoke(error, Array.Empty<object>());
        }
        return Succeeded && Value is not null;
    }

    public bool HasFailed([NotNullWhen(false)] out T? value, Action<string, object?[]>? onError)
        => HasSucceeded(out value, onError) is false;

    internal static bool TryGetErrorMessage(string? message, HttpStatusCode code, out string error)
    {
        if (string.IsNullOrWhiteSpace(message) is false)
        {
            error = message;
            return true;
        }
        else if (code != 0)
        {
            error = $"Status code: {code}";
            return true;
        }
        else
        {
            error = string.Empty;
            return false;
        }
    }
}