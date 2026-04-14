using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using JulschaVehicleTool.App.Converters;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;

namespace JulschaVehicleTool.App.ViewModels;

public partial class ModelViewerViewModel : ObservableObject
{
    private readonly BinaryFileService _binaryFileService = new();
    private readonly MeshConversionService _meshConversionService = new();

    [ObservableProperty] private IEffectsManager? _effectsManager;
    [ObservableProperty] private Camera? _camera;
    [ObservableProperty] private string _selectedLod = "High";
    [ObservableProperty] private bool _showWireframe;
    [ObservableProperty] private bool _showTextures = true;
    [ObservableProperty] private string _statusMessage = "No model loaded. Use File > Load YFT to open a vehicle model.";
    [ObservableProperty] private bool _isLoaded;

    private VehicleModelData? _vehicleModel;

    public ObservableCollection<MeshNode> MeshNodes { get; } = new();
    public ObservableCollection<TextureEntry> Textures { get; } = new();
    public string[] LodLevels { get; } = ["High", "Medium", "Low", "Very Low"];

    [ObservableProperty] private TextureEntry? _selectedTexture;

    public bool HasTextures => Textures.Count > 0;
    public string TexturePanelHeader => $"Textures ({Textures.Count})";

    public ModelViewerViewModel()
    {
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera
        {
            Position = new System.Windows.Media.Media3D.Point3D(3, 3, 3),
            LookDirection = new System.Windows.Media.Media3D.Vector3D(-3, -3, -3),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            NearPlaneDistance = 0.01,
            FarPlaneDistance = 1000
        };
    }

    [RelayCommand]
    private void LoadModel()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "YFT Files|*.yft|All Files|*.*",
            Title = "Open Vehicle Model (.yft)"
        };
        if (dialog.ShowDialog() == true)
            LoadFromPath(dialog.FileName);
    }

    public void LoadFromModelData(VehicleModelData modelData)
    {
        _vehicleModel = modelData;
        IsLoaded = true;
        StatusMessage = "Model loaded from project";
        UpdateDisplayedMeshes();
        LoadTextures(modelData.Textures);
    }

    public void LoadFromPath(string yftPath, string? ytdPath = null)
    {
        try
        {
            var yft = _binaryFileService.LoadYft(yftPath);

            CodeWalker.GameFiles.YtdFile? ytd = null;
            if (ytdPath != null && File.Exists(ytdPath))
            {
                ytd = _binaryFileService.LoadYtd(ytdPath);
            }
            else
            {
                // Auto-detect YTD: same name as YFT but with .ytd extension
                var autoYtdPath = Path.ChangeExtension(yftPath, ".ytd");
                if (File.Exists(autoYtdPath))
                    ytd = _binaryFileService.LoadYtd(autoYtdPath);
            }

            _vehicleModel = _meshConversionService.ConvertYft(yft, ytd);
            IsLoaded = true;
            StatusMessage = $"Loaded: {Path.GetFileName(yftPath)}";

            UpdateDisplayedMeshes();
            LoadTextures(_vehicleModel.Textures);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsLoaded = false;
        }
    }

    partial void OnSelectedLodChanged(string value)
    {
        UpdateDisplayedMeshes();
    }

    private void UpdateDisplayedMeshes()
    {
        MeshNodes.Clear();
        if (_vehicleModel == null) return;

        var lod = SelectedLod switch
        {
            "High" => _vehicleModel.High,
            "Medium" => _vehicleModel.Medium,
            "Low" => _vehicleModel.Low,
            "Very Low" => _vehicleModel.VeryLow,
            _ => _vehicleModel.High
        };

        // Fall back to whatever is available
        lod ??= _vehicleModel.High ?? _vehicleModel.Medium ?? _vehicleModel.Low ?? _vehicleModel.VeryLow;

        if (lod == null) return;

        for (int i = 0; i < lod.Meshes.Count; i++)
        {
            var meshData = lod.Meshes[i];
            var helixMesh = HelixMeshConverter.ToHelixMesh(meshData);
            MeshNodes.Add(new MeshNode
            {
                Name = $"Mesh {i}",
                Geometry = helixMesh,
                IsVisible = true
            });
        }

        StatusMessage = $"LOD: {lod.Name} - {lod.Meshes.Count} meshes";
    }

    private void LoadTextures(List<TextureInfo> textures)
    {
        Textures.Clear();
        foreach (var tex in textures)
        {
            var thumb = ConvertDdsThumbnail(tex.DdsData);
            Textures.Add(new TextureEntry { Info = tex, Thumbnail = thumb });
        }
        OnPropertyChanged(nameof(HasTextures));
        OnPropertyChanged(nameof(TexturePanelHeader));
    }

    private static BitmapSource? ConvertDdsThumbnail(byte[] ddsData)
    {
        if (ddsData == null || ddsData.Length == 0) return null;
        try
        {
            using var ms = new MemoryStream(ddsData);
            var image = Pfim.Pfimage.FromStream(ms);

            var data = image.Data.ToArray();
            if (image.Format == Pfim.ImageFormat.Rgba32)
            {
                for (int i = 0; i < data.Length; i += 4)
                    (data[i], data[i + 2]) = (data[i + 2], data[i]);
            }

            var format = image.Format switch
            {
                Pfim.ImageFormat.Rgba32 => PixelFormats.Bgra32,
                Pfim.ImageFormat.Rgb24  => PixelFormats.Bgr24,
                _                       => PixelFormats.Bgra32
            };

            var bmp = BitmapSource.Create(image.Width, image.Height, 96, 96,
                format, null, data, image.Stride);
            bmp.Freeze();

            double scale = Math.Min(64.0 / image.Width, 64.0 / image.Height);
            if (scale >= 1.0) return bmp;
            var tb = new TransformedBitmap(bmp, new ScaleTransform(scale, scale));
            tb.Freeze();
            return tb;
        }
        catch { return null; }
    }
}

public class TextureEntry
{
    public TextureInfo Info { get; init; } = null!;
    public BitmapSource? Thumbnail { get; init; }
    public string DisplayText => $"{Info.Name}  {Info.Width}×{Info.Height}  {Info.Format}";
}

public partial class MeshNode : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private MeshGeometry3D? _geometry;
    [ObservableProperty] private bool _isVisible = true;
}
