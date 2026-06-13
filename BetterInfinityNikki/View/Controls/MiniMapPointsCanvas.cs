using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using BetterInfinityNikki.Model.MaskMap;
using BetterInfinityNikki.ViewModel;

namespace BetterInfinityNikki.View.Controls;

public sealed class MiniMapPointsCanvas : FrameworkElement
{
    public static readonly DependencyProperty PointsSourceProperty =
        DependencyProperty.Register(
            nameof(PointsSource),
            typeof(ObservableCollection<MaskMapPoint>),
            typeof(MiniMapPointsCanvas),
            new PropertyMetadata(null, OnPointsSourceChanged));

    public static readonly DependencyProperty LabelsSourceProperty =
        DependencyProperty.Register(
            nameof(LabelsSource),
            typeof(IEnumerable<MaskMapPointLabel>),
            typeof(MiniMapPointsCanvas),
            new PropertyMetadata(null, OnLabelsSourceChanged));

    public static readonly DependencyProperty ShowCollectedPointsProperty =
        DependencyProperty.Register(
            nameof(ShowCollectedPoints),
            typeof(bool),
            typeof(MiniMapPointsCanvas),
            new PropertyMetadata(false, OnShowCollectedPointsChanged));

    private readonly VisualCollection _children;
    private readonly DrawingVisual _drawingVisual;
    private readonly Dictionary<string, Brush> _colorBrushCache;
    private int _refreshQueued;

    private ObservableCollection<MaskMapPoint>? _points;
    private List<MaskMapPoint> _allPoints = new();
    private Dictionary<string, MaskMapPointLabel> _labelMap = new();
    private Rect _viewportRect = Rect.Empty;

    // 网格空间索引：单元格尺寸 128 单位，约覆盖小地图视口的一半
    private const double GridCellSize = 128.0;
    private Dictionary<(long cx, long cy), List<MaskMapPoint>> _gridIndex = new();

    public ObservableCollection<MaskMapPoint>? PointsSource
    {
        get => (ObservableCollection<MaskMapPoint>?)GetValue(PointsSourceProperty);
        set => SetValue(PointsSourceProperty, value);
    }

    public IEnumerable<MaskMapPointLabel>? LabelsSource
    {
        get => (IEnumerable<MaskMapPointLabel>?)GetValue(LabelsSourceProperty);
        set => SetValue(LabelsSourceProperty, value);
    }

    public bool ShowCollectedPoints
    {
        get => (bool)GetValue(ShowCollectedPointsProperty);
        set => SetValue(ShowCollectedPointsProperty, value);
    }

