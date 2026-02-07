using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using HelixToolkit.Wpf.SharpDX;
using JulschaVehicleTool.App.ViewModels;
using Color4 = HelixToolkit.Maths.Color4;
using PhongMaterial = HelixToolkit.Wpf.SharpDX.PhongMaterial;

namespace JulschaVehicleTool.App.Views;

public partial class ModelViewerView : UserControl
{
    private readonly List<MeshGeometryModel3D> _meshElements = new();

    public ModelViewerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ModelViewerViewModel oldVm)
            oldVm.MeshNodes.CollectionChanged -= OnMeshNodesChanged;

        if (e.NewValue is ModelViewerViewModel newVm)
            newVm.MeshNodes.CollectionChanged += OnMeshNodesChanged;
    }

    private void OnMeshNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var elem in _meshElements)
                Viewport.Items.Remove(elem);
            _meshElements.Clear();
        }

        if (e.NewItems != null)
        {
            foreach (MeshNode node in e.NewItems)
            {
                if (node.Geometry == null) continue;

                var material = new PhongMaterial
                {
                    DiffuseColor = new Color4(0.7f, 0.7f, 0.7f, 1f),
                    SpecularColor = new Color4(0.2f, 0.2f, 0.2f, 1f),
                    SpecularShininess = 20f
                };

                var model = new MeshGeometryModel3D
                {
                    Geometry = node.Geometry,
                    Material = material,
                    IsRendering = node.IsVisible
                };

                _meshElements.Add(model);
                Viewport.Items.Add(model);
            }
        }
    }
}
