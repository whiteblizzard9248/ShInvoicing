using System;
using System.Collections.Generic;

namespace ShInvoicing.Models;

public class Invoice
{
    public string? InvoiceNo { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerGSTIN { get; set; }
    public string? CustomerState { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TaxTotal { get; set; }

    public List<InvoiceItem> Items { get; set; } = [];
}