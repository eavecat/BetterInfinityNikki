using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BetterInfinityNikki.Model.MaskMap;

namespace BetterInfinityNikki.View.Controls;

public class PointsCanvas : FrameworkElement
{
    private readonly VisualCollection _children;
    private readonly DrawingVisual _drawingVisual;
    private readonly Dictionary<string, Brush> _colorBrushCache;

    private ObservableCollection<MaskMapPoint>? _points;
    private ObservableCollection<MaskMapPointLabel>? _labels;
    private List<MaskMapPoint> _allPoints = new();
    private Dictionary<string, MaskMapPointLabel> _labelMap = new();
    private Rect _viewportRect = Rect.Empty;

    private static readonly SolidColorBrush FillBrush;
    private static readonly Pen BorderPen;
    private static readonly SolidColorBrush ShadowBrush;

    static PointsCanvas()
    {
        FillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#323947"));
        FillBrush.Freeze();

        var borderBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0xBC, 0x8E));
        borderBrush.Freeze();
        BorderPen = new Pen(borderBrush, 2.0);
        BorderPen.Freeze();

        ShadowBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
        ShadowBrush.Freeze();
    }

    #region 依赖属性

    public static readonly DependencyProperty PointsSourceProperty =
        DependencyProperty.Register(
            nameof(PointsSource),
            typeof(ObservableCollection<MaskMapPoint>),
            typeof(PointsCanvas),
            new PropertyMetadata(null, OnPointsSourceChanged));

    public static readonly DependencyProperty LabelsSourceProperty =
        DependencyProperty.Register(
            nameof(LabelsSource),
            typeof(ObservableCollection<MaskMapPointLabel>),
            typeof(PointsCanvas),
            new PropertyMetadata(null, OnLabelsSourceChanged));

    public ObservableCollection<MaskMapPoint>? PointsSource
    {
        get => (ObservableCollection<MaskMapPoint>?)GetValue(PointsSourceProperty);
        set => SetValue(PointsSourceProperty, value);
    }

    public ObservableCollection<MaskMapPointLabel>? LabelsSource
    {
        get => (ObservableCollection<MaskMapPointLabel>?)GetValue(LabelsSourceProperty);
        set => SetValue(LabelsSourceProperty, value);
    }

    #endregion

    private static void OnPointsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PointsCanvas)d).UpdatePoints(e.NewValue as ObservableCollection<MaskMapPoint>);
    }

    private static void OnLabelsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PointsCanvas)d).UpdateLabels(e.NewValue as ObservableCollection<MaskMapPointLabel>);
    }

    public PointsCanvas()
    {
        _children = new VisualCollection(this);
        _drawingVisual = new DrawingVisual();
        _children.Add(_drawingVisual);
        _colorBrushCache = new Dictionary<string, Brush>();
        IsHitTestVisible = true;
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Refresh();
    }

    #region Visual 相关

    protected override int VisualChildrenCount => _children.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }

    #endregion

    #region 集合和属性变更处理

    private void UpdatePoints(ObservableCollection<MaskMapPoint>? points)
    {
        if (_points != null)
        {
            _points.CollectionChanged -= OnPointsCollectionChanged;
            foreach (var point in _points)
            {
                UnsubscribePoint(point);
            }
        }

        _points = points;

        if (_points != null)
        {
            _points.CollectionChanged += OnPointsCollectionChanged;
            foreach (var point in _points)
            {
                SubscribePoint(point);
            }
            _allPoints = _points.ToList();
        }
        else
        {
            _allPoints.Clear();
        }

        Refresh();
    }

    private void UpdateLabels(ObservableCollection<MaskMapPointLabel>? labels)
    {
        if (_labels != null)
        {
            _labels.CollectionChanged -= OnLabelsCollectionChanged;
        }

        _labels = labels;

        if (_labels != null)
        {
            _labels.CollectionChanged += OnLabelsCollectionChanged;
            RebuildLabelMap();
        }
        else
        {
            _labelMap.Clear();
            _colorBrushCache.Clear();
        }

        Refresh();
    }

    private void RebuildLabelMap()
    {
        _labelMap.Clear();
        _colorBrushCache.Clear();
        if (_labels != null)
        {
            foreach (var label in _labels)
            {
                _labelMap[label.LabelId] = label;
            }
        }
    }

    private void OnLabelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildLabelMap();
        Refresh();
    }

    private void OnPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (MaskMapPoint point in e.OldItems)
            {
                UnsubscribePoint(point);
            }
        }

        if (e.NewItems != null)
        {
            foreach (MaskMapPoint point in e.NewItems)
            {
                SubscribePoint(point);
            }
        }

        _allPoints = _points?.ToList() ?? new List<MaskMapPoint>();
        Refresh();
    }

    private void SubscribePoint(MaskMapPoint point)
    {
        if (point is INotifyPropertyChanged notifyPoint)
        {
            notifyPoint.PropertyChanged += OnPointPropertyChanged;
        }
    }

    private void UnsubscribePoint(MaskMapPoint point)
    {
        if (point is INotifyPropertyChanged notifyPoint)
        {
            notifyPoint.PropertyChanged -= OnPointPropertyChanged;
        }
    }

    private void OnPointPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Refresh();
    }

    #endregion

    #region 渲染逻辑

    private void RenderPoints()
    {
        using var dc = _drawingVisual.RenderOpen();

        if (_allPoints.Count == 0 || _viewportRect.IsEmpty)
        {
            return;
        }

        var aw = ActualWidth;
        var ah = ActualHeight;
        if (aw <= 0 || ah <= 0)
        {
            return;
        }

        var expandedViewport = _viewportRect;
        expandedViewport.Inflate(MaskMapPointStatic.Width, MaskMapPointStatic.Height);

        var scaleX = aw / _viewportRect.Width;
        var scaleY = ah / _viewportRect.Height;

        foreach (var point in _allPoints)
        {
            if (!expandedViewport.Contains(point.ImageX, point.ImageY))
            {
                continue;
            }

            var localX = (point.ImageX - _viewportRect.X) * scaleX;
            var localY = (point.ImageY - _viewportRect.Y) * scaleY;

            DrawPoint(dc, point, localX, localY, MaskMapPointStatic.Width, MaskMapPointStatic.Height);
        }
    }

    private void DrawPoint(DrawingContext dc, MaskMapPoint point, double centerX, double centerY, double width, double height)
    {
        double radius = width / 2.0;
        var circleCenter = new Point(centerX, centerY);
        var circleGeometry = new EllipseGeometry(circleCenter, radius, radius);

        // 阴影
        var shadowGeometry = new EllipseGeometry(
            new Point(circleCenter.X + 2, circleCenter.Y + 2), radius, radius);
        dc.DrawGeometry(ShadowBrush, null, shadowGeometry);

        // 底圆
        dc.DrawGeometry(FillBrush, BorderPen, circleGeometry);

        // 内容
        if (_labelMap.TryGetValue(point.LabelId, out var label))
        {
            if (label.IconImage != null)
            {
                var imageRect = new Rect(centerX - radius, centerY - radius, width, height);
                dc.PushClip(circleGeometry);
                dc.DrawImage(label.IconImage, imageRect);
                dc.Pop();
            }
            else
            {
                dc.DrawEllipse(GetColorBrush(label), null, circleCenter, radius, radius);
            }
        }
        else
        {
            var brush = new SolidColorBrush(GenerateRandomColor(point.Id));
            brush.Freeze();
            dc.DrawEllipse(brush, null, circleCenter, radius, radius);
        }
    }

    private Brush GetColorBrush(MaskMapPointLabel label)
    {
        if (_colorBrushCache.TryGetValue(label.LabelId, out var cachedBrush))
        {
            return cachedBrush;
        }

        Color color;
        if (label.Color.HasValue)
        {
            var c = label.Color.Value;
            color = Color.FromArgb(c.A, c.R, c.G, c.B);
        }
        else
        {
            color = GenerateRandomColor(label.LabelId);
        }

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        _colorBrushCache[label.LabelId] = brush;

        return brush;
    }

    private static Color GenerateRandomColor(string seed)
    {
        var hash = seed?.GetHashCode() ?? 0;
        var random = new Random(hash);
        return Color.FromRgb(
            (byte)random.Next(80, 256),
            (byte)random.Next(80, 256),
            (byte)random.Next(80, 256));
    }

    #endregion

    #region 公共方法

    public void UpdateViewport(double x, double y, double width, double height)
    {
        var newRect = new Rect(x, y, width, height);
        if (newRect.Equals(_viewportRect))
        {
            return;
        }

        _viewportRect = newRect;
        Refresh();
    }

    public void Refresh()
    {
        RenderPoints();
    }

    #endregion
}
