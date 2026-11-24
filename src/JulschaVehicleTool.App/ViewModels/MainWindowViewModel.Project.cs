using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.App.Views;
using JulschaVehicleTool.Core.Constants;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

// New/open/save/close project, local encryption and project ZIP sharing.
public partial class MainWindowViewModel
{
    #region Project Commands

    private void OnCreateProjectRequested(string folderPath)
    {
        Log($"[CreateProject] Folder: {folderPath}");
        try
        {
            _currentProject = _projectService.CreateNew(folderPath);
            _projectService.AddToRecentProjects(folderPath);
            OpenProjectInternal();
            StatusText = $"Created new project: {_currentProject.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error creating project: {ex.Message}";
        }
    }

    private void OnOpenProjectRequested(string folderPath)
    {
        Log($"[OpenProject] Folder: {folderPath}");
        try
        {
            _currentProject = _projectService.Open(folderPath);
            _projectService.AddToRecentProjects(folderPath);
            OpenProjectInternal();
            StatusText = $"Opened: {_currentProject.Name} ({_currentProject.Resources.Count} resource(s))";

            if (_currentProject.LoadedFromBackup)
                System.Windows.MessageBox.Show(
                    "The project file was damaged, so the backup (project.julveh.bak) was opened instead.\n\n" +
                    "The most recent changes may be missing. Save the project to repair the project file.",
                    "Project restored from backup", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            StatusText = $"Error opening project: {ex.Message}";
            System.Windows.MessageBox.Show(ex.Message, "Could not open project",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>Mirrors Project.EncryptLocal for the menu checkbox.</summary>
    [ObservableProperty]
    private bool _isProjectEncrypted;

    [RelayCommand]
    private async Task ToggleLocalEncryptionAsync()
    {
        if (_currentProject == null) return;
        var enable = !_currentProject.EncryptLocal;

        var message = enable
            ? "Encrypt this project with your Windows account (DPAPI)?\n\n" +
              "Only you, on this PC, can then open it. After reinstalling Windows or on another PC the project " +
              "can NOT be opened any more — keep a password-protected project ZIP as backup (File > Export Project ZIP)."
            : "Remove the encryption from this project?\n\n" +
              "The project and vehicle files are then stored unencrypted and can be opened on any PC.";
        if (System.Windows.MessageBox.Show(message, enable ? "Encrypt project" : "Decrypt project",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
        {
            IsProjectEncrypted = _currentProject.EncryptLocal; // undo the checkbox toggle
            return;
        }

        var project = _currentProject;
        try
        {
            SaveProject();
            IsProgressVisible = true;
            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = p.current;
                ProgressMax = p.total;
                ProgressMessage = p.message;
            });
            await Task.Run(() => _projectService.SetLocalEncryption(project, enable, progress));
            StatusText = enable ? "Project is now encrypted for this Windows user" : "Project is now stored unencrypted";
        }
        catch (Exception ex)
        {
            StatusText = $"Changing encryption failed: {ex.Message}";
        }
        finally
        {
            IsProgressVisible = false;
            IsProjectEncrypted = project.EncryptLocal;
        }
    }

    private void OpenProjectInternal()
    {
        Log($"[OpenInternal] Project: {_currentProject!.Name}, Resources: {_currentProject.Resources.Count}, Vehicles: {_currentProject.Resources.Sum(r => r.Vehicles.Count)}");
        IsProjectOpen = true;
        IsProjectEncrypted = _currentProject!.EncryptLocal;
        TitleSuffix = $" - {_currentProject!.Name}";
        _modelCache.Clear();

        // Set up project-level dirty tracking for the siren pool
        SubscribeProjectCarColsDirtyTracking(_currentProject);

        RebuildTree();
        _autoSaveTimer.Start();

        // Select first vehicle if available
        if (TreeNodes.Count > 0 && TreeNodes[0].Children.Count > 0)
        {
            var firstResource = TreeNodes[0].Children[0];
            if (firstResource.Children.Count > 0)
                SelectedTreeNode = firstResource.Children[0];
            else
                SelectedTreeNode = firstResource;
        }
    }

    private static System.Windows.Window? Owner
        => System.Windows.Application.Current.MainWindow;

    [RelayCommand]
    private void NewProject()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select folder for new project",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) == true)
            OnCreateProjectRequested(dialog.SelectedPath);
    }

    [RelayCommand]
    private void OpenProject()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Open project folder",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) == true)
            OnOpenProjectRequested(dialog.SelectedPath);
    }

    [RelayCommand]
    private void SaveProject()
    {
        if (_currentProject == null) return;

        Log($"[Save] Saving project: {_currentProject.Name}, Path: {_currentProject.FolderPath}");
        Log($"[Save] Resources: {_currentProject.Resources.Count}");
        foreach (var res in _currentProject.Resources)
        {
            Log($"[Save]   Resource '{res.Name}': {res.Vehicles.Count} vehicle(s)");
            foreach (var v in res.Vehicles)
            {
                Log($"[Save]     Vehicle '{v.Name}': YFT={v.YftRelativePath ?? "null"}, Handling={v.Handling?.HandlingName ?? "null"}, Meta={v.VehicleMeta?.GameName ?? "null"}, CarVar={v.CarVariation?.ModelName ?? "null"}, SirenId={v.CarVariation?.SirenSettings}");
            }
        }
        Log($"[Save] Project siren pool: {_currentProject.CarCols.SirenSettings.Count} group(s)");

        try
        {
            _projectService.Save(_currentProject);
            _currentProject.IsDirty = false;
            AutoSaveIndicator = $"Saved {DateTime.Now:HH:mm}";
            Log("[Save] OK");
            StatusText = $"Project saved";
        }
        catch (Exception ex)
        {
            Log($"[Save] ERROR: {ex.Message}");
            StatusText = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveProjectAs()
    {
        if (_currentProject == null) return;

        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Save project to folder",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) != true) return;

        try
        {
            _projectService.SaveAs(_currentProject, dialog.SelectedPath);
            _projectService.AddToRecentProjects(dialog.SelectedPath);
            _currentProject.IsDirty = false;
            TitleSuffix = $" - {_currentProject.Name}";
            StatusText = $"Project saved as: {dialog.SelectedPath}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseProject()
    {
        if (_currentProject != null && _currentProject.IsDirty)
            SaveProject();

        UnsubscribeDirtyTracking();
        UnsubscribeProjectCarColsDirtyTracking();
        _currentProject = null;
        _autoSaveTimer.Stop();
        IsProjectOpen = false;
        IsVehicleSelected = false;
        TreeNodes.Clear();
        _modelCache.Clear();
        TitleSuffix = "";
        StatusText = "Ready";

        _welcomeVm.RefreshRecentProjects();
        CurrentViewModel = _welcomeVm;
    }

    private void AutoSave()
    {
        if (_currentProject == null || !_currentProject.IsDirty) return;

        Log("[AutoSave] Triggering auto-save...");
        try
        {
            _projectService.Save(_currentProject);
            _currentProject.IsDirty = false;
            AutoSaveIndicator = $"Auto-saved {DateTime.Now:HH:mm}";
            Log("[AutoSave] OK");
        }
        catch (Exception ex)
        {
            Log($"[AutoSave] ERROR: {ex.Message}");
        }
    }

    #endregion

    #region ZIP Export/Import

    [RelayCommand]
    private async Task ExportProjectZipAsync()
    {
        if (_currentProject == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP Archive|*.zip",
            FileName = $"{_currentProject.Name}.zip",
            Title = "Export project as ZIP"
        };
        if (dialog.ShowDialog(Owner) != true) return;

        var pwdDlg = new PasswordDialog("Enter a password to protect the ZIP export:", confirm: true) { Owner = Owner };
        if (pwdDlg.ShowDialog() != true || string.IsNullOrEmpty(pwdDlg.Password)) return;
        var password = pwdDlg.Password;

        try
        {
            IsProgressVisible = true;
            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = p.current;
                ProgressMax = p.total;
                ProgressMessage = p.message;
            });

            await Task.Run(() => _projectService.ExportProjectZip(_currentProject, dialog.FileName, password, progress));
            StatusText = $"Project exported: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export error: {ex.Message}";
        }
        finally
        {
            IsProgressVisible = false;
        }
    }

    [RelayCommand]
    private async Task ImportProjectZipAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "ZIP Archive|*.zip",
            Title = "Import project from ZIP"
        };
        if (openDialog.ShowDialog(Owner) != true) return;

        var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select target folder for imported project",
            UseDescriptionForTitle = true,
        };
        if (folderDialog.ShowDialog(Owner) != true) return;

        var pwdDlg = new PasswordDialog("Enter the ZIP archive password:") { Owner = Owner };
        if (pwdDlg.ShowDialog() != true || string.IsNullOrEmpty(pwdDlg.Password)) return;
        var password = pwdDlg.Password;

        try
        {
            IsProgressVisible = true;
            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = p.current;
                ProgressMax = p.total;
                ProgressMessage = p.message;
            });

            _currentProject = await Task.Run(() =>
                _projectService.ImportProjectZip(openDialog.FileName, password, folderDialog.SelectedPath, progress));

            _projectService.AddToRecentProjects(folderDialog.SelectedPath);
            OpenProjectInternal();
            StatusText = $"Imported project: {_currentProject.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Import error: {ex.Message}";
        }
        finally
        {
            IsProgressVisible = false;
        }
    }

    #endregion
}
