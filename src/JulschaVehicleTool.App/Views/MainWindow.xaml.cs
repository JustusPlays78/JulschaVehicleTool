using System.Windows;
using System.Windows.Controls;
using JulschaVehicleTool.App.ViewModels;

namespace JulschaVehicleTool.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Julscha Vehicle Tool\nVersion 1.0.0\n\nFiveM Vehicle Resource Builder\n\n\u00a9 2026 Julscha",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel vm && e.NewValue is TreeNodeViewModel node)
        {
            vm.SelectedTreeNode = node;
        }
    }
}
