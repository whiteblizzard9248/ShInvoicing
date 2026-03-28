using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ShInvoicing.Models;

namespace ShInvoicing.Services;

public class ExcelService
{
    public async Task<List<Invoice>> LoadInvoicesAsync(string filePath)
    {
        var invoices = new List<Invoice>();
        var invoiceItems = new List<InvoiceItem>();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);

            // Read Invoice Items sheet
            var itemsSheet = workbook.Worksheet("Invoice Items");
            if (itemsSheet != null)
            {
                foreach (var row in itemsSheet.RowsUsed().Skip(1)) // Skip header
                {
                    var item = new InvoiceItem
                    {
                        InvoiceNo = row.Cell(1).GetValue<string>(),
                        Description = row.Cell(2).GetValue<string>(),
                        HSNSACCode = row.Cell(3).GetValue<string>(),
                        Units = row.Cell(4).GetValue<int>(),
                        Quantity = row.Cell(5).GetValue<int>(),
                        Rate = row.Cell(6).GetValue<decimal>(),
                        TaxableValue = row.Cell(7).GetValue<decimal>(),
                        CGSTPercent = row.Cell(8).GetValue<decimal>(),
                        CGSTAmount = row.Cell(9).GetValue<decimal>(),
                        SGSTPercent = row.Cell(10).GetValue<decimal>(),
                        SGSTAmount = row.Cell(11).GetValue<decimal>(),
                        IGSTPercent = row.Cell(12).GetValue<decimal>(),
                        IGSTAmount = row.Cell(13).GetValue<decimal>()
                    };
                    if (!string.IsNullOrEmpty(item.InvoiceNo))
                    {
                        invoiceItems.Add(item);
                    }
                }
            }

            // Read Invoices sheet for vendor info
            var invoicesSheet = workbook.Worksheet("Invoices");
            string vendorName = "", vendorAddress = "", mobile = "", email = "", gstin = "", pan = "", accountNo = "", ifsc = "";
            if (invoicesSheet != null)
            {
                foreach (var row in invoicesSheet.RowsUsed())
                {
                    var firstCell = row.Cell(1).GetValue<string>();
                    if (firstCell == "Vendor Name")
                    {
                        vendorName = row.Cell(2).GetValue<string>();
                    }
                    else if (firstCell == "Vendor Address")
                    {
                        vendorAddress = row.Cell(2).GetValue<string>();
                    }
                    else if (firstCell == "Mobile Number")
                    {
                        mobile = row.Cell(2).GetValue<string>();
                    }
                    else if (firstCell == "Email")
                    {
                        email = row.Cell(2).GetValue<string>();
                    }
                    else if (firstCell == "GSTIN")
                    {
                        gstin = row.Cell(4).GetValue<string>();
                    }
                    else if (firstCell == "PAN No")
                    {
                        pan = row.Cell(4).GetValue<string>();
                    }
                    else if (firstCell == "A/C No")
                    {
                        accountNo = row.Cell(9).GetValue<string>();
                    }
                    else if (firstCell == "IFSC")
                    {
                        ifsc = row.Cell(9).GetValue<string>();
                    }
                }
            }

            // Group items by InvoiceNo
            var groupedItems = invoiceItems.GroupBy(i => i.InvoiceNo);
            foreach (var group in groupedItems)
            {
                var invoice = new Invoice
                {
                    InvoiceNo = group.Key,
                    VendorName = vendorName,
                    VendorAddress = vendorAddress,
                    MobileNumber = mobile,
                    Email = email,
                    GSTIN = gstin,
                    PANNo = pan,
                    AccountNo = accountNo,
                    IFSC = ifsc,
                    Items = group.ToList()
                };
                invoices.Add(invoice);
            }
        });

        return invoices;
    }
}