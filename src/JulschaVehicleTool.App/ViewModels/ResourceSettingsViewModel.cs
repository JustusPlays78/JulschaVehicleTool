using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;

namespace JulschaVehicleTool.App.ViewModels;

public partial class ResourceSettingsViewModel : ObservableObject
{
    private readonly FiveMExportService _exportService;
    private readonly MetaXmlService _metaXmlService;
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private Resource? _resource;

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private int _exportProgress;

    [ObservableProperty]
    private int _exportTotal;

    [ObservableProperty]
    private string _exportProgressMessage = "";

    public ResourceSettingsViewModel(
        FiveMExportService exportService,
        MetaXmlService metaXmlService,
        IProjectService projectService)
    {
        _exportService = exportService;
        _metaXmlService = metaXmlService;
        _projectService = projectService;
    }

    public void Load(Resource resource, Project project)
    {
        Resource = resource;
        Project = project;
        StatusMessage = $"Resource: {resource.Name} ({resource.Vehicles.Count} vehicle(s))";
    }

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select export output folder",
            UseDescriptionForTitle = true,
        };

        var owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog(owner) == true)
            OutputPath = dialog.SelectedPath;
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (Resource == null || Project == null)
        {
            StatusMessage = "No resource selected.";
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            StatusMessage = "Please select an output folder.";
            return;
        }

        if (Resource.Vehicles.Count == 0)
        {
            StatusMessage = "Resource has no vehicles to export.";
            return;
        }

        // Validate all vehicles before export
        var validationIssues = Resource.Vehicles
            .SelectMany(v => VehicleValidator.Validate(v)
                .Select(i => $"[{v.Name}] {i.Severity}: {i.Message}"))
            .ToList();

        if (validationIssues.Any(m => m.Contains("Error")))
        {
            var errors = validationIssues.Where(m => m.Contains("Error")).ToList();
            StatusMessage = "Export blocked — critical errors:\n" + string.Join("\n", errors);
            return;
        }

        if (validationIssues.Count > 0)
            StatusMessage = $"Warnings ({validationIssues.Count}): {string.Join("; ", validationIssues)}";

        IsExporting = true;
        if (validationIssues.Count == 0)
            StatusMessage = "Exporting...";

        try
        {
            var exportDir = Path.Combine(OutputPath, Resource.Name);
            var options = new FiveMExportOptions
            {
                Resource = Resource,
                Project = Project,
                OutputPath = exportDir,
                MetaXmlService = _metaXmlService,
                ProjectService = _projectService,
            };

            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ExportProgress = p.current;
                ExportTotal = p.total;
                ExportProgressMessage = p.message;
            });

            var result = await Task.Run(() => _exportService.ExportResource(options, progress));

            if (result.Success)
            {
                StatusMessage = $"Export successful: {result.OutputPath}";
                if (result.Warnings.Count > 0)
                    StatusMessage += $" ({result.Warnings.Count} warning(s))";
            }
            else
            {
                StatusMessage = $"Export failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }
}
