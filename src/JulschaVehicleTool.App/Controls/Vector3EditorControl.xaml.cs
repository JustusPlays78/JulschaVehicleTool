using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace JulschaVehicleTool.App.Controls;

public partial class Vector3EditorControl : UserControl
{
    private bool _updatingFromValue;

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(Vector3EditorControl), new PropertyMetadata(""));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(Vector3), typeof(Vector3EditorControl),
            new FrameworkPropertyMetadata(Vector3.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty XProperty =
        DependencyProperty.Register(nameof(X), typeof(double), typeof(Vector3EditorControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentChanged));

    public static readonly DependencyProperty YProperty =
        DependencyProperty.Register(nameof(Y), typeof(double), typeof(Vector3EditorControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentChanged));

    public static readonly DependencyProperty ZProperty =
        DependencyProperty.Register(nameof(Z), typeof(double), typeof(Vector3EditorControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentChanged));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public Vector3 Value { get => (Vector3)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double X { get => (double)GetValue(XProperty); set => SetValue(XProperty, value); }
    public double Y { get => (double)GetValue(YProperty); set => SetValue(YProperty, value); }
    public double Z { get => (double)GetValue(ZProperty); set => SetValue(ZProperty, value); }

    public Vector3EditorControl()
    {
        InitializeComponent();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (Vector3EditorControl)d;
        if (ctrl._updatingFromValue) return;
        ctrl._updatingFromValue = true;
        var v = (Vector3)e.NewValue;
        ctrl.X = v.X;
        ctrl.Y = v.Y;
        ctrl.Z = v.Z;
        ctrl._updatingFromValue = false;
    }

    private static void OnComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (Vector3EditorControl)d;
        if (ctrl._updatingFromValue) return;
        ctrl._updatingFromValue = true;
        ctrl.Value = new Vector3((float)ctrl.X, (float)ctrl.Y, (float)ctrl.Z);
        ctrl._updatingFromValue = false;
    }
}
