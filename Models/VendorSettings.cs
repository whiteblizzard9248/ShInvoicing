using CommunityToolkit.Mvvm.ComponentModel;

namespace ShInvoicing.Models;

public partial class VendorSettings : ObservableObject
{
    [ObservableProperty] private string? vendorName;
    [ObservableProperty] private string? vendorAddress;
    [ObservableProperty] private string? gSTIN;
    [ObservableProperty] private string? pANNo;
    [ObservableProperty] private string? bankAccountNo;
    [ObservableProperty] private string? iFSC;
    [ObservableProperty] private string? mobileNumber;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? address;
}