using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ShInvoicing.Models;
using ShInvoicing.Security;
using ShInvoicing.Utils;

namespace ShInvoicing.Services;

public class LicenseService
{
    public License? Current { get; private set; }
    private readonly string _publicKey;

    public LicenseService()
    {
        _publicKey = PublicKeyProvider.PublicKey;
    }

    public bool Validate()
    {
        var licensePathFile = LicensePaths.GetLicensePath();
        if (!File.Exists(licensePathFile))
            return false;

        var json = File.ReadAllText(licensePathFile);
        var license = JsonSerializer.Deserialize<License>(json);

        if (license == null)
            return false;

        if (!VerifySignature(license))
            return false;

        if (license.Expiry < DateTime.UtcNow)
            return false;

        if (license.MachineId != MachineFingerprint.GetMachineId())
            return false;

        Current = license;

        return true;
    }

    private bool VerifySignature(License lic)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_publicKey);

        var payload = $"{lic.LicenseKey}|{lic.CustomerName}|{lic.MachineId}|{lic.Expiry:O}|{lic.Edition}";
        var data = Encoding.UTF8.GetBytes(payload);
        var sig = Convert.FromBase64String(lic.Signature ?? throw new Exception("Signature is missing"));

        return rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}