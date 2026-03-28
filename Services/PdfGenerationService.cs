using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShInvoicing.Models;

namespace ShInvoicing.Services;

public class PdfGenerationService
{
    public async Task GeneratePdfAsync(Invoice invoice, CustomerSettings settings, string outputPath)
    {
        await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Tax Invoice")
                        .SemiBold().FontSize(20).FontColor(settings.PrimaryColor ?? Colors.Black);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Vendor Info
                            column.Item().Text($"Vendor: {invoice.VendorName}").Bold();
                            column.Item().Text(invoice.VendorAddress);
                            column.Item().Text($"Mobile: {invoice.MobileNumber}");
                            column.Item().Text($"Email: {invoice.Email}");
                            column.Item().Text($"GSTIN: {invoice.GSTIN}");
                            column.Item().Text($"PAN: {invoice.PANNo}");

                            column.Item().PaddingVertical(1, Unit.Centimetre);

                            // Invoice No
                            column.Item().Text($"Invoice No: {invoice.InvoiceNo}").Bold();

                            column.Item().PaddingVertical(0.5f, Unit.Centimetre);

                            // Items Table
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4); // Description
                                    columns.RelativeColumn(1); // HSN
                                    columns.RelativeColumn(1); // Qty
                                    columns.RelativeColumn(1); // Rate
                                    columns.RelativeColumn(1); // Taxable
                                    columns.RelativeColumn(1); // CGST
                                    columns.RelativeColumn(1); // SGST
                                    columns.RelativeColumn(1); // IGST
                                    columns.RelativeColumn(1); // Total
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Description");
                                    header.Cell().Element(CellStyle).Text("HSN");
                                    header.Cell().Element(CellStyle).Text("Qty");
                                    header.Cell().Element(CellStyle).Text("Rate");
                                    header.Cell().Element(CellStyle).Text("Taxable");
                                    header.Cell().Element(CellStyle).Text("CGST");
                                    header.Cell().Element(CellStyle).Text("SGST");
                                    header.Cell().Element(CellStyle).Text("IGST");
                                    header.Cell().Element(CellStyle).Text("Total");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                    }
                                });

                                foreach (var item in invoice.Items)
                                {
                                    table.Cell().Element(CellStyle).Text(item.Description);
                                    table.Cell().Element(CellStyle).Text(item.HSNSACCode);
                                    table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                                    table.Cell().Element(CellStyle).Text(item.Rate.ToString("F2"));
                                    table.Cell().Element(CellStyle).Text(item.TaxableValue.ToString("F2"));
                                    table.Cell().Element(CellStyle).Text(item.CGSTAmount.ToString("F2"));
                                    table.Cell().Element(CellStyle).Text(item.SGSTAmount.ToString("F2"));
                                    table.Cell().Element(CellStyle).Text(item.IGSTAmount.ToString("F2"));
                                    table.Cell().Element(CellStyle).Text((item.TaxableValue + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount).ToString("F2"));

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                    }
                                }
                            });

                            // Bank Details
                            column.Item().PaddingVertical(1, Unit.Centimetre);
                            column.Item().Text("Bank Details:").Bold();
                            column.Item().Text($"A/C No: {invoice.AccountNo}");
                            column.Item().Text($"IFSC: {invoice.IFSC}");
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated by ShInvoicing").FontSize(10);
                        });
                });
            });

            document.GeneratePdf(outputPath);
        });
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.PaddingVertical(5);
    }
}