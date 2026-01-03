using LocalSpaceManager.UI.ViewModels;
using System.Windows;

namespace LocalSpaceManager.UI.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Instead of closing, just hide the window to keep the background service running
        e.Cancel = true;
        this.Hide();
    }
}
