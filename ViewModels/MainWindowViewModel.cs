using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ShInvoicing.Models;
using ShInvoicing.Services;

namespace ShInvoicing.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ExcelService _excelService = new();
    private readonly PdfGenerationService _pdfService = new();
    private readonly ILogger<MainWindowViewModel> _logger;

    public Window? Window { get; set; }

    [ObservableProperty]
    private ObservableCollection<Invoice> invoices = [];

    [ObservableProperty]
    private ObservableCollection<Invoice> filteredInvoices = [];

    [ObservableProperty]
    private Invoice? selectedInvoice;

    [ObservableProperty]
    private ObservableCollection<InvoiceItem> invoiceItems = [];

    [ObservableProperty]
    private int invoiceItemCount;

    [ObservableProperty]
    private bool isInvoiceItemsEmpty;

    [ObservableProperty]
    private VendorSettings vendorSettings = new();

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int totalPages = 1;

    [ObservableProperty]
    private int itemsPerPage = 5;

    [ObservableProperty]
    private bool isPreviousPageEnabled;

    [ObservableProperty]
    private bool isNextPageEnabled;

    [ObservableProperty]
    private string pageInfo = "Page 1/1";

    [ObservableProperty]
    private string filteredInvoiceCountSummary = string.Empty;

    [ObservableProperty]
    private string invoiceItemsHeader = "Invoice Items (0)";

    [ObservableProperty]
    private int invoiceItemsCurrentPage = 1;

    [ObservableProperty]
    private int invoiceItemsTotalPages = 1;

    [ObservableProperty]
    private int invoiceItemsPerPage = 5;

    [ObservableProperty]
    private bool isInvoiceItemsPreviousEnabled;

    [ObservableProperty]
    private bool isInvoiceItemsNextEnabled;

    [ObservableProperty]
    private string invoiceItemsPageInfo = "Page 1/1";

    [ObservableProperty]
    private ObservableCollection<InvoiceItem> filteredInvoiceItems = [];


    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isVendorDetailsVisible = true;

    public string VendorDetailsToggleText => IsVendorDetailsVisible ? "Hide Vendor Details" : "Show Vendor Details";

    partial void OnIsVendorDetailsVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(VendorDetailsToggleText));
    }

    public decimal TotalRate => InvoiceItems.Sum(i => i.Rate);
    public decimal TotalTaxable => InvoiceItems.Sum(i => i.TaxableValue);
    public decimal TotalCGST => InvoiceItems.Sum(i => i.CGSTAmount);
    public decimal TotalSGST => InvoiceItems.Sum(i => i.SGSTAmount);
    public decimal TotalIGST => InvoiceItems.Sum(i => i.IGSTAmount);

    public MainWindowViewModel()
    {
        _logger = LogService.CreateLogger<MainWindowViewModel>();
        _logger.LogInformation("ViewModel Initialized");
        LoadBrandingSettings();
        // Load Excel from data folder
        if (!Design.IsDesignMode)
        {
            _ = LoadExcelFromDataAsync();
        }
        // 1. Check if we are in the Avalonia Designer
        if (Design.IsDesignMode)
        {
            LoadDesignData();
        }
        // 2. Optional: Load mock data during F5 Debugging so you don't have to pick a file
#if DEBUG
        else if (Invoices.Count == 0)
        {
            LoadDesignData();
        }
#endif
    }

    private async Task LoadExcelFromDataAsync()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "invoices.xlsx");
        await LoadInvoicesFromFileAsync(filePath);
    }

    private async Task LoadInvoicesFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            StatusMessage = $"File not found: {filePath}";
            return;
        }

        try
        {
            var (loadedInvoices, loadedSettings) = await _excelService.LoadInvoicesAsync(filePath);
            Invoices.Clear();
            foreach (var invoice in loadedInvoices)
            {
                Invoices.Add(invoice);
            }

            if (Invoices.Count > 0)
            {
                SelectedInvoice = Invoices[0];
            }

            ApplyFilterAndPaging();

            VendorSettings.VendorName = loadedSettings.VendorName;
            VendorSettings.VendorAddress = loadedSettings.VendorAddress;
            VendorSettings.GSTIN = loadedSettings.GSTIN;
            VendorSettings.PANNo = loadedSettings.PANNo;
            VendorSettings.BankAccountNo = loadedSettings.BankAccountNo;
            VendorSettings.IFSC = loadedSettings.IFSC;
            VendorSettings.MobileNumber = loadedSettings.MobileNumber;
            VendorSettings.Email = loadedSettings.Email;
            VendorSettings.LogoPath = loadedSettings.LogoPath;
            VendorSettings.Address = loadedSettings.Address;

            StatusMessage = $"Loaded {loadedInvoices.Count} invoices from {filePath}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading Excel file: {ex.Message}";
        }
    }

    private List<Invoice> _allFilteredInvoices = new();

    private void LoadBrandingSettings()
    {
        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "branding.json");
        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            VendorSettings = JsonSerializer.Deserialize<VendorSettings>(json) ?? new VendorSettings
            {
                PrimaryColor = "#000000",
                LogoPath = "",
                Address = ""
            };
        }
        else
        {
            VendorSettings = new VendorSettings
            {
                PrimaryColor = "#000000",
                LogoPath = "",
                Address = ""
            };
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplyFilterAndPaging();
    }

    partial void OnCurrentPageChanged(int value)
    {
        ApplyFilterAndPaging();
    }

    partial void OnInvoiceItemCountChanged(int value)
    {
        InvoiceItemsHeader = $"Invoice Items ({value}) {InvoiceItemsPageInfo}";
    }

    partial void OnInvoiceItemsCurrentPageChanged(int value)
    {
        ApplyInvoiceItemsPaging();
        InvoiceItemsHeader = $"Invoice Items ({InvoiceItemCount}) {InvoiceItemsPageInfo}";
    }

    private void ApplyFilterAndPaging()
    {
        var query = SearchQuery?.Trim();
        IEnumerable<Invoice> filtered;

        if (string.IsNullOrWhiteSpace(query))
        {
            filtered = Invoices;
        }
        else
        {
            filtered = Invoices.Where(inv =>
                (!string.IsNullOrWhiteSpace(inv.InvoiceNo) && inv.InvoiceNo.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(inv.CustomerName) && inv.CustomerName.Contains(query, StringComparison.OrdinalIgnoreCase))
            );
        }

        _allFilteredInvoices = filtered.ToList();

        TotalPages = Math.Max(1, (int)Math.Ceiling(_allFilteredInvoices.Count / (double)ItemsPerPage));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        var pageItems = _allFilteredInvoices
            .Skip((CurrentPage - 1) * ItemsPerPage)
            .Take(ItemsPerPage);

        FilteredInvoices = new ObservableCollection<Invoice>(pageItems);

        IsPreviousPageEnabled = CurrentPage > 1;
        IsNextPageEnabled = CurrentPage < TotalPages;
        PageInfo = $"Page {CurrentPage}/{TotalPages}";
        FilteredInvoiceCountSummary = $"Showing {FilteredInvoices.Count} of {_allFilteredInvoices.Count} invoices";
    }

    private void ApplyInvoiceItemsPaging()
    {
        if (InvoiceItems.Count == 0)
        {
            FilteredInvoiceItems.Clear();
            InvoiceItemsTotalPages = 1;
            InvoiceItemsCurrentPage = 1;
            IsInvoiceItemsPreviousEnabled = false;
            IsInvoiceItemsNextEnabled = false;
            InvoiceItemsPageInfo = "Page 1/1";
            return;
        }

        InvoiceItemsTotalPages = Math.Max(1, (int)Math.Ceiling(InvoiceItems.Count / (double)InvoiceItemsPerPage));
        if (InvoiceItemsCurrentPage > InvoiceItemsTotalPages) InvoiceItemsCurrentPage = InvoiceItemsTotalPages;

        var pageItems = InvoiceItems
            .Skip((InvoiceItemsCurrentPage - 1) * InvoiceItemsPerPage)
            .Take(InvoiceItemsPerPage);

        FilteredInvoiceItems = new ObservableCollection<InvoiceItem>(pageItems);

        IsInvoiceItemsPreviousEnabled = InvoiceItemsCurrentPage > 1;
        IsInvoiceItemsNextEnabled = InvoiceItemsCurrentPage < InvoiceItemsTotalPages;
        InvoiceItemsPageInfo = $"Page {InvoiceItemsCurrentPage}/{InvoiceItemsTotalPages}";
    }

    [RelayCommand]
    public void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    [RelayCommand]
    public void NextInvoiceItemsPage()
    {
        if (InvoiceItemsCurrentPage < InvoiceItemsTotalPages)
        {
            InvoiceItemsCurrentPage++;
        }
    }

    [RelayCommand]
    public void PreviousInvoiceItemsPage()
    {
        if (InvoiceItemsCurrentPage > 1)
        {
            InvoiceItemsCurrentPage--;
        }
    }

    partial void OnSelectedInvoiceChanged(Invoice? value)
    {
        _logger.LogDebug("SelectedInvoice changed: {InvoiceNo}", value?.InvoiceNo);

        if (value == null)
        {
            InvoiceItems.Clear();
            FilteredInvoiceItems.Clear();
            InvoiceItemCount = 0;
            IsInvoiceItemsEmpty = true;
            InvoiceItemsCurrentPage = 1;
            return;
        }

        var items = value.Items ?? new List<InvoiceItem>();

        InvoiceItems.Clear();

        foreach (var item in items)
        {
            InvoiceItems.Add(item);
        }

        InvoiceItemCount = InvoiceItems.Count;
        IsInvoiceItemsEmpty = InvoiceItems.Count == 0;
        InvoiceItemsCurrentPage = 1; // Reset to first page
        ApplyInvoiceItemsPaging();

        StatusMessage = $"Loaded {InvoiceItems.Count} items for invoice {value.InvoiceNo}";
    }

    [RelayCommand]
    public async Task LoadExcelFileAsync()
    {
        if (Window?.StorageProvider is not { } provider)
        {
            StatusMessage = "File picker unavailable.";
            return;
        }

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Excel File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Excel Files")
                {
                    Patterns = ["*.xlsx", "*.xls"],
                    MimeTypes = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel"]
                }
            ]
        });

        if (files.Count == 0)
        {
            StatusMessage = "No file selected.";
            return;
        }

        await LoadInvoicesFromFileAsync(files[0].Path.LocalPath);
    }

    [RelayCommand]
    public async Task GeneratePdfAsync()
    {
        if (SelectedInvoice == null)
        {
            StatusMessage = "Please select an invoice.";
            return;
        }

        var outputFolder = string.IsNullOrWhiteSpace(VendorSettings.Address)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : VendorSettings.Address;

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        var outputPath = Path.Combine(outputFolder, $"{SelectedInvoice.InvoiceNo}.pdf");
        await _pdfService.GeneratePdfAsync(SelectedInvoice, VendorSettings!, outputPath);

        StatusMessage = $"PDF generated: {outputPath}";
    }

    [RelayCommand]
    public void ToggleVendorDetails()
    {
        IsVendorDetailsVisible = !IsVendorDetailsVisible;
    }

    [RelayCommand]
    public void SaveVendorSettings()
    {
        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "branding.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(VendorSettings, options);
            File.WriteAllText(settingsPath, json);
            StatusMessage = "Vendor settings saved.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving vendor settings: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task BrowseLogoPathAsync()
    {
        if (Window?.StorageProvider is not { } provider) return;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Vendor Logo",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image Files")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.svg"],
                    MimeTypes = ["image/png", "image/jpeg", "image/svg+xml"]
                }
            ]
        });

        if (files.Count > 0)
        {
            VendorSettings.LogoPath = files[0].Path.LocalPath;
            StatusMessage = "Logo path set.";
        }
    }

    [RelayCommand]
    public async Task BrowseDownloadPathAsync()
    {
        if (Window?.StorageProvider is not { } provider) return;

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Folder"
        });

        if (folders.Count > 0)
        {
            VendorSettings.Address = folders[0].Path.LocalPath;
            StatusMessage = "Download folder set.";
        }
    }


    private void LoadDesignData()
    {
        StatusMessage = "Design-Time Preview Active";

        // 1. Populate Vendor Settings (Reflecting rows 1-5 of your Invoices sheet)
        VendorSettings = new VendorSettings
        {
            VendorName = "UK ADVERTISERS",
            VendorAddress = "BJP Office Complex, JewelRock Road, Durgigudi, Shivamogga 577201",
            GSTIN = "29AAGPU77081ZP",
            PANNo = "AAGPU7708P",
            BankAccountNo = "21430200000004",
            IFSC = "UCB0002143",
            MobileNumber = "9448638133",
            Email = "ukadsmg@gmail.com"
        };

        // 2. Populate Sample Invoice (Reflecting row 8 of your Invoices sheet)
        var mockInvoice = new Invoice
        {
            InvoiceNo = "007/2022-23",
            InvoiceDate = new DateTime(2021, 05, 03),
            CustomerName = "SAHYADRI NARAYANA MULTI SPECIALITY HOSPITAL",
            CustomerAddress = "N T ROAD, HARKERE, Shivamogga 577202",
            GrandTotal = 47200,

            // 3. Populate Sample Items (Reflecting the 'Invoice Items' sheet)
            Items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                InvoiceNo = "007/2022-23",
                Description = "39 CENTER MEDIAN BOARDS ONE MONTH DISPLAY CHARGES (32 LIT AND 7 NONLIT BOARDS)",
                HSNSACCode = "998361",
                Quantity = 39,
                Rate = 40000,
                TaxableValue = 40000,
                CGSTAmount = 3600,
                SGSTAmount = 3600,
                IGSTAmount = 0
            }
        }
        };

        // 4. Update Collections
        Invoices.Clear();
        Invoices.Add(mockInvoice);

        ApplyFilterAndPaging();

        // Set as selected to trigger the DataGrid to fill via OnSelectedInvoiceChanged
        SelectedInvoice = mockInvoice;
    }
}
