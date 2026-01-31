using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FileSorterXT.Services;

namespace FileSorterXT.Views;

public partial class HistoryView : System.Windows.Controls.UserControl
{
    private List<LogRow> _logs = new();

    public HistoryView()
    {
        InitializeComponent();
        Refresh();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        Paths.Ensure();
        _logs = Directory.EnumerateFiles(Paths.LogsDir, "*.log", SearchOption.TopDirectoryOnly)
            .Select(p => new FileInfo(p))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .Select(fi => new LogRow { FullPath = fi.FullName, Name = fi.Name, Modified = fi.LastWriteTime.ToString() })
            .ToList();

        LogList.ItemsSource = _logs;
        LogPreview.Text = $"Logs folder: {Paths.LogsDir}{Environment.NewLine}Settings: {Paths.SettingsPath}{Environment.NewLine}Last run: {RunHistoryService.LastRunPath}";
    }

    private void LogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LogList.SelectedItem is LogRow row)
        {
            try
            {
                var text = File.ReadAllText(row.FullPath);
                LogPreview.Text = text;
            }
            catch (Exception ex)
            {
                LogPreview.Text = ex.Message;
            }
        }
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        Paths.Ensure();
        Process.Start(new ProcessStartInfo
        {
            FileName = Paths.LogsDir,
            UseShellExecute = true
        });
    }

    private void OpenLastRun_Click(object sender, RoutedEventArgs e)
    {
        Paths.Ensure();
        var path = RunHistoryService.LastRunPath;
        if (!File.Exists(path))
        {
            System.Windows.MessageBox.Show("No last_run.json found yet. Run a sort first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public class LogRow
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Modified { get; set; } = "";
    }
}
