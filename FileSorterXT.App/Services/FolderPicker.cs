using System;
using System.Windows.Forms;

namespace FileSorterXT.Services;

public static class FolderPicker
{
    /// <summary>
    /// Stable folder picker (WinForms) to avoid COM dialog crashes on some systems.
    /// </summary>
    public static string? PickFolder(IntPtr ownerHwnd, string title)
    {
        try
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = title,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            DialogResult result;

            if (ownerHwnd != IntPtr.Zero)
            {
                using var owner = new Win32Window(ownerHwnd);
                result = dialog.ShowDialog(owner);
            }
            else
            {
                result = dialog.ShowDialog();
            }

            if (result != DialogResult.OK) return null;

            var path = dialog.SelectedPath;
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch
        {
            return null;
        }
    }

    private sealed class Win32Window : IWin32Window, IDisposable
    {
        public IntPtr Handle { get; }

        public Win32Window(IntPtr handle) => Handle = handle;

        public void Dispose() { }
    }
}
