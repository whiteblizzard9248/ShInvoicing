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
                        Units = GetStringValue(row.Cell(4)),
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
                for (int r = 1; r <= 5; r++)
                {
                    var row = invoicesSheet.Row(r);

                    for (int c = 1; c <= 10; c++)
                    {
                        var label = GetStringValue(row.Cell(c));
                        if (string.IsNullOrWhiteSpace(label))
                            continue;

                        var value = GetStringValue(row.Cell(c + 1));

                        var map = new Dictionary<string, Action<string>>
                        {
                            ["Vendor Name"] = v => settings.VendorName = v,
                            ["Vendor Address"] = v => settings.VendorAddress = v,
                            ["Mobile Number"] = v => settings.MobileNumber = v,
                            ["Email"] = v => settings.Email = v,
                            ["GSTIN"] = v => settings.GSTIN = v,
                            ["PAN No"] = v => settings.PANNo = v,
                            ["A/C No"] = v => settings.BankAccountNo = v,
                            ["IFSC"] = v => settings.IFSC = v
                        };

                        if (map.TryGetValue(label, out var setter))
                        {
                            setter(value);
                        }
                    }
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