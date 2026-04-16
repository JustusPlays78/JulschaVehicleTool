using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private void OnKeyboardShortcutsClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Keyboard Shortcuts\n\n" +
            "Ctrl+N          New Project\n" +
            "Ctrl+O          Open Project\n" +
            "Ctrl+S          Save Project\n" +
            "Ctrl+Shift+S    Save Project As\n\n" +
            "Delete          Delete selected vehicle\n" +
            "F2              Rename selected item\n\n" +
            "Alt+F4          Exit",
            "Keyboard Shortcuts",
            MessageBoxButton.OK,
            MessageBoxImage.None);
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        vm.SelectedTreeNode = e.NewValue as TreeNodeViewModel;
    }

    // ── Rename inline editing ──

    private void OnRenameBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    private void OnRenameBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is TreeNodeViewModel node && node.IsEditing)
        {
            CommitRename(node, tb.Text);
        }
    }

    private void OnRenameBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || tb.DataContext is not TreeNodeViewModel node) return;

        if (e.Key == Key.Enter)
        {
            CommitRename(node, tb.Text);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (DataContext is MainWindowViewModel vm)
                vm.CancelRename(node);
            e.Handled = true;
        }
    }

    private void CommitRename(TreeNodeViewModel node, string newName)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.CommitRename(node, newName);
    }

    private void OnVehicleCheckBoxClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // Prevent click from bubbling to TreeViewItem selection
    }
}
