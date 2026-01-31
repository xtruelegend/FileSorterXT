using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FileSorterXT.Models;
using FileSorterXT.Services;

namespace FileSorterXT.Views;

public partial class DuplicatesView : System.Windows.Controls.UserControl
{
    private AppSettings _settings;
    private CancellationTokenSource? _cts;
    private List<DuplicateGroup> _groups = new();

    public DuplicatesView()
    {
        InitializeComponent();
        _settings = SettingsService.Load();

        ScopeCombo.SelectedIndex = 0;
        RefreshApplyButton();
    }

    private void ScopeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CustomScanPanel.Visibility = ScopeCombo.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BrowseCustomScan_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(Window.GetWindow(this)).Handle;
        var folder = FolderPicker.PickFolder(hwnd, "Pick a folder to scan for duplicates");
        if (!string.IsNullOrWhiteSpace(folder))
            CustomScanBox.Text = folder;
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = SettingsService.Load();
            var folders = GetFoldersToScan();
            if (folders.Count == 0) throw new Exception("No folders to scan.");

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            DupCancelButton.IsEnabled = true;
            DupProgress.Value = 0;

            DupStatus.Text = "Scanningand more";
            GroupList.ItemsSource = null;
            FileList.ItemsSource = null;

            await Task.Run(() =>
            {
                _groups = DuplicateService.FindDuplicates(_settings, folders, _cts.Token);
            });

            GroupList.ItemsSource = _groups;
            GroupMeta.Text = $"Groups: {_groups.Count}";
            DupStatus.Text = _groups.Count == 0 ? "No duplicates found." : "Scan complete.";
            DupProgress.Value = 100;
            DupCancelButton.IsEnabled = false;

            RefreshApplyButton();
        }
        catch (Exception ex)
        {
            DupCancelButton.IsEnabled = false;
            System.Windows.MessageBox.Show(ex.Message, "Scan failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private List<string> GetFoldersToScan()
    {
        var folders = new List<string>();

        if (ScopeCombo.SelectedIndex == 1)
        {
            var custom = (CustomScanBox.Text ?? "").Trim();
            if (Directory.Exists(custom))
                folders.Add(custom);
            return folders;
        }

        var last = RunHistoryService.LoadLastRun();
        foreach (var d in last.DestinationsUsed)
        {
            if (Directory.Exists(d))
                folders.Add(d);
        }

        return folders.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void RefreshApplyButton()
    {
        _settings = SettingsService.Load();
        ApplyActionButton.IsEnabled = _settings.DuplicateAction != DuplicateAction.DoNotMove && _groups.Count > 0;
    }

    private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GroupList.SelectedItem is DuplicateGroup g)
            FileList.ItemsSource = g.Files;
    }

    private async void ApplyAction_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = SettingsService.Load();
            if (_settings.DuplicateAction == DuplicateAction.DoNotMove)
            {
                DupStatus.Text = "Duplicate action is set to Do not move.";
                return;
            }

            if (_groups.Count == 0) return;

            if (_settings.DuplicateActionRequiresConfirm)
            {
                var result = System.Windows.MessageBox.Show(
                    "Apply duplicate action now",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            DupCancelButton.IsEnabled = true;
            DupProgress.Value = 0;

            var logFile = LogService.NewLogFile("duplicates");
            DupStatus.Text = $"Applying action. Log: {logFile}";

            int moved = 0;
            await Task.Run(() =>
            {
                moved = DuplicateService.ApplyDuplicateAction(_settings, _groups, logFile, _cts.Token);
            });

            DupProgress.Value = 100;
            DupCancelButton.IsEnabled = false;

            DupStatus.Text = $"Duplicate action applied. Moved: {moved}.";
            _groups.Clear();
            GroupList.ItemsSource = null;
            FileList.ItemsSource = null;
            GroupMeta.Text = "";
            RefreshApplyButton();
        }
        catch (Exception ex)
        {
            DupCancelButton.IsEnabled = false;
            System.Windows.MessageBox.Show(ex.Message, "Apply failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        DupCancelButton.IsEnabled = false;
        DupStatus.Text = "Cancel requested.";
    }
}
