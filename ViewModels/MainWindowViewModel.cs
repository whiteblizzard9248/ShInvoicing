using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using ShInvoicing.Models;
using ShInvoicing.Services;

namespace ShInvoicing.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ExcelService _excelService = new();
    private readonly InvoiceDbService _dbService = new();
    private readonly PdfGenerationService _pdfService = new();

    public Window? Window { get; set; }

    [ObservableProperty]
    private ObservableCollection<Invoice> invoices = new();

    [ObservableProperty]
    private Invoice? selectedInvoice;

    [ObservableProperty]
    private CustomerSettings? customerSettings;

    [ObservableProperty]
    private string? statusMessage;

    public MainWindowViewModel()
    {
        LoadBrandingSettings();
    }

    private void LoadBrandingSettings()
    {
        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "branding.json");
        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            CustomerSettings = JsonSerializer.Deserialize<CustomerSettings>(json) ?? new CustomerSettings
            {
                PrimaryColor = "#000000",
                LogoPath = "",
                Address = ""
            };
        }
        else
        {
            CustomerSettings = new CustomerSettings
            {
                PrimaryColor = "#000000",
                LogoPath = "",
                Address = ""
            };
        }
    }

    [RelayCommand]
    public async Task LoadExcelFileAsync()
    {
        if (Window?.StorageProvider is not { } provider) return;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Excel File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Excel Files")
                {
                    Patterns = ["*.xlsx"],
                    MimeTypes = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
                }
            ]
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var filePath = file.Path.LocalPath;
            try
            {
                var loadedInvoices = await _excelService.LoadInvoicesAsync(filePath);
                Invoices.Clear();
                foreach (var invoice in loadedInvoices)
                {
                    Invoices.Add(invoice);
                }
                StatusMessage = $"Loaded {loadedInvoices.Count} invoices.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading file: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    public async Task GeneratePdfAsync()
    {
        if (SelectedInvoice == null)
        {
            StatusMessage = "Please select an invoice.";
            return;
        }

        // Check if already exists
        if (await _dbService.InvoiceExistsAsync(SelectedInvoice.InvoiceNo!))
        {
            StatusMessage = $"Invoice {SelectedInvoice.InvoiceNo} already exists. Skipping generation.";
            return;
        }

        // Save to DB
        await _dbService.SaveInvoiceAsync(SelectedInvoice);

        // Generate PDF
        var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{SelectedInvoice.InvoiceNo}.pdf");
        await _pdfService.GeneratePdfAsync(SelectedInvoice, CustomerSettings!, outputPath);

        StatusMessage = $"PDF generated: {outputPath}";
    }

    private Window GetWindow()
    {
        // Need to get the window, but in MVVM, perhaps inject or find another way.
        // For simplicity, assume it's available.
        // In real app, use IWindowManager or something.
        return null; // Placeholder
    }
}
