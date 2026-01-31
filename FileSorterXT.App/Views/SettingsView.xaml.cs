using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.VisualBasic;
using FileSorterXT.Models;
using FileSorterXT.Services;

namespace FileSorterXT.Views;

public partial class SettingsView : System.Windows.Controls.UserControl
{
    private AppSettings _settings = new();

    private ObservableCollection<MappingRow> _mappings = new();
    private ObservableCollection<string> _ignoreExt = new();
    private ObservableCollection<string> _ignorePaths = new();

    public SettingsView()
    {
        InitializeComponent();
        ReloadSettings();
        IsVisibleChanged += (_, __) => { if (IsVisible) ReloadSettings(); };
    }

    private void ReloadSettings()
    {
        _settings = SettingsService.Load();
        LoadToUi();
    }

    private void LoadToUi()
    {
        // Default destinations (editable)
        DefaultPicsBox.Text = string.IsNullOrWhiteSpace(_settings.DefaultPicturesFolder) ? KnownFolders.Pictures : _settings.DefaultPicturesFolder;
        DefaultDocsBox.Text = string.IsNullOrWhiteSpace(_settings.DefaultDocumentsFolder) ? KnownFolders.Documents : _settings.DefaultDocumentsFolder;
        DefaultMusicBox.Text = string.IsNullOrWhiteSpace(_settings.DefaultMusicFolder) ? KnownFolders.Music : _settings.DefaultMusicFolder;
        DefaultVideoBox.Text = string.IsNullOrWhiteSpace(_settings.DefaultVideosFolder) ? KnownFolders.Videos : _settings.DefaultVideosFolder;

        DefaultExamplesText.Text =
            "Examples: Pictures (.jpg, .jpeg, .png, .gif, .webp), " +
            "Documents (.pdf, .docx, .xlsx, .txt, .md), " +
            "Music (.mp3, .wav, .flac, .m4a, .ogg), " +
            "Videos (.mp4, .mov, .mkv, .avi, .webm).";

        // Extension mappings
        _mappings = new ObservableCollection<MappingRow>(
            _settings.ExtensionDestinations
                .OrderBy(kv => kv.Key)
                .Select(kv => new MappingRow { Extension = kv.Key, Destination = kv.Value })
        );
        MappingGrid.ItemsSource = _mappings;

        // Ignore lists
        _ignoreExt = new ObservableCollection<string>(_settings.IgnoreExtensions.OrderBy(x => x));
        IgnoreExtList.ItemsSource = _ignoreExt;

        _ignorePaths = new ObservableCollection<string>(_settings.IgnorePaths.OrderBy(x => x));
        IgnorePathList.ItemsSource = _ignorePaths;

        // Duplicate settings
        SetDupDefinitionUi(_settings.DuplicateDefinition);
        SetDupActionUi(_settings.DuplicateAction);
        SetKeepRuleUi(_settings.KeepRule);

        DupConfirmCheck.IsChecked = _settings.DuplicateActionRequiresConfirm;

        var useCustomDup = _settings.DuplicateAction == DuplicateAction.MoveToCustomFolder;
        CustomDupFolderPanel.Visibility = useCustomDup ? Visibility.Visible : Visibility.Collapsed;
        CustomDupFolderBox.Text = _settings.DuplicateCustomFolder ?? "";

        var quickAcc = _settings.DuplicateDefinition == DuplicateDefinition.QuickAccurateModes;
        QuickAccurateLabel.Visibility = quickAcc ? Visibility.Visible : Visibility.Collapsed;
        QuickAccurateCombo.Visibility = quickAcc ? Visibility.Visible : Visibility.Collapsed;
        SetQuickAccurateUi(_settings.QuickAccurateMode);

        SettingsPathText.Text = $"Settings stored at: {Paths.SettingsPath}";
        SaveStatus.Text = "";
    }

    private void SetDupDefinitionUi(DuplicateDefinition def)
    {
        // Matches XAML item text
        string text = def switch
        {
            DuplicateDefinition.FilenameOnly => "Same filename only",
            DuplicateDefinition.FilenameAndSize => "Same filename plus same size",
            DuplicateDefinition.HashMatch => "Same content (hash match)",
            DuplicateDefinition.QuickAccurateModes => "Quick plus Accurate modes available",
            _ => "Same filename plus same size"
        };
        SelectComboItemByContent(DupDefCombo, text);
    }

