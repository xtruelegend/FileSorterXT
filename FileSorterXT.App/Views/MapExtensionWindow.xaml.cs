using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using FileSorterXT.Models;
using FileSorterXT.Services;

namespace FileSorterXT.Views;

public partial class MapExtensionWindow : Window
{
    private readonly string _ext;
    private readonly AppSettings _settings;

    private string? _pendingDest;

    public string? MappedDestination { get; private set; }

    public MapExtensionWindow(string extension, AppSettings settings)
    {
        InitializeComponent();
        _ext = FileCategorizer.NormalizeExt(extension);
        _settings = settings;

        Header.Text = $"Map {_ext}";
        NewFolderNameBox.Text = $"{_ext.Trim('.').ToUpperInvariant()} Files";

        if (_settings.ExtensionDestinations.TryGetValue(_ext, out var existing) && !string.IsNullOrWhiteSpace(existing))
            ExistingFolderBox.Text = existing;
    }

    private void BrowseExisting_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var folder = FolderPicker.PickFolder(hwnd, "Pick destination folder");
        if (!string.IsNullOrWhiteSpace(folder))
            ExistingFolderBox.Text = folder.Trim();
    }

    private void PickParent_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var folder = FolderPicker.PickFolder(hwnd, "Pick parent folder to create inside");
        if (!string.IsNullOrWhiteSpace(folder))
            CreateParentBox.Text = folder.Trim();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _pendingDest = ResolveDestinationOrThrow();
            ReviewExtText.Text = _ext;
            ReviewDestText.Text = _pendingDest;

            EditPanel.Visibility = Visibility.Collapsed;
            ReviewPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Cannot continue", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ReviewPanel.Visibility = Visibility.Collapsed;
        EditPanel.Visibility = Visibility.Visible;
    }

    private void ConfirmSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dest = _pendingDest ?? ResolveDestinationOrThrow();
            dest = Path.GetFullPath(dest);

            Directory.CreateDirectory(dest);

            _settings.ExtensionDestinations[_ext] = dest;
            MappedDestination = dest;

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Cannot save mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string ResolveDestinationOrThrow()
    {
        var existing = (ExistingFolderBox.Text ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var parent = (CreateParentBox.Text ?? "").Trim();
        var name = (NewFolderNameBox.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(parent))
            throw new Exception("Pick an existing folder or choose a parent folder.");

        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Enter a new folder name.");

        return Path.Combine(parent, name);
    }
}
