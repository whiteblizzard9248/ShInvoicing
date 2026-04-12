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
using ShInvoicing.Data;
using ShInvoicing.Models;
using ShInvoicing.Services;

namespace ShInvoicing.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ExcelService _excelService = new();
    private readonly PdfGenerationService _pdfService = new();
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly AuthService _authService;
    private readonly InventoryService _inventoryService;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly UserRepository _userRepository;
    private readonly DatabaseService _db;

    public Window? Window { get; set; }

    // ===== AUTH & ROLE PROPERTIES =====
    [ObservableProperty]
    private ApplicationUser? currentUser;

    [ObservableProperty]
    private string loginUsername = string.Empty;

    [ObservableProperty]
    private string loginPassword = string.Empty;

    [ObservableProperty]
    private bool isLoginVisible = true;

    [ObservableProperty]
    private bool isCoreUiVisible = false;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    // ===== ROLE-BASED VISIBILITY =====
    public bool IsAuthenticated => CurrentUser != null;
    public bool CanManageUsers => CurrentUser?.RoleName == "Admin";
    public bool CanUseSales => CurrentUser?.RoleName == "Admin" || CurrentUser?.RoleName == "Staff";
    public bool CanUseInventory => CurrentUser?.RoleName == "Admin" || CurrentUser?.RoleName == "Staff";
    public bool CanUsePurchases => CurrentUser?.RoleName == "Admin" || CurrentUser?.RoleName == "Staff";

    // ===== DASHBOARD PROPERTIES =====
    [ObservableProperty]
    private int totalInvoices;

    [ObservableProperty]
    private int totalProducts;

    [ObservableProperty]
    private int totalUsers;

    [ObservableProperty]
    private decimal totalRevenue;

    // ===== INVOICE MANAGEMENT PROPERTIES =====
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

    // ===== PAGINATION PROPERTIES =====
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
    private bool isDashboardVisible = true;

    [ObservableProperty]
    private bool isInventoryVisible = false;

    [ObservableProperty]
    private bool isSalesVisible = false;

    [ObservableProperty]
    private bool isPurchaseVisible = false;

    [ObservableProperty]
    private bool isUserManagementVisible = false;

    [ObservableProperty]
    private bool isInvoiceListVisible = false;

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

    // ===== SALES PROPERTIES =====
    [ObservableProperty]
    private ObservableCollection<InvoiceItem> saleItems = [];

    [ObservableProperty]
    private string saleProductCode = string.Empty;

    [ObservableProperty]
    private int saleQuantity = 1;

    // ===== PURCHASE PROPERTIES =====
    [ObservableProperty]
    private ObservableCollection<InvoiceItem> purchaseItems = [];

    [ObservableProperty]
    private string purchaseProductCode = string.Empty;

    [ObservableProperty]
    private int purchaseQuantity = 1;

    // ===== INVENTORY & POS PROPERTIES =====
    [ObservableProperty]
    private ObservableCollection<Product> products = [];

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private string productCode = string.Empty;

    [ObservableProperty]
    private string productName = string.Empty;

    [ObservableProperty]
    private decimal productPrice = 0;

    [ObservableProperty]
    private string newProductCode = string.Empty;

    [ObservableProperty]
    private string newProductName = string.Empty;

    [ObservableProperty]
    private decimal newProductSaleRate = 0;

    [ObservableProperty]
    private int productStock = 0;

    [ObservableProperty]
    private int productMinStock = 5;

    [ObservableProperty]
    private ObservableCollection<Product> saleCart = [];

    [ObservableProperty]
    private ObservableCollection<Product> purchaseCart = [];

    [ObservableProperty]
    private decimal saleCartTotal = 0;

    [ObservableProperty]
    private decimal saleCartGst = 0;

    [ObservableProperty]
    private decimal purchaseCartTotal = 0;

    [ObservableProperty]
    private string customerName = string.Empty;

    [ObservableProperty]
    private string supplierName = string.Empty;

    // ===== USER MANAGEMENT PROPERTIES =====
    [ObservableProperty]
    private ObservableCollection<ApplicationUser> users = [];

    [ObservableProperty]
    private ApplicationUser? selectedUser;

    [ObservableProperty]
    private string newUserUsername = string.Empty;

    [ObservableProperty]
    private string newUserDisplayName = string.Empty;

    [ObservableProperty]
    private string newUsername = string.Empty;

    [ObservableProperty]
    private string newUserPassword = string.Empty;

    [ObservableProperty]
    private string selectedUserRole = "User";

    // ===== TAX CALCULATION CONSTANTS =====
    private const decimal GST_RATE = 0.18m;  // 18% GST for India

    public string VendorDetailsToggleText => true ? "Hide Vendor Details" : "Show Vendor Details";

    public decimal TotalRate => InvoiceItems.Sum(i => i.Rate);
    public decimal TotalTaxable => InvoiceItems.Sum(i => i.TaxableValue);
    public decimal TotalCGST => InvoiceItems.Sum(i => i.CGSTAmount);
    public decimal TotalSGST => InvoiceItems.Sum(i => i.SGSTAmount);
    public decimal TotalIGST => InvoiceItems.Sum(i => i.IGSTAmount);



    public MainWindowViewModel()
    {
        _logger = LogService.CreateLogger<MainWindowViewModel>();
        _db = new DatabaseService();
        _invoiceRepository = new InvoiceRepository(_db);
        _userRepository = new UserRepository(_db);
        _authService = new AuthService();
        _inventoryService = new InventoryService();

        // Initialize vendor settings
        VendorSettings = new VendorSettings();

        // Check if we are in Avalonia Designer
        if (Design.IsDesignMode)
        {
            LoadDesignData();
        }
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

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();

            try
            {
                foreach (var invoice in loadedInvoices)
                {
                    await _invoiceRepository.UpsertInvoiceAsync(conn, tx, invoice);

                    foreach (var item in invoice.Items ?? Enumerable.Empty<InvoiceItem>())
                    {
                        await _invoiceRepository.InsertItemAsync(conn, tx, item);
                    }
                }

                await _invoiceRepository.SaveVendorSettingsAsync(conn, tx, loadedSettings);

                // ✅ Commit ONLY if everything succeeds
                tx.Commit();
            }
            catch
            {
                // ❌ Rollback on failure
                tx.Rollback();
                throw;
            }

            // 🔹 Load from DB
            var invoices = await _invoiceRepository.GetInvoicesAsync();
            Invoices = new ObservableCollection<Invoice>(invoices);

            SelectedInvoice = Invoices.FirstOrDefault();
            ApplyFilterAndPaging();

            var settingsFromDb = await _invoiceRepository.GetVendorSettingsAsync();
            if (settingsFromDb != null)
                VendorSettings = settingsFromDb;

            StatusMessage = $"Loaded {loadedInvoices.Count} invoices from {filePath}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading Excel file: {ex.Message}";
        }
    }

    private List<Invoice> _allFilteredInvoices = [];

    partial void OnCurrentUserChanged(ApplicationUser? value)
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanUseSales));
        OnPropertyChanged(nameof(CanUseInventory));
        OnPropertyChanged(nameof(CanUsePurchases));
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

    // ===== NAVIGATION COMMANDS =====
    [RelayCommand]
    public void ShowDashboard()
    {
        IsDashboardVisible = true;
        IsInventoryVisible = false;
        IsSalesVisible = false;
        IsPurchaseVisible = false;
        IsUserManagementVisible = false;
        IsInvoiceListVisible = false;
    }

    [RelayCommand]
    public async Task ShowInventoryAsync()
    {
        IsDashboardVisible = false;
        IsInventoryVisible = true;
        IsSalesVisible = false;
        IsPurchaseVisible = false;
        IsUserManagementVisible = false;
        IsInvoiceListVisible = false;

        await LoadProductsAsync();
    }

    [RelayCommand]
    public void ShowSales()
    {
        IsDashboardVisible = false;
        IsInventoryVisible = false;
        IsSalesVisible = true;
        IsPurchaseVisible = false;
        IsUserManagementVisible = false;
        IsInvoiceListVisible = false;
    }

    [RelayCommand]
    public void ShowPurchase()
    {
        IsDashboardVisible = false;
        IsInventoryVisible = false;
        IsSalesVisible = false;
        IsPurchaseVisible = true;
        IsUserManagementVisible = false;
        IsInvoiceListVisible = false;
    }

    [RelayCommand]
    public async Task ShowUsersAsync()
    {
        IsDashboardVisible = false;
        IsInventoryVisible = false;
        IsSalesVisible = false;
        IsPurchaseVisible = false;
        IsUserManagementVisible = true;
        IsInvoiceListVisible = false;

        await LoadUsersAsync();
    }

    [RelayCommand]
    public void ShowInvoices()
    {
        IsDashboardVisible = false;
        IsInventoryVisible = false;
        IsSalesVisible = false;
        IsPurchaseVisible = false;
        IsUserManagementVisible = false;
        IsInvoiceListVisible = true;
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        try
        {
            var user = await _authService.ValidateUserAsync(LoginUsername, LoginPassword);
            if (user != null)
            {
                CurrentUser = user;
                IsLoginVisible = false;
                IsCoreUiVisible = true;
                StatusMessage = $"Welcome, {user.DisplayName}!";
                ShowDashboard();
                await LoadDashboardDataAsync();
            }
            else
            {
                StatusMessage = "Invalid username or password.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Login failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public void Logout()
    {
        CurrentUser = null;
        IsLoginVisible = true;
        IsCoreUiVisible = false;
        StatusMessage = "Logged out successfully.";
        LoginUsername = string.Empty;
        LoginPassword = string.Empty;
    }

    // ===== LOAD METHODS =====
    private async Task LoadDashboardDataAsync()
    {
        try
        {
            var invoices = await _invoiceRepository.GetInvoicesAsync();
            Invoices = new ObservableCollection<Invoice>(invoices);
            TotalInvoices = Invoices.Count;
            TotalRevenue = Invoices.Sum(i => i.GrandTotal);

            var products = await _inventoryService.GetAllProductsAsync();
            Products = new ObservableCollection<Product>(products);
            TotalProducts = Products.Count;

            var users = await _userRepository.GetUsersAsync();
            Users = new ObservableCollection<ApplicationUser>(users);
            TotalUsers = Users.Count;

            SelectedInvoice = Invoices.FirstOrDefault();
            CurrentPage = 1;
            ApplyFilterAndPaging();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load dashboard data: {ex.Message}";
        }
    }
    private async Task LoadProductsAsync()
    {
        try
        {
            var productsList = await _inventoryService.GetAllProductsAsync();
            Products.Clear();
            foreach (var product in productsList)
            {
                Products.Add(product);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load products: {ex.Message}";
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var usersList = await _userRepository.GetUsersAsync();
            Users.Clear();
            foreach (var user in usersList)
            {
                Users.Add(user);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load users: {ex.Message}";
        }
    }

    // ===== PRODUCT COMMANDS =====
    [RelayCommand]
    public async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProductCode) || string.IsNullOrWhiteSpace(NewProductName))
        {
            StatusMessage = "Product code and name are required.";
            return;
        }

        try
        {
            var product = new Product
            {
                Code = NewProductCode.Trim(),
                Name = NewProductName.Trim(),
                SaleRate = NewProductSaleRate,
                GSTPercent = GST_RATE * 100,
                StockQty = 0,
                MinStockQty = 0
            };

            await _inventoryService.AddProductAsync(product);
            Products.Add(product);
            StatusMessage = "Product saved successfully.";

            // Clear form
            NewProductCode = string.Empty;
            NewProductName = string.Empty;
            NewProductSaleRate = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save product: {ex.Message}";
        }
    }

    // ===== SALES COMMANDS =====
    [RelayCommand]
    public async Task AddSaleItemAsync()
    {
        if (string.IsNullOrWhiteSpace(SaleProductCode) || SaleQuantity <= 0)
        {
            StatusMessage = "Valid product code and quantity required.";
            return;
        }

        try
        {
            var product = await _inventoryService.GetProductByCodeAsync(SaleProductCode.Trim());
            if (product == null)
            {
                StatusMessage = "Product not found.";
                return;
            }

            if (product.StockQty < SaleQuantity)
            {
                StatusMessage = "Insufficient stock.";
                return;
            }

            var item = new InvoiceItem
            {
                ProductCode = product.Code,
                ProductName = product.Name,
                Description = product.Name,
                HSNSACCode = product.HSNSACCode,
                Units = product.Unit,
                Quantity = SaleQuantity,
                Rate = product.SaleRate,
                TaxableValue = SaleQuantity * product.SaleRate,
                CGSTPercent = product.GSTPercent / 2,
                CGSTAmount = (SaleQuantity * product.SaleRate * product.GSTPercent / 2) / 100,
                SGSTPercent = product.GSTPercent / 2,
                SGSTAmount = (SaleQuantity * product.SaleRate * product.GSTPercent / 2) / 100,
                IGSTPercent = 0,
                IGSTAmount = 0,
                LineTotal = SaleQuantity * product.SaleRate + (SaleQuantity * product.SaleRate * product.GSTPercent) / 100
            };

            SaleItems.Add(item);
            StatusMessage = "Item added to sale.";

            // Clear form
            SaleProductCode = string.Empty;
            SaleQuantity = 1;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to add item: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task CreateSalesInvoiceAsync()
    {
        if (!SaleItems.Any())
        {
            StatusMessage = "No items in sale.";
            return;
        }

        try
        {
            var invoice = new Invoice
            {
                InvoiceNo = $"S{DateTime.Now:yyyyMMddHHmmss}",
                InvoiceDate = DateTime.Now,
                InvoiceType = InvoiceType.Sales,
                CounterpartyName = "Customer",
                CreatedBy = CurrentUser?.Username,
                SubTotal = SaleItems.Sum(i => i.TaxableValue),
                TaxTotal = SaleItems.Sum(i => i.CGSTAmount + i.SGSTAmount + i.IGSTAmount),
                GrandTotal = SaleItems.Sum(i => i.LineTotal),
                Items = SaleItems.ToList()
            };

            await CreateInvoiceAsync(invoice);

            // Adjust stock
            foreach (var item in SaleItems)
            {
                await _inventoryService.AdjustStockAsync(item.ProductCode!, -item.Quantity, "Sale", invoice.InvoiceNo!, CurrentUser?.Username ?? "System");
            }

            Invoices.Add(invoice);
            StatusMessage = $"Sales invoice {invoice.InvoiceNo} created successfully.";

            // Clear sale
            SaleItems.Clear();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create invoice: {ex.Message}";
        }
    }

    // ===== PURCHASE COMMANDS =====
    [RelayCommand]
    public async Task AddPurchaseItemAsync()
    {
        if (string.IsNullOrWhiteSpace(PurchaseProductCode) || PurchaseQuantity <= 0)
        {
            StatusMessage = "Valid product code and quantity required.";
            return;
        }

        try
        {
            var product = await _inventoryService.GetProductByCodeAsync(PurchaseProductCode.Trim());
            if (product == null)
            {
                StatusMessage = "Product not found.";
                return;
            }

            var item = new InvoiceItem
            {
                ProductCode = product.Code,
                ProductName = product.Name,
                Description = product.Name,
                HSNSACCode = product.HSNSACCode,
                Units = product.Unit,
                Quantity = PurchaseQuantity,
                Rate = product.PurchaseRate,
                TaxableValue = PurchaseQuantity * product.PurchaseRate,
                CGSTPercent = product.GSTPercent / 2,
                CGSTAmount = (PurchaseQuantity * product.PurchaseRate * product.GSTPercent / 2) / 100,
                SGSTPercent = product.GSTPercent / 2,
                SGSTAmount = (PurchaseQuantity * product.PurchaseRate * product.GSTPercent / 2) / 100,
                IGSTPercent = 0,
                IGSTAmount = 0,
                LineTotal = PurchaseQuantity * product.PurchaseRate + (PurchaseQuantity * product.PurchaseRate * product.GSTPercent) / 100
            };

            PurchaseItems.Add(item);
            StatusMessage = "Item added to purchase.";

            // Clear form
            PurchaseProductCode = string.Empty;
            PurchaseQuantity = 1;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to add item: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task CreatePurchaseInvoiceAsync()
    {
        if (!PurchaseItems.Any())
        {
            StatusMessage = "No items in purchase.";
            return;
        }

        try
        {
            var invoice = new Invoice
            {
                InvoiceNo = $"P{DateTime.Now:yyyyMMddHHmmss}",
                InvoiceDate = DateTime.Now,
                InvoiceType = InvoiceType.Purchase,
                CounterpartyName = "Supplier",
                CreatedBy = CurrentUser?.Username,
                SubTotal = PurchaseItems.Sum(i => i.TaxableValue),
                TaxTotal = PurchaseItems.Sum(i => i.CGSTAmount + i.SGSTAmount + i.IGSTAmount),
                GrandTotal = PurchaseItems.Sum(i => i.LineTotal),
                Items = PurchaseItems.ToList()
            };

            await CreateInvoiceAsync(invoice);

            // Adjust stock
            foreach (var item in PurchaseItems)
            {
                await _inventoryService.AdjustStockAsync(item.ProductCode!, item.Quantity, "Purchase", invoice.InvoiceNo!, CurrentUser?.Username ?? "System");
            }

            Invoices.Add(invoice);
            StatusMessage = $"Purchase invoice {invoice.InvoiceNo} created successfully.";

            // Clear purchase
            PurchaseItems.Clear();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create invoice: {ex.Message}";
        }
    }

    // ===== USER MANAGEMENT COMMANDS =====
    [RelayCommand]
    public async Task CreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUserUsername) || string.IsNullOrWhiteSpace(NewUserDisplayName) || string.IsNullOrWhiteSpace(NewUserPassword))
        {
            StatusMessage = "All fields are required.";
            return;
        }

        try
        {
            var user = new ApplicationUser
            {
                Username = NewUserUsername.Trim(),
                DisplayName = NewUserDisplayName.Trim(),
                PasswordHash = PasswordHelper.HashPassword(NewUserPassword),
                RoleId = SelectedUserRole == "Admin" ? 1 : 2,
                RoleName = SelectedUserRole,
                CreatedAt = DateTime.Now
            };

            await _userRepository.CreateOrUpdateAsync(user);
            Users.Add(user);
            StatusMessage = "User created successfully.";

            // Clear form
            NewUserUsername = string.Empty;
            NewUserDisplayName = string.Empty;
            NewUserPassword = string.Empty;
            SelectedUserRole = "User";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create user: {ex.Message}";
        }
    }

    partial void OnSelectedInvoiceChanged(Invoice? value)
    {
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

    private async Task CreateInvoiceAsync(Invoice invoice)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            await _invoiceRepository.UpsertInvoiceAsync(conn, tx, invoice);

            foreach (var item in invoice.Items ?? [])
            {
                item.InvoiceNo = invoice.InvoiceNo;
                await _invoiceRepository.InsertItemAsync(conn, tx, item);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        foreach (var ch in invalidChars)
        {
            fileName = fileName.Replace(ch, '_');
        }

        return fileName;
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
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            : VendorSettings.Address;

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        var sanitizedFileName = SanitizeFileName(SelectedInvoice.InvoiceNo ?? DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var outputPath = Path.Combine(outputFolder, $"{sanitizedFileName}.pdf");
        await _pdfService.GeneratePdfAsync(SelectedInvoice, VendorSettings!, outputPath);
        _logger.LogDebug("PDF generated at: {OutputPath}", outputPath);

        StatusMessage = $"PDF generated: {outputPath}";
    }

    private void LoadDesignData()
    {
        StatusMessage = "Design-Time Preview Active";

        // 1. Populate Vendor Settings (Reflecting rows 1-5 of your Invoices sheet)
        VendorSettings = new VendorSettings
        {
            VendorName = "Test 123",
            VendorAddress = "Address Line 1\nAddress Line 2\nCity, State ZIP",
            GSTIN = "GSTIN1234A1Z5",
            PANNo = "PAN1234A",
            BankAccountNo = "ACCOUNT123456789",
            IFSC = "IFSC0001234",
            MobileNumber = "9876543210",
            Email = "emaail@example.com"
        };

        // 2. Populate Sample Invoice (Reflecting row 8 of your Invoices sheet)
        var mockInvoice = new Invoice
        {
            InvoiceNo = "007/2022-23",
            InvoiceDate = new DateTime(2021, 05, 03),
            CustomerName = "Customer ABC",
            CustomerAddress = "Address Line 3\nAddress Line 5\nCity, State ZIP",
            GrandTotal = 47200,

            // 3. Populate Sample Items (Reflecting the 'Invoice Items' sheet)
            Items =
        [
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
        ]
        };

        // 4. Update Collections
        Invoices.Clear();
        Invoices.Add(mockInvoice);

        ApplyFilterAndPaging();

        // Set as selected to trigger the DataGrid to fill via OnSelectedInvoiceChanged
        SelectedInvoice = mockInvoice;
    }
}
