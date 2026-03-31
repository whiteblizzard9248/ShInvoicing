using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ShInvoicing.Utils;

namespace ShInvoicing.ViewModels;

public class ActivationViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string MachineId => MachineFingerprint.GetMachineId();

    public async Task ImportLicense(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                StatusMessage = "Selected file does not exist.";
                return;
            }

            var dest = GetLicensePath();

            // Ensure directory exists
            var dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.Copy(path, dest, true);

            StatusMessage = "License file imported successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing license: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    private string GetLicensePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShInvoicing");

        return Path.Combine(dir, "license.json");
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}