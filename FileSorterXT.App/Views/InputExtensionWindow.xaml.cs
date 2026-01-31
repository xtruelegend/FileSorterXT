using System.Windows;
using FileSorterXT.Services;

namespace FileSorterXT.Views;

public partial class InputExtensionWindow : Window
{
    public string? Extension { get; private set; }

    public InputExtensionWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var raw = (ExtBox.Text ?? "").Trim();
        var ext = FileCategorizer.NormalizeExt(raw);

        if (string.IsNullOrWhiteSpace(ext) || ext == ".")
        {
            ErrorText.Text = "Enter a valid extension like .zip";
            return;
        }

        Extension = ext;
        DialogResult = true;
        Close();
    }
}