    private static void OnShowCollectedPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MiniMapPointsCanvas)d).Refresh();
    }

    public MiniMapPointsCanvas()
    {
        _children = new VisualCollection(this);
        _drawingVisual = new DrawingVisual();
        _children.Add(_drawingVisual);
        _colorBrushCache = new Dictionary<string, Brush>();

        IsHitTestVisible = false;

        MapIconImageCache.ImageUpdated += OnImageCacheUpdated;
    }

    private static void OnPointsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MiniMapPointsCanvas)d).UpdatePoints(e.NewValue as ObservableCollection<MaskMapPoint>);
    }

    private static void OnLabelsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MiniMapPointsCanvas)d).UpdateLabels(e.NewValue as IEnumerable<MaskMapPointLabel>);
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        if (VisualParent == null)
        {
            MapIconImageCache.ImageUpdated -= OnImageCacheUpdated;
        }
    }

    private void OnImageCacheUpdated(object? sender, string e)
    {
        if (Interlocked.Exchange(ref _refreshQueued, 1) != 0) return;

        Dispatcher.BeginInvoke(() =>
        {
            Interlocked.Exchange(ref _refreshQueued, 0);
            Refresh();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    protected override int VisualChildrenCount => _children.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }

    #region 集合管理

    private void UpdatePoints(ObservableCollection<MaskMapPoint>? points)
    {
        if (_points != null)
        {
            _points.CollectionChanged -= OnPointsCollectionChanged;
            foreach (var point in _points) UnsubscribePoint(point);
        }

        _points = points;

        if (_points != null)
        {
            _points.CollectionChanged += OnPointsCollectionChanged;
            foreach (var point in _points) SubscribePoint(point);
            _allPoints = _points.ToList();
        }
        else
        {
            _allPoints.Clear();
        }

        RebuildGridIndex();
        Refresh();
    }

    private void UpdateLabels(IEnumerable<MaskMapPointLabel>? labels)
    {
        if (labels != null)
        {
            _labelMap = labels.ToDictionary(l => l.LabelId, l => l);
            _colorBrushCache.Clear();
        }
        else
        {
            _labelMap.Clear();
            _colorBrushCache.Clear();
        }

        Refresh();
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (MaskMapPoint point in e.OldItems) UnsubscribePoint(point);

        if (e.NewItems != null)
            foreach (MaskMapPoint point in e.NewItems) SubscribePoint(point);

        _allPoints = _points?.ToList() ?? new List<MaskMapPoint>();
        RebuildGridIndex();
        Refresh();
    }

    private void SubscribePoint(MaskMapPoint point)
    {
        if (point is INotifyPropertyChanged n) n.PropertyChanged += OnPointPropertyChanged;
    }

    private void UnsubscribePoint(MaskMapPoint point)
    {
        if (point is INotifyPropertyChanged n) n.PropertyChanged -= OnPointPropertyChanged;
    }

    private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    /// <summary>
    /// 构建网格空间索引，把每个点放入对应的单元格，用于 O(1) 视口查询
    /// </summary>
    private void RebuildGridIndex()
    {
        _gridIndex.Clear();
        foreach (var point in _allPoints)
        {
            var cell = CellOf(point.ImageX, point.ImageY);
            if (!_gridIndex.TryGetValue(cell, out var list))
            {
                list = new List<MaskMapPoint>();
                _gridIndex[cell] = list;
            }
            list.Add(point);
        }
    }

    private static (long cx, long cy) CellOf(double x, double y)
    {
        return ((long)Math.Floor(x / GridCellSize), (long)Math.Floor(y / GridCellSize));
    }

    #endregion

    #region 渲染

    private void RenderPoints()
    {
        using var dc = _drawingVisual.RenderOpen();
        if (_allPoints.Count == 0 || _viewportRect.IsEmpty || _viewportRect.Width == 0) return;

        var aw = ActualWidth;
        var ah = ActualHeight;
        if (aw <= 0 || ah <= 0) return;

        var side = Math.Min(aw, ah);
        if (side <= 0) return;

        var clipRect = new Rect((aw - side) / 2.0, (ah - side) / 2.0, side, side);
        dc.PushClip(new EllipseGeometry(clipRect));

        var expandedViewport = _viewportRect;
        expandedViewport.Inflate(MaskMapPointStatic.Width, MaskMapPointStatic.Height);

        var scaleX = side / _viewportRect.Width;
        var scaleY = side / _viewportRect.Height;
        var pointSide = Math.Max(8, Math.Min(16, side / 12.0));

        var showCollected = ShowCollectedPoints;

        // 通过网格索引只遍历视口附近的单元格，避免 O(N) 全量扫描
        var minCell = CellOf(expandedViewport.Left, expandedViewport.Top);
        var maxCell = CellOf(expandedViewport.Right, expandedViewport.Bottom);

        for (var cx = minCell.cx; cx <= maxCell.cx; cx++)
        {
            for (var cy = minCell.cy; cy <= maxCell.cy; cy++)
            {
                if (!_gridIndex.TryGetValue((cx, cy), out var cellPoints))
                    continue;

                foreach (var point in cellPoints)
                {
                    if (!expandedViewport.Contains(point.ImageX, point.ImageY)) continue;
                    if (point.IsCollected && !showCollected) continue;

                    var localX = clipRect.X + (point.ImageX - _viewportRect.X) * scaleX;
                    var localY = clipRect.Y + (point.ImageY - _viewportRect.Y) * scaleY;
                    DrawPoint(dc, point, localX, localY, pointSide, pointSide);
                }
            }
        }

        dc.Pop();
    }

    private void DrawPoint(DrawingContext dc, MaskMapPoint point, double centerX, double centerY, double width, double height)
    {
        var radius = width / 2.0;
        const double strokeThickness = 2.0;
        var circleCenter = new Point(centerX, centerY);

        var fillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#323947"));
        fillBrush.Freeze();
        var borderBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0xBC, 0x8E));
        borderBrush.Freeze();
        var borderPen = new Pen(borderBrush, strokeThickness);
        borderPen.Freeze();

        var shadowBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
        shadowBrush.Freeze();
        dc.DrawGeometry(shadowBrush, null,
            new EllipseGeometry(new Point(centerX + 2, centerY + 2), radius, radius));

        var circleGeometry = new EllipseGeometry(circleCenter, radius, radius);
        dc.DrawGeometry(fillBrush, borderPen, circleGeometry);

        if (_labelMap.TryGetValue(point.LabelId, out var label))
        {
            var image = MapIconImageCache.TryGet(label.IconUrl);
            if (image != null)
            {
                var imageRect = new Rect(circleCenter.X - radius, circleCenter.Y - radius, width, height);
                dc.PushClip(circleGeometry);
                dc.DrawImage(image, imageRect);
                dc.Pop();
            }
            else
            {
                _ = MapIconImageCache.GetAsync(label.IconUrl, CancellationToken.None);
                dc.DrawEllipse(GetColorBrush(label), null, circleCenter, width / 2.0, height / 2.0);
            }
        }
        else
        {
            var brush = new SolidColorBrush(GenerateRandomColor(point.Id));
            brush.Freeze();
            dc.DrawEllipse(brush, null, circleCenter, width / 2.0, height / 2.0);
        }
    }

    private Brush GetColorBrush(MaskMapPointLabel label)
    {
        if (_colorBrushCache.TryGetValue(label.LabelId, out var cached)) return cached;

        Color color = label.Color.HasValue
            ? Color.FromArgb(label.Color.Value.A, label.Color.Value.R, label.Color.Value.G, label.Color.Value.B)
            : GenerateRandomColor(label.LabelId);

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        _colorBrushCache[label.LabelId] = brush;
        return brush;
    }

    private static Color GenerateRandomColor(string seed)
    {
        var random = new Random(seed?.GetHashCode() ?? 0);
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
        if (newRect.Equals(_viewportRect)) return;
        _viewportRect = newRect;
        Refresh();
    }

    /// <summary>
    /// 返回视口统计：视口内点位数、扩展视口内点位数、最近的3个点位及其距离（坐标单位）
    /// </summary>
    public string GetViewportStats()
    {
        if (_viewportRect.IsEmpty)
            return "viewport=empty";

        var cx = _viewportRect.X + _viewportRect.Width / 2.0;
        var cy = _viewportRect.Y + _viewportRect.Height / 2.0;

        var inCount = 0;
        var expandedCount = 0;
        var expanded = _viewportRect;
        expanded.Inflate(64, 64);

        var nearest = new List<(double dist, double x, double y)>();

        // 利用网格索引只查询扩展视口覆盖的单元格
        var minCell = CellOf(expanded.Left, expanded.Top);
        var maxCell = CellOf(expanded.Right, expanded.Bottom);

        for (var gx = minCell.cx; gx <= maxCell.cx; gx++)
        {
            for (var gy = minCell.cy; gy <= maxCell.cy; gy++)
            {
                if (!_gridIndex.TryGetValue((gx, gy), out var cellPoints))
                    continue;

                foreach (var p in cellPoints)
                {
                    if (_viewportRect.Contains(p.ImageX, p.ImageY)) inCount++;
                    if (expanded.Contains(p.ImageX, p.ImageY)) expandedCount++;

                    var dx = p.ImageX - cx;
                    var dy = p.ImageY - cy;
                    var d = Math.Sqrt(dx * dx + dy * dy);

                    if (nearest.Count < 3)
                    {
                        nearest.Add((d, p.ImageX, p.ImageY));
                        if (nearest.Count == 3)
                            nearest.Sort((a, b) => a.dist.CompareTo(b.dist));
                    }
                    else if (d < nearest[2].dist)
                    {
                        nearest[2] = (d, p.ImageX, p.ImageY);
                        nearest.Sort((a, b) => a.dist.CompareTo(b.dist));
                    }
                }
            }
        }

        var nearestStr = string.Join(", ", nearest.Select(n =>
            $"({n.x:F0},{n.y:F0},d={n.dist:F0})"));

        return $"viewport={_viewportRect}, center=({cx:F0},{cy:F0}), " +
               $"points={_allPoints.Count}, in={inCount}, expanded={expandedCount}, " +
               $"size={ActualWidth:F0}x{ActualHeight:F0}, " +
               $"nearest=[{nearestStr}]";
    }

    public void Refresh() => RenderPoints();

    #endregion
}
