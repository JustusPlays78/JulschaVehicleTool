using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Services;

namespace JulschaVehicleTool.App.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly IProjectService _projectService;

    public ObservableCollection<RecentProjectItem> RecentProjects { get; } = new();

    /// <summary>
    /// Raised when a project should be created. Parameter: folder path.
    /// </summary>
    public event Action<string>? CreateProjectRequested;

    /// <summary>
    /// Raised when a project should be opened. Parameter: folder path.
    /// </summary>
    public event Action<string>? OpenProjectRequested;

    public WelcomeViewModel(IProjectService projectService)
    {
        _projectService = projectService;
        RefreshRecentProjects();
    }

    public void RefreshRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var path in _projectService.LoadRecentProjects())
        {
            RecentProjects.Add(new RecentProjectItem
            {
                FilePath = path,
                DisplayName = Path.GetFileName(path),
                FolderPath = path,
            });
        }
    }

    [RelayCommand]
    private void NewProject()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select folder for new project",
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog() == true)
            CreateProjectRequested?.Invoke(dialog.SelectedPath);
    }

    [RelayCommand]
    private void OpenProject()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Open project folder",
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog() == true)
            OpenProjectRequested?.Invoke(dialog.SelectedPath);
    }

    [RelayCommand]
    private void OpenRecentProject(string? path)
    {
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            OpenProjectRequested?.Invoke(path);
    }
}
