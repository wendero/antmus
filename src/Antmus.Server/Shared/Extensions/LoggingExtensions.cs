using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace Antmus.Server.Shared.Extensions;

public static class LoggingExtensions
{
    public static void LogDebug(this ILogger logger, Type callerType, object? tracing = null, LogLevel level = LogLevel.Debug, [CallerMemberName] string method = "")
    {
        if (logger.IsEnabled(LogLevel.Trace) && tracing != null)
        {
            logger.LogJson($"{callerType.Name} -> {method}: ", tracing);
        }
        else
        {
            logger.Log(level, $"{callerType.Name} -> {method}");
        }
    }
    public static void LogDebug(this ILogger logger, object caller, object? tracing = null, LogLevel level = LogLevel.Debug, [CallerMemberName] string method = "")
        => LogDebug(logger, caller.GetType(), tracing, level, method);

    public static void LogDebug<T>(this ILogger<T> logger, object? tracing = null, LogLevel level = LogLevel.Debug, [CallerMemberName] string method = "")
        => LogDebug(logger, typeof(T), tracing, level, method);

    public static void LogJson(this ILogger logger, string message, object obj, LogLevel level = LogLevel.Trace)
    {
        try
        {
            if (!logger.IsEnabled(level)) return;

            var json = JsonSerializer.Serialize(obj, JsonHelper.Options.Minified);
            logger.Log(level, $"{message} -> {json}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tried to log Json object but serialization failed. Flow was not interrupted.");
        }
    }
    public static void LogJson(this ILogger logger, object obj, LogLevel level = LogLevel.Trace)
        => LogJson(logger, "Json Object", obj, level);
}
