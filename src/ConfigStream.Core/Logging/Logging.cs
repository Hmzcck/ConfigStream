using Microsoft.Extensions.Logging;

namespace ConfigStream.Core.Logging;

public static class Logging
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole().AddDebug();
    });

    public static ILogger<T> CreateLogger<T>() => _loggerFactory.CreateLogger<T>();

    public static ILogger CreateLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);
}