using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace ShInvoicing.Utils;

public static class MachineFingerprint
{
    public static string GetMachineId()
    {
        var raw = string.Join("|",
            Environment.MachineName,
            Environment.UserName,
            GetMacAddress(),
            GetDiskId()
        );

        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(raw))
        );
    }

    private static string GetMacAddress()
        => NetworkInterface
            .GetAllNetworkInterfaces()
            .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up)?
            .GetPhysicalAddress()
            .ToString() ?? "NA";

    private static string GetDiskId()
        => Environment.OSVersion.Platform.ToString();
}