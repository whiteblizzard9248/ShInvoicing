using System;

namespace ShInvoicing.Models;

public class License
{
    public required string LicenseKey { get; set; }
    public required string CustomerName { get; set; }
    public required DateTime Expiry { get; set; }
    public required string MachineId { get; set; }
    public string? Signature { get; set; }
    public string Edition { get; set; } = "Standard";
}