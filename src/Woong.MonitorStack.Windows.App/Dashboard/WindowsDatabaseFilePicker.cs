using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Woong.MonitorStack.Windows.App.Dashboard;

public sealed class WindowsDatabaseFilePicker : IWindowsDatabaseFilePicker
{
    public string? PickNewDatabasePath(string currentDatabasePath)
    {
        var dialog = CreateSaveDialog(currentDatabasePath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickExistingDatabasePath(string currentDatabasePath)
    {
        var dialog = CreateOpenDialog(currentDatabasePath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool ConfirmDeleteDatabase(string currentDatabasePath)
    {
        MessageBoxResult result = MessageBox.Show(
            $"Delete local database and recreate an empty one?\n\n{currentDatabasePath}",
            "Delete local database",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    private static SaveFileDialog CreateSaveDialog(string currentDatabasePath)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Create or switch local Woong Monitor SQLite database",
            Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
            AddExtension = true,
            DefaultExt = ".db",
            FileName = Path.GetFileName(currentDatabasePath),
            InitialDirectory = GetInitialDirectory(currentDatabasePath)
        };

        return dialog;
    }

    private static OpenFileDialog CreateOpenDialog(string currentDatabasePath)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open existing Woong Monitor SQLite database",
            Filter = "SQLite database (*.db)|*.db|All files (*.*)|*.*",
            CheckFileExists = true,
            FileName = Path.GetFileName(currentDatabasePath),
            InitialDirectory = GetInitialDirectory(currentDatabasePath)
        };

        return dialog;
    }

    private static string GetInitialDirectory(string currentDatabasePath)
    {
        string? directory = Path.GetDirectoryName(currentDatabasePath);
        return string.IsNullOrWhiteSpace(directory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : directory;
    }
}
