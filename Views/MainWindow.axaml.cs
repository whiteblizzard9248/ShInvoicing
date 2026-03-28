using Avalonia.Controls;
using ShInvoicing.ViewModels;

namespace ShInvoicing.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Window = this;
        }
    }
}