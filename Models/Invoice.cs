using System.Collections.Generic;

namespace ShInvoicing.Models;

public class Invoice
{
    public string? InvoiceNo { get; set; }
    public string? VendorName { get; set; }
    public string? VendorAddress { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? GSTIN { get; set; }
    public string? PANNo { get; set; }
    public string? AccountNo { get; set; }
    public string? IFSC { get; set; }
    public List<InvoiceItem> Items { get; set; } = new();
}