    private void SetDupActionUi(DuplicateAction act)
    {
        string text = act switch
        {
            DuplicateAction.DoNotMove => "Do not move duplicates (default)",
            DuplicateAction.MoveToDuplicatesFolder => "Move duplicates to Duplicates folder",
            DuplicateAction.MoveToCustomFolder => "Move duplicates to custom folder",
            _ => "Do not move duplicates (default)"
        };
        SelectComboItemByContent(DupActionCombo, text);
    }

    private void SetKeepRuleUi(KeepRule rule)
    {
        string text = rule switch
        {
            KeepRule.KeepNewest => "Keep newest",
            KeepRule.KeepOldest => "Keep oldest",
            
            _ => "Keep newest"
        };
        SelectComboItemByContent(KeepRuleCombo, text);
    }

    private void SetQuickAccurateUi(QuickAccurateMode mode)
    {
        string text = mode == QuickAccurateMode.Accurate ? "Accurate" : "Quick";
        SelectComboItemByContent(QuickAccurateCombo, text);
    }

    private static void SelectComboItemByContent(System.Windows.Controls.ComboBox combo, string content)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem cbi &&
                string.Equals((cbi.Content?.ToString() ?? "").Trim(), content.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = cbi;
                return;
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private void AddMapping_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        if (owner == null) return;

        var ext = Interaction.InputBox("Enter a file extension to map (example: .zip)", "Add Mapping", ".zip");
        ext = FileCategorizer.NormalizeExt(ext ?? "");
        if (string.IsNullOrWhiteSpace(ext)) return;

        var wnd = new MapExtensionWindow(ext, _settings) { Owner = owner };
        if (wnd.ShowDialog() == true && !string.IsNullOrWhiteSpace(wnd.MappedDestination))
        {
            SettingsService.Save(_settings);
            System.Windows.MessageBox.Show($"Mapping saved: {ext} -> {wnd.MappedDestination}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            ReloadSettings();
        }
    }

    private void RemoveMapping_Click(object sender, RoutedEventArgs e)
    {
        if (MappingGrid.SelectedItem is not MappingRow row) return;

        if (_settings.ExtensionDestinations.Remove(row.Extension))
        {
            SettingsService.Save(_settings);
            SaveStatus.Text = $"Mapping removed: {row.Extension}";
            ReloadSettings();
        }
    }

    private void AddIgnoreExt_Click(object sender, RoutedEventArgs e)
    {
        var ext = (IgnoreExtBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ext)) return;

        ext = FileCategorizer.NormalizeExt(ext);

        if (!_settings.IgnoreExtensions.Any(x => string.Equals(x, ext, StringComparison.OrdinalIgnoreCase)))
        {
            _settings.IgnoreExtensions.Add(ext);
            SettingsService.Save(_settings);
            IgnoreExtBox.Text = "";
            SaveStatus.Text = $"Ignore extension added: {ext}";
            ReloadSettings();
        }
    }

    private void RemoveIgnoreExt_Click(object sender, RoutedEventArgs e)
    {
        if (IgnoreExtList.SelectedItem is not string ext) return;

        _settings.IgnoreExtensions.RemoveAll(x => string.Equals(x, ext, StringComparison.OrdinalIgnoreCase));
        SettingsService.Save(_settings);
        SaveStatus.Text = "Ignore extension removed.";
        ReloadSettings();
    }

    private void BrowseIgnorePath_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

