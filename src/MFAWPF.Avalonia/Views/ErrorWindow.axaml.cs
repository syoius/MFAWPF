using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MFAWPF.Avalonia.Views;

public partial class ErrorWindow : Window
{
    private readonly bool _isTerminating;

    public static void ShowException(Exception ex, bool isTerminating = false)
    {
        var window = new ErrorWindow(ex, isTerminating);
        window.ShowDialog();
    }

    private ErrorWindow(Exception ex, bool isTerminating)
    {
        InitializeComponent();
        _isTerminating = isTerminating;
        ErrorTextBox.Text = ex.ToString();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current?.Clipboard != null)
        {
            Application.Current.Clipboard.SetTextAsync(ErrorTextBox.Text);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
        if (_isTerminating)
        {
            Environment.Exit(1);
        }
    }
}