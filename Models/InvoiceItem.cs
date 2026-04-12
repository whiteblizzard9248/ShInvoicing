namespace ShInvoicing.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public string? InvoiceNo { get; set; }
    public int LineItemNumber { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? Description { get; set; }
    public string? HSNSACCode { get; set; }
    public string? Units { get; set; }
    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal CGSTPercent { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTPercent { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTPercent { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal CostPrice { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal MarkupAmount { get; set; }
}
