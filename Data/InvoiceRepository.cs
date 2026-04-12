using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using ShInvoicing.Models;

namespace ShInvoicing.Data;

public class InvoiceRepository
{
    private readonly DatabaseService _db;

    public InvoiceRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task UpsertInvoiceAsync(SqliteConnection conn, SqliteTransaction tx, Invoice invoice)
    {
        var sql = @"
INSERT INTO Invoices (
    InvoiceNo, InvoiceDate, InvoiceType, CounterpartyName, CounterpartyAddress,
    CounterpartyGSTIN, CounterpartyState, CreatedBy, CreatedAt, ModifiedAt, ModifiedBy,
    CustomerName, CustomerAddress, CustomerGSTIN, CustomerState
)
VALUES (
    @InvoiceNo, @InvoiceDate, @InvoiceType, @CounterpartyName, @CounterpartyAddress,
    @CounterpartyGSTIN, @CounterpartyState, @CreatedBy, @CreatedAt, @ModifiedAt, @ModifiedBy,
    @CustomerName, @CustomerAddress, @CustomerGSTIN, @CustomerState
)
ON CONFLICT(InvoiceNo) DO UPDATE SET
    InvoiceDate = excluded.InvoiceDate,
    InvoiceType = excluded.InvoiceType,
    CounterpartyName = excluded.CounterpartyName,
    CounterpartyAddress = excluded.CounterpartyAddress,
    CounterpartyGSTIN = excluded.CounterpartyGSTIN,
    CounterpartyState = excluded.CounterpartyState,
    CreatedBy = excluded.CreatedBy,
    CreatedAt = excluded.CreatedAt,
    ModifiedAt = excluded.ModifiedAt,
    ModifiedBy = excluded.ModifiedBy,
    CustomerName = excluded.CustomerName,
    CustomerAddress = excluded.CustomerAddress,
    CustomerGSTIN = excluded.CustomerGSTIN,
    CustomerState = excluded.CustomerState;
";
        await conn.ExecuteAsync(sql, invoice, tx);
    }

    public async Task InsertItemAsync(SqliteConnection conn, SqliteTransaction tx, InvoiceItem item)
    {
        var sql = @"
INSERT INTO InvoiceItems (
    InvoiceNo, ProductCode, ProductName, Description, HSNSACCode, Units,
    Quantity, Rate, TaxableValue, CGSTPercent, CGSTAmount,
    SGSTPercent, SGSTAmount, IGSTPercent, IGSTAmount, LineTotal
)
VALUES (
    @InvoiceNo, @ProductCode, @ProductName, @Description, @HSNSACCode, @Units,
    @Quantity, @Rate, @TaxableValue, @CGSTPercent, @CGSTAmount,
    @SGSTPercent, @SGSTAmount, @IGSTPercent, @IGSTAmount, @LineTotal
);
";
        await conn.ExecuteAsync(sql, item, tx);
    }

    public async Task<List<Invoice>> GetInvoicesAsync()
    {
        using var conn = _db.GetConnection();
        var invoices = (await conn.QueryAsync<Invoice>("SELECT * FROM Invoices")).ToList();

        foreach (var invoice in invoices)
        {
            var items = await conn.QueryAsync<InvoiceItem>(
                "SELECT * FROM InvoiceItems WHERE InvoiceNo = @InvoiceNo",
                new { invoice.InvoiceNo });
            invoice.Items = items.ToList();
        }

        return invoices;
    }

    public async Task<List<Invoice>> GetInvoicesByTypeAsync(int invoiceType)
    {
        using var conn = _db.GetConnection();
        var invoices = (await conn.QueryAsync<Invoice>(
            "SELECT * FROM Invoices WHERE InvoiceType = @InvoiceType",
            new { InvoiceType = invoiceType })).ToList();

        foreach (var invoice in invoices)
        {
            invoice.Items = (await conn.QueryAsync<InvoiceItem>(
                "SELECT * FROM InvoiceItems WHERE InvoiceNo = @InvoiceNo",
                new { invoice.InvoiceNo })).ToList();
        }

        return invoices;
    }

    public async Task DeleteInvoiceAsync(string invoiceNo)
    {
        using var conn = _db.GetConnection();
        await conn.ExecuteAsync("DELETE FROM InvoiceItems WHERE InvoiceNo = @InvoiceNo", new { invoiceNo });
        await conn.ExecuteAsync("DELETE FROM Invoices WHERE InvoiceNo = @InvoiceNo", new { invoiceNo });
    }

    public async Task SaveVendorSettingsAsync(SqliteConnection conn, SqliteTransaction tx, VendorSettings settings)
    {
        var sql = @"
INSERT INTO VendorSettings (Id, VendorName, VendorAddress, GSTIN, PANNo, BankAccountNo, IFSC, MobileNumber, Email)
VALUES (1, @VendorName, @VendorAddress, @GSTIN, @PANNo, @BankAccountNo, @IFSC, @MobileNumber, @Email)
ON CONFLICT(Id) DO UPDATE SET
    VendorName = excluded.VendorName,
    VendorAddress = excluded.VendorAddress,
    GSTIN = excluded.GSTIN,
    PANNo = excluded.PANNo,
    BankAccountNo = excluded.BankAccountNo,
    IFSC = excluded.IFSC,
    MobileNumber = excluded.MobileNumber,
    Email = excluded.Email;
";
        await conn.ExecuteAsync(sql, settings, tx);
    }

    public async Task<VendorSettings?> GetVendorSettingsAsync()
    {
        using var conn = _db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<VendorSettings>(
            "SELECT * FROM VendorSettings WHERE Id = 1");
    }
}
