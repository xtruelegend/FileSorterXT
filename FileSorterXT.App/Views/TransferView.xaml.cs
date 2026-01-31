using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FileSorterXT.Services;

namespace FileSorterXT.Views;

public partial class TransferView : System.Windows.Controls.UserControl
{
    private CancellationTokenSource? _cts;
    private List<string> _files = new();
    private long _totalBytes;

    public TransferView()
    {
        InitializeComponent();
        StatusText.Text = "Pick a source folder and a destination folder.";
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var owner = Window.GetWindow(this);
            var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

            var path = FolderPicker.PickFolder(hwnd, "Pick the folder you want to transfer");
            if (!string.IsNullOrWhiteSpace(path))
            {
                SourceFolderBox.Text = path.Trim();
                StatusText.Text = "Source selected. Now pick a destination.";
                RunButton.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Pick source failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BrowseDest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var owner = Window.GetWindow(this);
            var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

            var path = FolderPicker.PickFolder(hwnd, "Pick the destination folder (drive or folder)");
            if (!string.IsNullOrWhiteSpace(path))
            {
                DestFolderBox.Text = path.Trim();
                StatusText.Text = "Destination selected. Click Preview.";
                RunButton.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Pick destination failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GuardInputs();

            _files.Clear();
            _totalBytes = 0;

            var src = SourceFolderBox.Text.Trim();
            var destRoot = DestFolderBox.Text.Trim();

            var warning = TransferGuards.GuardRiskySource(src);
            if (!string.IsNullOrWhiteSpace(warning))
            {
                var res = System.Windows.MessageBox.Show(
                    warning + "\n\nDo you still want to preview this transfer?",
                    "Potentially unsafe folder",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (res != MessageBoxResult.Yes)
                {
                    StatusText.Text = "Preview canceled.";
                    return;
                }
            }

            foreach (var f in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
            {
                _files.Add(f);
                try { _totalBytes += new FileInfo(f).Length; } catch { }
            }

            var mode = TransferModeCombo.SelectedIndex == 1 ? "Copy" : "Move";
            SummaryText.Text = $"Found {_files.Count} file(s). Total size: {FormatBytes(_totalBytes)}. Mode: {mode}.";
            StatusText.Text = "Preview complete. Click Transfer to run.";
            RunButton.IsEnabled = _files.Count > 0;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Preview failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GuardInputs();
            if (_files.Count == 0) Preview_Click(sender, e);
            if (_files.Count == 0) throw new Exception("Nothing to transfer.");

            var src = SourceFolderBox.Text.Trim();
            var destRoot = DestFolderBox.Text.Trim();
            var keepName = KeepNameCheck.IsChecked == true;

            var folderName = new DirectoryInfo(src).Name;
            var destBase = keepName ? Path.Combine(destRoot, folderName) : destRoot;
            destBase = TransferService.GetNonCollidingFolderPath(destBase);

            var modeCopy = TransferModeCombo.SelectedIndex == 1;
            var verify = VerifyCheck.IsChecked == true;

            var confirm = System.Windows.MessageBox.Show(
                $"Start transfer?\n\nSource: {src}\nDestination: {destBase}\nMode: {(modeCopy ? "Copy" : "Move")}\nVerify: {(verify ? "Yes" : "No")}",
                "Confirm transfer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            CancelButton.IsEnabled = true;
            RunButton.IsEnabled = false;
            PreviewButton.IsEnabled = false;
            Progress.Value = 0;

            StatusText.Text = "Transferringand more";

            var result = await TransferService.TransferFolderAsync(
                src,
                destBase,
                modeCopy,
                verify,
                progress =>
                {
                    Progress.Value = progress.Percent;
                    StatusText.Text = progress.Message;
                },
                _cts.Token
            );

            CancelButton.IsEnabled = false;
            PreviewButton.IsEnabled = true;

            if (result.Canceled)
            {
                CancelButton.IsEnabled = false;
                PreviewButton.IsEnabled = true;
                RunButton.IsEnabled = true;
                StatusText.Text = "Transfer canceled.";
                return;
            }

            StatusText.Text = $"Done. Files: {result.FilesProcessed}. Failed: {result.Failed}. Destination: {destBase}";
            SummaryText.Text = $"Transfer complete. Total: {result.FilesProcessed}. Failed: {result.Failed}.";
            System.Windows.MessageBox.Show("Transfer complete.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CancelButton.IsEnabled = false;
            PreviewButton.IsEnabled = true;
            RunButton.IsEnabled = true;
            System.Windows.MessageBox.Show(ex.Message, "Transfer failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        StatusText.Text = "Cancel requested.";
        CancelButton.IsEnabled = false;
    }

    private void OpenAppsSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true });
        }
        catch
        {
            System.Windows.MessageBox.Show("Could not open Windows Apps Settings.", "Open failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GuardInputs()
    {
        var src = (SourceFolderBox.Text ?? "").Trim();
        var dest = (DestFolderBox.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(src) || !Directory.Exists(src))
            throw new Exception("Pick a valid source folder.");

        if (string.IsNullOrWhiteSpace(dest) || !Directory.Exists(dest))
            throw new Exception("Pick a valid destination folder.");

        // Basic guard: don't allow destination inside source
        var srcFull = Path.GetFullPath(src).TrimEnd(Path.DirectorySeparatorChar);
        var destFull = Path.GetFullPath(dest).TrimEnd(Path.DirectorySeparatorChar);

        if (destFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Destination cannot be inside the source folder.");
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return $"{v:0.##} {units[i]}";
    }
}
