using System;

namespace ShInvoicing.Models;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "PCS";
    public string HSNSACCode { get; set; } = string.Empty;
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
    public decimal GSTPercent { get; set; }
    public decimal StockQty { get; set; }
    public decimal MinStockQty { get; set; }
}