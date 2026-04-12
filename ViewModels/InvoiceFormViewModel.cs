using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShInvoicing.Data;
using ShInvoicing.Models;

namespace ShInvoicing.ViewModels;

public partial class InvoiceFormViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly InvoiceRepository _repo;

    public InvoiceFormViewModel()
    {
        _db = new DatabaseService();
        _repo = new InvoiceRepository(_db);
    }

    [ObservableProperty] private string invoiceNo = "";
    [ObservableProperty] private DateTime invoiceDate = DateTime.Now;
    [ObservableProperty] private string customerName = "";
    [ObservableProperty] private string customerAddress = "";

    [ObservableProperty]
    private ObservableCollection<InvoiceItem> items = new();

    [ObservableProperty] private string statusMessage = "";

    // 👉 Add item
    [RelayCommand]
    void AddItem()
    {
        Items.Add(new InvoiceItem
        {
            Description = "",
            Quantity = 1,
            Rate = 0
        });
    }

    // 👉 Remove item
    [RelayCommand]
    void RemoveItem(InvoiceItem item)
    {
        Items.Remove(item);
    }

    // 👉 Save invoice
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(InvoiceNo))
        {
            StatusMessage = "Invoice No is required";
            return;
        }

        var invoice = new Invoice
        {
            InvoiceNo = InvoiceNo,
            InvoiceDate = InvoiceDate,
            CustomerName = CustomerName,
            CustomerAddress = CustomerAddress,
            Items = Items.ToList()
        };

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            await _repo.UpsertInvoiceAsync(conn, tx, invoice);

            foreach (var item in invoice.Items)
            {
                item.InvoiceNo = invoice.InvoiceNo;
                await _repo.InsertItemAsync(conn, tx, item);
            }

            tx.Commit();
            StatusMessage = "Saved successfully";
        }
        catch (Exception ex)
        {
            tx.Rollback();
            StatusMessage = ex.Message;
        }
    }
}