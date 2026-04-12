using System;
using System.Collections.Generic;

namespace ShInvoicing.Models;

public enum InvoiceType
{
    Sales = 1,
    Purchase = 2,
    Return = 3
}

public class Role
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class Invoice
{
    public string? InvoiceNo { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public InvoiceType InvoiceType { get; set; } = InvoiceType.Sales;
    public string? CounterpartyName { get; set; }
    public string? CounterpartyAddress { get; set; }
    public string? CounterpartyGSTIN { get; set; }
    public string? CounterpartyState { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public bool IsReturn { get; set; }
    public string? Notes { get; set; }

    // Legacy fields for backward compatibility
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerGSTIN { get; set; }
    public string? CustomerState { get; set; }

    public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
