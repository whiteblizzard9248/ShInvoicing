using System;

namespace ShInvoicing.Models;

public class StockTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string TransactionType { get; set; } = string.Empty; // SALE, PURCHASE, ADJUSTMENT
    public string ReferenceNo { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}