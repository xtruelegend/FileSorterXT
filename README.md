FileSorterXT v2 (WPF, .NET 8)
============================

What it does
------------
- Pick a folder full of mixed files and sort them to Windows known folders by default:
  Pictures, Documents, Music, Videos.
- Override destinations per extension in Settings.
- If an extension has no destination mapping, those files do not move and Preview lists the unsorted types.
- Duplicate scan is supported with configurable definitions.
- Default duplicate behavior is detect only (do not move).
- Undo last run is supported.

Run with PowerShell
-------------------
1) cd into the folder that contains FileSorterXT_v2.sln
2) dotnet build
3) dotnet run --project .\FileSorterXT.App\FileSorterXT.App.csproj

Publish EXE
-----------
dotnet publish .\FileSorterXT.App\FileSorterXT.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

Where settings and logs live
----------------------------
Settings:
  %LOCALAPPDATA%\FileSorterXT\settings.json

Logs:
  %LOCALAPPDATA%\FileSorterXT\Logs

Last run (for undo + duplicate scope):
  %LOCALAPPDATA%\FileSorterXT\Runs\last_run.json

Default duplicates folder (when you choose "Move to Duplicates folder"):
  %USERPROFILE%\Documents\FileSorterXT\Duplicates


Folder picker note
-----------------
This build uses a WinForms FolderBrowserDialog for stability on systems where the COM folder picker can crash.


OneDrive note
------------
This build forces default targets to local user folders under %USERPROFILE% (Documents, Pictures, Music, Videos) instead of OneDrive-redirected Known Folders.
If you want OneDrive targets, map extensions to OneDrive paths in Settings.


Multiple source folders
----------------------
Sort tab supports adding multiple source folders. Preview and Confirm applies to all added folders in one run.


Transfer tab
------------
The Transfer tab moves or copies a folder to another drive or destination. It is intended for normal folders and portable apps. Installed programs usually should not be moved by folder transfer.


UX updates
---------
After mapping an extension, the app shows a confirmation and refreshes the Preview automatically.
Settings also shows a confirmation when mappings or settings are saved.


Name collisions
---------------
If a destination file already exists with the same name, the app will auto-rename the incoming file with (1), (2), etc so the sort can still run.
