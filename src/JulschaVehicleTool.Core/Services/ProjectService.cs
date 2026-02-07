using System.Text.Json;
using System.Text.Json.Serialization;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

public class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string RecentProjectsFileName = "recent_projects.json";
    private const int MaxRecentProjects = 10;

    public VehicleProject CreateNew()
    {
        return new VehicleProject { ProjectName = "Untitled" };
    }

    public VehicleProject Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var project = JsonSerializer.Deserialize<VehicleProject>(json, JsonOptions)
                      ?? throw new InvalidOperationException("Failed to deserialize project file.");
        project.ProjectFilePath = filePath;
        project.IsDirty = false;
        return project;
    }

    public void Save(VehicleProject project, string filePath)
    {
        project.ProjectFilePath = filePath;
        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(filePath, json);
        project.IsDirty = false;
    }

    // Recent Projects
    public List<string> LoadRecentProjects()
    {
        var path = GetRecentProjectsPath();
        if (!File.Exists(path)) return new List<string>();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void AddToRecentProjects(string filePath)
    {
        var recent = LoadRecentProjects();
        recent.Remove(filePath);
        recent.Insert(0, filePath);
        if (recent.Count > MaxRecentProjects)
            recent.RemoveRange(MaxRecentProjects, recent.Count - MaxRecentProjects);

        var json = JsonSerializer.Serialize(recent, JsonOptions);
        var dir = Path.GetDirectoryName(GetRecentProjectsPath());
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(GetRecentProjectsPath(), json);
    }

    private static string GetRecentProjectsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "JulschaVehicleTool", RecentProjectsFileName);
    }
}
