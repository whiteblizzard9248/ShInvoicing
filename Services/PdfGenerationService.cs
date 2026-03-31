using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShInvoicing.Models;

namespace ShInvoicing.Services;

public class PdfGenerationService
{
    public async Task GeneratePdfAsync(Invoice invoice, VendorSettings settings, string outputPath)
    {
        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    // 🔴 Print-accurate margins
                    page.MarginTop(20);
                    page.MarginBottom(20);
                    page.MarginLeft(25);
                    page.MarginRight(15);

                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(e => BuildHeader(e, settings));

                    page.Content().Column(col =>
                    {
                        col.Spacing(2);

                        col.Item().Element(e => BuildMetaSection(e, invoice));
                        col.Item().Element(e => BuildItemsTable(e, invoice));
                        col.Item().Element(e => BuildTotals(e, invoice));
                        col.Item().Element(e => BuildBankAndSignature(e, settings));
                    });

                    page.Footer().AlignCenter().Text(" ");
                });
            }).GeneratePdf(outputPath);
        });
    }

    // ================= HEADER =================
    private static void BuildHeader(IContainer container, VendorSettings s)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("TAX INVOICE").Bold().FontSize(14);

            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(s.VendorName).FontSize(18).Bold();
                    c.Item().Text(s.VendorAddress);
                    c.Item().Text($"Mob : {s.MobileNumber}   Email : {s.Email}");
                });

                row.ConstantItem(200).Border(1).Padding(5).Column(c =>
                {
                    c.Item().Text($"GSTIN : {s.GSTIN}").Bold();
                    c.Item().Text($"PAN No : {s.PANNo}");
                });
            });
        });
    }

    // ================= META =================
    private static void BuildMetaSection(IContainer container, Invoice inv)
    {
        container.Border(1).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(3);
                c.RelativeColumn(3);
            });

            table.Cell().Padding(5).Column(c =>
            {
                c.Item().Text("Bill To:").Bold();
                c.Item().Text(inv.CustomerName);
                c.Item().Text(inv.CustomerAddress);
            });

            table.Cell().Padding(5).Column(c =>
            {
                c.Item().Text("Place of Supply:").Bold();
                c.Item().Text("SHIMOGA");
                c.Item().Text("KARNATAKA");
            });

            table.Cell().Padding(5).Column(c =>
            {
                c.Item().Text($"Invoice No : {inv.InvoiceNo}");
                c.Item().Text($"Date : {inv.InvoiceDate:dd.MM.yyyy}");
            });
        });
    }

    // ================= ITEMS (MULTI-PAGE SAFE) =================
    private static void BuildItemsTable(IContainer container, Invoice inv)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(5); // Description
                c.RelativeColumn(2); // HSN
                c.RelativeColumn(1); // Units
                c.RelativeColumn(1); // Qty
                c.RelativeColumn(2); // Rate
                c.RelativeColumn(2); // Amount
            });

            // 🔁 Header repeats on every page
            table.Header(header =>
            {
                HeaderCell(header, "Description of Goods / Service");
                HeaderCell(header, "HSN/SAC");
                HeaderCell(header, "Units");
                HeaderCell(header, "Qty");
                HeaderCell(header, "Rate per sq.ft");
                HeaderCell(header, "Amount");
            });

            foreach (var item in inv.Items)
            {
                BodyCell(table, item.Description ?? "");
                BodyCell(table, item.HSNSACCode ?? "");
                BodyCell(table, item.Units.ToString());
                BodyCell(table, item.Quantity.ToString());
                BodyCell(table, $"₹ {item.Rate:N2}");
                BodyCell(table, $"₹ {item.TaxableValue:N2}");
            }
        });
    }

    // ================= TOTALS =================
    private static void BuildTotals(IContainer container, Invoice inv)
    {
        var taxable = inv.Items.Sum(x => x.TaxableValue);
        var cgst = inv.Items.Sum(x => x.CGSTAmount);
        var sgst = inv.Items.Sum(x => x.SGSTAmount);
        var igst = inv.Items.Sum(x => x.IGSTAmount);
        var total = taxable + cgst + sgst + igst;

        container.Border(1).Padding(5).Column(col =>
        {
            col.Item().AlignRight().Text($"Total Taxable Value : {taxable:N2}");
            col.Item().AlignRight().Text($"Add CGST 9% : {cgst:N2}");
            col.Item().AlignRight().Text($"Add SGST 9% : {sgst:N2}");
            col.Item().AlignRight().Text($"Add IGST : {igst:N2}");

            col.Item().AlignRight().Text($"Total : {total:N2}").Bold();

            col.Item().PaddingTop(5)
                .Text($"Amount (in words): {NumberToWords((int)total)} only");
        });
    }

    // ================= FOOTER =================
    private static void BuildBankAndSignature(IContainer container, VendorSettings s)
    {
        container.Border(1).Padding(5).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Bank A/c details:").Bold();
                col.Item().Text($"A/C NO: {s.BankAccountNo}");
                col.Item().Text($"IFSC: {s.IFSC}");
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"For {s.VendorName}");
                col.Item().PaddingTop(25).Text("(Authorized Signatory)");
            });
        });
    }

    // ================= HELPERS =================
    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell().Border(1).Padding(3).Text(text).Bold();
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell().Border(1).Padding(3).Text(text);
    }

    private static string NumberToWords(int number)
    {
        if (number == 0) return "zero";
        return number.ToString(); // 🔧 Replace with full converter if needed
    }
}