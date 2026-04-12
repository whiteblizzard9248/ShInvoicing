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

public class ApplicationUser
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? PasswordHash { get; set; }
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Unit { get; set; }
    public string? HSNSACCode { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
    public decimal GSTPercent { get; set; }
    public decimal StockQty { get; set; }
    public decimal MinStockQty { get; set; }
}

public class StockTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; }
    public string? TransactionType { get; set; }
    public string? ReferenceNo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Remarks { get; set; }
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

    public List<InvoiceItem> Items { get; set; } = [];
}
