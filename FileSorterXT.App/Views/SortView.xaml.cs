using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FileSorterXT.Models;
using FileSorterXT.Services;

namespace FileSorterXT.Views;

public partial class SortView : System.Windows.Controls.UserControl
{
    private AppSettings _settings;
    private List<PlanItem> _plan = new();
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoCts;

    private readonly ObservableCollection<string> _sources = new();
    public ObservableCollection<object> UnmappedItems { get; } = new();

    public SortView()
    {
        InitializeComponent();

        _settings = SettingsService.Load();
        ApplySettingsToUi();

        SourceList.ItemsSource = _sources;
        UnmappedList.ItemsSource = UnmappedItems;

        StatusText.Text = $"Settings: {Paths.SettingsPath}";
    }

    private void ApplySettingsToUi()
    {
        IncludeSubfoldersCheck.IsChecked = _settings.IncludeSubfolders;
        PreserveStructureCheck.IsChecked = _settings.PreserveStructure;
        IncludeHiddenCheck.IsChecked = _settings.IncludeHiddenAndSystem;

        ActionModeCombo.SelectedIndex = _settings.SortActionMode == SortActionMode.Copy ? 1 : 0;
        ConfirmModeCombo.SelectedIndex = _settings.ConfirmationMode switch
        {
            ConfirmationMode.AutoCountdown => 1,
            ConfirmationMode.PreviewOnly => 2,
            _ => 0
        };

        PreserveStructureCheck.IsEnabled = IncludeSubfoldersCheck.IsChecked == true;
    }

    private void SaveUiToSettings()
    {
        _settings.IncludeSubfolders = IncludeSubfoldersCheck.IsChecked == true;
        _settings.PreserveStructure = PreserveStructureCheck.IsChecked == true;
        _settings.IncludeHiddenAndSystem = IncludeHiddenCheck.IsChecked == true;

        _settings.SortActionMode = ActionModeCombo.SelectedIndex == 1 ? SortActionMode.Copy : SortActionMode.Move;
        _settings.ConfirmationMode = ConfirmModeCombo.SelectedIndex switch
        {
            1 => ConfirmationMode.AutoCountdown,
            2 => ConfirmationMode.PreviewOnly,
            _ => ConfirmationMode.Manual
        };

        SettingsService.Save(_settings);
    }

    private void AddSource_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var owner = Window.GetWindow(this);
            var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

            var path = FolderPicker.PickFolder(hwnd, "Pick a folder to sort");
            if (string.IsNullOrWhiteSpace(path)) return;

