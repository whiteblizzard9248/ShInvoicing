using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ShInvoicing.ViewModels;

namespace ShInvoicing.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        AttachWindowToViewModel();
    }

    private void AttachWindowToViewModel()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Window = this;
        }
    }
}