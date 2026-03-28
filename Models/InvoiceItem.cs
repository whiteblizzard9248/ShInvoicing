namespace ShInvoicing.Models;

public class InvoiceItem
{
    public string? InvoiceNo { get; set; }
    public string? Description { get; set; }
    public string? HSNSACCode { get; set; }
    public int Units { get; set; }
    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal CGSTPercent { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTPercent { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTPercent { get; set; }
    public decimal IGSTAmount { get; set; }
}