            path = path.Trim();
            if (!Directory.Exists(path))
            {
                System.Windows.MessageBox.Show("That folder does not exist.", "Invalid folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_sources.Any(s => string.Equals(s, path, StringComparison.OrdinalIgnoreCase)))
            {
                StatusText.Text = "Folder already added.";
                return;
            }

            _sources.Add(path);
            StatusText.Text = $"Added: {path}";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Folder select failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveSource_Click(object sender, RoutedEventArgs e)
    {
        if (SourceList.SelectedItem is string s)
        {
            _sources.Remove(s);
            StatusText.Text = "Removed selected folder.";
        }
    }

    private void ClearSources_Click(object sender, RoutedEventArgs e)
    {
        _sources.Clear();
        StatusText.Text = "Cleared source folders.";
    }

    private void OptionChanged_Click(object sender, RoutedEventArgs e)
    {
        PreserveStructureCheck.IsEnabled = IncludeSubfoldersCheck.IsChecked == true;
        SaveUiToSettings();
        UpdateRunButtonState();
    }

    private void OptionChanged_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveUiToSettings();
        UpdateRunButtonState();
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveUiToSettings();
            GuardSources();

            UnmappedItems.Clear();
            _plan.Clear();
            PlanList.ItemsSource = null;
            PlanMetaText.Text = "";

            var (plan, unmapped) = PlanService.BuildPlanMany(_settings, _sources);
            _plan = plan;

            foreach (var kv in unmapped.OrderByDescending(x => x.Value))
                UnmappedItems.Add(new { Ext = kv.Key, Count = kv.Value });

            PlanList.ItemsSource = _plan;

            var willProcess = _plan.Count(p => p.WillMoveOrCopy);
            var skipped = _plan.Count(p => p.IsSkipped);
            var collisions = _plan.Count(p => p.IsCollisionPossibleDuplicate);

            long totalBytes = PlanService.TotalBytesToProcess(_plan);
            var est = EstimateTime(totalBytes);

            PlanMetaText.Text = $"To process: {willProcess}   Skipped: {skipped}   Collision candidates: {collisions}";
            SummaryText.Text = $"Plan built for {_sources.Count} folder(s). Total bytes: {FormatBytes(totalBytes)}. Estimated time: {est}.";

            RunButton.IsEnabled = _settings.ConfirmationMode != ConfirmationMode.PreviewOnly && willProcess > 0;
            UpdateRunButtonState();

            StatusText.Text = UnmappedItems.Count > 0
                ? "Unsorted types found. Map destinations or decline. Those files will not move."
                : "Preview complete. Ready to run.";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Preview failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string EstimateTime(long bytes)
    {
        if (bytes <= 0) return "0s";
        double seconds = bytes / (80.0 * 1024 * 1024);
        if (seconds < 60) return $"{Math.Ceiling(seconds)}s";
        if (seconds < 3600) return $"{Math.Ceiling(seconds / 60)}m";
        return $"{Math.Ceiling(seconds / 3600)}h";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return $"{v:0.##} {units[i]}";
    }

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveUiToSettings();
            GuardSources();

            if (_plan.Count == 0)
                Preview_Click(sender, e);

            if (_plan.Count == 0) throw new Exception("Nothing to run.");

            if (_settings.ConfirmationMode == ConfirmationMode.Manual)
            {
                var result = System.Windows.MessageBox.Show(
                    "Run the sort now?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                await ExecuteRunAsync();
                return;
            }

            if (_settings.ConfirmationMode == ConfirmationMode.AutoCountdown)
            {
                StartAutoCountdown();
                return;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Run failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void StartAutoCountdown()
    {
        CancelAutoButton.IsEnabled = true;
        RunButton.IsEnabled = false;

        _autoCts?.Cancel();
        _autoCts = new CancellationTokenSource();
        var token = _autoCts.Token;

        StatusText.Text = "Auto confirm enabled. Running in 5 seconds. Click Cancel Auto Run to stop.";

        _ = Task.Run(async () =>
        {
            try
            {
                for (int i = 5; i >= 1; i--)
                {
                    token.ThrowIfCancellationRequested();
                    Dispatcher.Invoke(() => StatusText.Text = $"Auto run in {i} seconds. Click Cancel Auto Run to stop.");
                    await Task.Delay(1000, token);
                }

                Dispatcher.Invoke(() =>
                {
                    CancelAutoButton.IsEnabled = false;
                    _ = ExecuteRunAsync();
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Auto run canceled.";
                    CancelAutoButton.IsEnabled = false;
                    UpdateRunButtonState();
                });
            }
        });
    }

    private void CancelAuto_Click(object sender, RoutedEventArgs e)
    {
        _autoCts?.Cancel();
    }

    private async Task ExecuteRunAsync()
    {
        var toRun = _plan.Where(p => p.WillMoveOrCopy).ToList();
        if (toRun.Count == 0)
        {
            StatusText.Text = "Nothing to move or copy.";
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        CancelButton.IsEnabled = true;
        Progress.Value = 0;

        var logFile = LogService.NewLogFile("sort");
        StatusText.Text = $"Running. Log: {logFile}";

        var destinationsUsed = toRun
            .Select(p => Path.GetDirectoryName(p.DestinationPath ?? "") ?? "")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        int processedOk = 0, skipped = 0, failed = 0;
        var actions = new List<RunAction>();

        await Task.Run(() =>
        {
            int processed = 0;
            int total = toRun.Count;

            foreach (var item in toRun)
            {
                _cts!.Token.ThrowIfCancellationRequested();

                var (a, ok, sk, fl) = ExecutorService.ExecuteOne(_settings, item, logFile, _cts.Token);
                actions.AddRange(a);
                processedOk += ok;
                skipped += sk;
                failed += fl;

                processed++;
                var pct = (processed * 100.0) / Math.Max(1, total);
                Dispatcher.Invoke(() => Progress.Value = pct);
            }
        });

        CancelButton.IsEnabled = false;
        Progress.Value = 100;

        RunHistoryService.SaveLastRun(actions, destinationsUsed);

        StatusText.Text = $"Done. {processedOk} processed, {failed} failed. Log: {logFile}";
        SummaryText.Text = $"Run complete. Processed: {processedOk}. Failed: {failed}. Undo available.";

        _plan.Clear();
        PlanList.ItemsSource = null;
        RunButton.IsEnabled = false;
        UnmappedItems.Clear();
        PlanMetaText.Text = "";
    }

    private void CancelRun_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelButton.IsEnabled = false;
        StatusText.Text = "Cancel requested.";
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var log = LogService.NewLogFile("undo");
            var (undone, failed) = UndoService.UndoLastRun(log);
            StatusText.Text = $"Undo complete. Undone: {undone}. Failed: {failed}. Log: {log}";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Undo failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MapExt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string ext && !string.IsNullOrWhiteSpace(ext))
            {
                var owner = Window.GetWindow(this);
                var wnd = new MapExtensionWindow(ext, _settings) { Owner = owner };

                var ok = wnd.ShowDialog();
                if (ok == true)
                {
                    SettingsService.Save(_settings);
                    _settings = SettingsService.Load();

                    var dest = string.IsNullOrWhiteSpace(wnd.MappedDestination) ? "(unknown)" : wnd.MappedDestination!;
                    System.Windows.MessageBox.Show(
                        $"Mapping saved. {ext} → {dest}",
                        "Mapping complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    StatusText.Text = $"Mapping saved for {ext}. Refreshing preview.";
                    // Rebuild preview so the unmapped list and Run state update immediately.
                    Preview_Click(this, new RoutedEventArgs());
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Map failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    

private void UpdateRunButtonState()
{
    var willProcess = _plan.Any(p => p.WillMoveOrCopy);
    RunButton.IsEnabled = _settings.ConfirmationMode != ConfirmationMode.PreviewOnly && willProcess;
}
private void GuardSources()
    {
        if (_sources.Count == 0)
            throw new Exception("Add at least one source folder first.");

        var missing = _sources.Where(s => !Directory.Exists(s)).ToList();
        if (missing.Count > 0)
            throw new Exception("One or more source folders no longer exist.");
    }
}