        var path = FolderPicker.PickFolder(hwnd, "Pick a folder to ignore");
        if (!string.IsNullOrWhiteSpace(path))
            IgnorePathBox.Text = path.Trim();
    }

    private void AddIgnorePath_Click(object sender, RoutedEventArgs e)
    {
        var p = (IgnorePathBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(p)) return;

        if (!_settings.IgnorePaths.Any(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase)))
        {
            _settings.IgnorePaths.Add(p);
            SettingsService.Save(_settings);
            IgnorePathBox.Text = "";
            SaveStatus.Text = "Ignore path added.";
            ReloadSettings();
        }
    }

    private void RemoveIgnorePath_Click(object sender, RoutedEventArgs e)
    {
        if (IgnorePathList.SelectedItem is not string p) return;

        _settings.IgnorePaths.RemoveAll(x => string.Equals(x, p, StringComparison.OrdinalIgnoreCase));
        SettingsService.Save(_settings);
        SaveStatus.Text = "Ignore path removed.";
        ReloadSettings();
    }

    private void DupDefCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DupDefCombo.SelectedItem is not ComboBoxItem cbi) return;
        var text = (cbi.Content?.ToString() ?? "").Trim();

        _settings.DuplicateDefinition = text switch
        {
            "Same filename only" => DuplicateDefinition.FilenameOnly,
            "Same filename plus same size" => DuplicateDefinition.FilenameAndSize,
            "Same content (hash match)" => DuplicateDefinition.HashMatch,
            "Quick plus Accurate modes available" => DuplicateDefinition.QuickAccurateModes,
            _ => DuplicateDefinition.FilenameAndSize
        };

        var quickAcc = _settings.DuplicateDefinition == DuplicateDefinition.QuickAccurateModes;
        QuickAccurateLabel.Visibility = quickAcc ? Visibility.Visible : Visibility.Collapsed;
        QuickAccurateCombo.Visibility = quickAcc ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DupActionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DupActionCombo.SelectedItem is not ComboBoxItem cbi) return;
        var text = (cbi.Content?.ToString() ?? "").Trim();

        _settings.DuplicateAction = text switch
        {
            "Do not move duplicates (default)" => DuplicateAction.DoNotMove,
            "Move duplicates to Duplicates folder" => DuplicateAction.MoveToDuplicatesFolder,
            "Move duplicates to custom folder" => DuplicateAction.MoveToCustomFolder,
            _ => DuplicateAction.DoNotMove
        };

        CustomDupFolderPanel.Visibility = _settings.DuplicateAction == DuplicateAction.MoveToCustomFolder
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PickCustomDupFolder_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

        var path = FolderPicker.PickFolder(hwnd, "Pick custom Duplicates folder");
        if (!string.IsNullOrWhiteSpace(path))
            CustomDupFolderBox.Text = path.Trim();
    }

    private void BrowseDefaultPics_Click(object sender, RoutedEventArgs e) => PickDefaultInto(DefaultPicsBox, "Pick default Pictures destination");
    private void BrowseDefaultDocs_Click(object sender, RoutedEventArgs e) => PickDefaultInto(DefaultDocsBox, "Pick default Documents destination");
    private void BrowseDefaultMusic_Click(object sender, RoutedEventArgs e) => PickDefaultInto(DefaultMusicBox, "Pick default Music destination");
    private void BrowseDefaultVideo_Click(object sender, RoutedEventArgs e) => PickDefaultInto(DefaultVideoBox, "Pick default Videos destination");

    private void PickDefaultInto(System.Windows.Controls.TextBox box, string title)
    {
        var owner = Window.GetWindow(this);
        var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;

        var path = FolderPicker.PickFolder(hwnd, title);
        if (!string.IsNullOrWhiteSpace(path))
            box.Text = path.Trim();
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.DefaultPicturesFolder = (DefaultPicsBox.Text ?? "").Trim();
            _settings.DefaultDocumentsFolder = (DefaultDocsBox.Text ?? "").Trim();
            _settings.DefaultMusicFolder = (DefaultMusicBox.Text ?? "").Trim();
            _settings.DefaultVideosFolder = (DefaultVideoBox.Text ?? "").Trim();

            // Pull grid back into dictionary
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in _mappings)
            {
                var ext = FileCategorizer.NormalizeExt(row.Extension ?? "");
                var dest = (row.Destination ?? "").Trim();
                if (string.IsNullOrWhiteSpace(ext) || string.IsNullOrWhiteSpace(dest)) continue;
                dict[ext] = dest;
            }
            _settings.ExtensionDestinations = dict;

            _settings.DuplicateCustomFolder = (CustomDupFolderBox.Text ?? "").Trim();
            _settings.DuplicateActionRequiresConfirm = DupConfirmCheck.IsChecked == true;

            // Keep rule
            if (KeepRuleCombo.SelectedItem is ComboBoxItem keepItem)
            {
                var k = (keepItem.Content?.ToString() ?? "").Trim();
                _settings.KeepRule = k switch
                {
                    "Keep newest" => KeepRule.KeepNewest,
                    "Keep oldest" => KeepRule.KeepOldest,
                    "Ask each time (not implemented, uses keep newest)" => KeepRule.KeepNewest,
                    _ => KeepRule.KeepNewest
                };
            }

            // Quick/Accurate
            if (QuickAccurateCombo.SelectedItem is ComboBoxItem qaItem)
            {
                var q = (qaItem.Content?.ToString() ?? "").Trim();
                _settings.QuickAccurateMode = q == "Accurate" ? QuickAccurateMode.Accurate : QuickAccurateMode.Quick;
            }

            SettingsService.Save(_settings);
            System.Windows.MessageBox.Show("Settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            SaveStatus.Text = "Settings saved.";
            ReloadSettings();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public class MappingRow
    {
        public string Extension { get; set; } = "";
        public string Destination { get; set; } = "";
    }
}
