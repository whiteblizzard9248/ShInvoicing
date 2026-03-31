using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ShInvoicing.Models;

namespace ShInvoicing.Services;

public class ExcelService
{

    public async Task<(List<Invoice> Invoices, VendorSettings Settings)> LoadInvoicesAsync(string filePath)
    {
        var invoices = new List<Invoice>();
        var invoiceItems = new List<InvoiceItem>();
        var settings = new VendorSettings();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);
            workbook.RecalculateAllFormulas();

            // 1. Read Invoice Items sheet (Link by InvoiceNo)
            var itemsSheet = workbook.Worksheet("Invoice Items");
            if (itemsSheet != null)
            {
                foreach (var row in itemsSheet.RowsUsed().Skip(1))
                {
                    var item = new InvoiceItem
                    {
                        InvoiceNo = GetStringValue(row.Cell(1)),
                        Description = GetStringValue(row.Cell(2)),
                        HSNSACCode = GetStringValue(row.Cell(3)),
                        Units = GetIntValue(row.Cell(4)),
                        Quantity = GetIntValue(row.Cell(5)),
                        Rate = GetDecimalValue(row.Cell(6)),
                        TaxableValue = GetDecimalValue(row.Cell(7)),
                        CGSTAmount = GetDecimalValue(row.Cell(9)),
                        SGSTAmount = GetDecimalValue(row.Cell(11)),
                        IGSTAmount = GetDecimalValue(row.Cell(13))
                    };
                    if (!string.IsNullOrWhiteSpace(item.InvoiceNo)) invoiceItems.Add(item);
                }
            }

            // 2. Read Invoices sheet (Vendor info in Rows 1-5, Invoices from Row 8)
            var invoicesSheet = workbook.Worksheet("Invoices");
            if (invoicesSheet != null)
            {
                // Parse Vendor metadata (Rows 1-5)
                foreach (var row in invoicesSheet.Rows(1, 5))
                {
                    var label = GetStringValue(row.Cell(1));
                    int targetCol = label switch
                    {
                        "GSTIN" or "PAN No" => 4,
                        "A/C No" or "IFSC" => 9,
                        _ => 2
                    };

                    var val = GetStringValue(row.Cell(targetCol));
                    _ = label switch
                    {
                        "Vendor Name" => settings.VendorName = val,
                        "Vendor Address" => settings.VendorAddress = val,
                        "Mobile Number" => settings.MobileNumber = val,
                        "Email" => settings.Email = val,
                        "GSTIN" => settings.GSTIN = val,
                        "PAN No" => settings.PANNo = val,
                        "A/C No" => settings.BankAccountNo = val,
                        "IFSC" => settings.IFSC = val,
                        _ => null
                    };
                }

                // Parse Invoice table (Starting from Row 8)
                foreach (var row in invoicesSheet.RowsUsed().Where(r => r.RowNumber() >= 8))
                {
                    var invNo = GetStringValue(row.Cell(1));
                    if (string.IsNullOrWhiteSpace(invNo)) continue;

                    var groupedItems = invoiceItems
    .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceNo)) // avoid null keys
    .GroupBy(i => Normalize(i.InvoiceNo), StringComparer.OrdinalIgnoreCase)
    .ToDictionary(
        g => g.Key,
        g => g.ToList(),
        StringComparer.OrdinalIgnoreCase
    );

                    var key = Normalize(invNo);

                    var invoice = new Invoice
                    {
                        InvoiceNo = invNo,
                        InvoiceDate = row.Cell(2).GetDateTime(),
                        CustomerName = GetStringValue(row.Cell(4)),
                        CustomerAddress = GetStringValue(row.Cell(5)),
                        GrandTotal = GetDecimalValue(row.Cell(12)),
                        // Link items from the list loaded earlier
                        Items = groupedItems.TryGetValue(key, out var items) ? items : []
                    };
                    invoices.Add(invoice);
                }
            }
        });

        return (Invoices: invoices, Settings: settings);
    }
    private static string GetStringValue(IXLCell cell)
    {
        return cell.GetValue<string>().Trim();
    }

    private static int GetIntValue(IXLCell cell)
    {
        if (cell.TryGetValue<int>(out var intValue))
        {
            return intValue;
        }

        if (cell.TryGetValue<double>(out var doubleValue))
        {
            return Convert.ToInt32(Math.Round(doubleValue, MidpointRounding.AwayFromZero));
        }

        var rawText = cell.GetValue<string>();
        return int.TryParse(rawText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedInt)
            ? parsedInt
            : 0;
    }

    private static decimal GetDecimalValue(IXLCell cell)
    {
        if (cell.TryGetValue<decimal>(out var decimalValue))
        {
            return decimalValue;
        }

        if (cell.TryGetValue<double>(out var doubleValue))
        {
            return Convert.ToDecimal(doubleValue);
        }

        var rawText = cell.GetValue<string>();
        return decimal.TryParse(rawText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDecimal)
            ? parsedDecimal
            : 0m;
    }

    private static string Normalize(string? s)
    {
        return string.IsNullOrWhiteSpace(s)
            ? string.Empty
            : s.Trim().Replace("\u00A0", " ");
    }
}