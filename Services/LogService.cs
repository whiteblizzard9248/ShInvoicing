using Microsoft.Extensions.Logging;
namespace ShInvoicing.Services;

public static class LogService
{
    public static ILoggerFactory Factory { get; } = LoggerFactory.Create(builder =>
    {
        builder
           .AddConsole()
           .SetMinimumLevel(LogLevel.Debug);
    });

    public static ILogger<T> CreateLogger<T>() => Factory.CreateLogger<T>();
}