using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ShInvoicing.ViewModels;
using ShInvoicing.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;

namespace ShInvoicing.Views;

public partial class ActivationWindow : Window
{
    public ActivationWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    private ActivationViewModel VM => DataContext as ActivationViewModel
        ?? throw new Exception("Invalid ViewModel");

    private async void OnCopyMachineId(object? sender, RoutedEventArgs e)
    {
        await TopLevel.GetTopLevel(this)!
            .Clipboard!
            .SetTextAsync(VM.MachineId);
    }

    private async void OnImportLicense(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select License File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON")
                {
                    Patterns = new[] { "*.json" }
                }
            ]
        });

        var file = files.FirstOrDefault();
        if (file == null) return;

        var path = file.TryGetLocalPath();
        if (path == null) return;

        await VM.ImportLicense(path);

        // Revalidate after import
        var service = new LicenseService();
        if (service.Validate())
        {
            VM.StatusMessage = "License activated successfully. Restarting...";

            await Task.Delay(1000);

            // Restart app
            Environment.Exit(0);
        }
        else
        {
            VM.StatusMessage = "Invalid license file.";
        }
    }

    private void OnExit(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }
}