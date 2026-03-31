using System;
using System.IO;

public static class LicensePaths
{
    public static string GetLicensePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShInvoicing"
        );

        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "license.json");
    }
}