using System;
using Avalonia;
using Avalonia.Logging;
using Microsoft.Extensions.Logging;
using ShInvoicing.Services;

namespace ShInvoicing;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .WithDeveloperTools()
            .LogToTrace(LogEventLevel.Debug, LogArea.Binding);
}