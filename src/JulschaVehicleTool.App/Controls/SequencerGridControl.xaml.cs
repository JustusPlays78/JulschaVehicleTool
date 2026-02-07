using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JulschaVehicleTool.App.Controls;

public partial class SequencerGridControl : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SequencerGridControl), new PropertyMetadata("Sequencer"));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(uint), typeof(SequencerGridControl),
            new FrameworkPropertyMetadata(0u, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty ActiveColorProperty =
        DependencyProperty.Register(nameof(ActiveColor), typeof(Brush), typeof(SequencerGridControl),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x5B, 0x9B, 0xD5)), OnValueChanged));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public uint Value { get => (uint)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public Brush ActiveColor { get => (Brush)GetValue(ActiveColorProperty); set => SetValue(ActiveColorProperty, value); }

    private bool _isDragging;
    private bool _paintMode; // true = set bit, false = clear bit

    public SequencerGridControl()
    {
        InitializeComponent();
        BuildGrid();
    }

    private void BuildGrid()
    {
        BitGrid.Children.Clear();
        for (int i = 31; i >= 0; i--)
        {
            var rect = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = Application.Current.TryFindResource("BorderBrush") as Brush ?? Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Tag = i,
                Cursor = Cursors.Hand,
                SnapsToDevicePixels = true,
            };
            BitGrid.Children.Add(rect);
        }
        UpdateGrid();
    }

    private void UpdateGrid()
    {
        var val = Value;
        var activeBrush = ActiveColor;
        var inactiveBrush = Application.Current.TryFindResource("SurfaceBrush") as Brush ?? Brushes.Black;

        foreach (Border child in BitGrid.Children)
        {
            int bit = (int)child.Tag;
            bool isSet = (val & (1u << bit)) != 0;
            child.Background = isSet ? activeBrush : inactiveBrush;
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SequencerGridControl ctrl)
            ctrl.UpdateGrid();
    }

    private void ToggleBit(int bit)
    {
        Value ^= (1u << bit);
    }

    private void SetBit(int bit, bool on)
    {
        if (on)
            Value |= (1u << bit);
        else
            Value &= ~(1u << bit);
    }

    private int? GetBitFromPosition(Point pos)
    {
        var result = VisualTreeHelper.HitTest(BitGrid, pos);
        if (result?.VisualHit is Border border && border.Tag is int bit)
            return bit;
        // Walk up in case we hit a child
        var element = result?.VisualHit as DependencyObject;
        while (element != null && element != BitGrid)
        {
            if (element is Border b && b.Tag is int bitVal)
                return bitVal;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private void BitGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var bit = GetBitFromPosition(e.GetPosition(BitGrid));
        if (bit == null) return;

        _isDragging = true;
        bool isCurrentlySet = (Value & (1u << bit.Value)) != 0;
        _paintMode = !isCurrentlySet; // toggle: if off -> paint on, if on -> paint off
        SetBit(bit.Value, _paintMode);
        BitGrid.CaptureMouse();
        e.Handled = true;
    }

    private void BitGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var bit = GetBitFromPosition(e.GetPosition(BitGrid));
        if (bit == null) return;
        SetBit(bit.Value, _paintMode);
    }

    private void BitGrid_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        BitGrid.ReleaseMouseCapture();
    }
}
