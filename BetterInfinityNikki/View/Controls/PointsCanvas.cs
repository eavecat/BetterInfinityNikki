using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BetterInfinityNikki.Model.MaskMap;

namespace BetterInfinityNikki.View.Controls;

/// <summary>
/// 高性能点位绘制控件，使用 DrawingVisual 实现轻量级渲染
/// </summary>
public class PointsCanvas : FrameworkElement
{
    private readonly VisualCollection _children;
    private readonly DrawingVisual _drawingVisual;
    private readonly Dictionary<string, Brush> _colorBrushCache;
    
    // 私有字段
    private ObservableCollection<MaskMapPoint>? _points;
    private List<MaskMapPoint> _allPoints = new();
    private Dictionary<string, MaskMapPointLabel> _labelMap = new();
    private Rect _viewportRect = Rect.Empty;

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
            typeof(IEnumerable<MaskMapPointLabel>),
            typeof(PointsCanvas),
            new PropertyMetadata(null, OnLabelsSourceChanged));

    /// <summary>
    /// 点位数据源
    /// </summary>
    public ObservableCollection<MaskMapPoint>? PointsSource
    {
        get => (ObservableCollection<MaskMapPoint>?)GetValue(PointsSourceProperty);
        set => SetValue(PointsSourceProperty, value);
    }

    /// <summary>
    /// 标签数据源
    /// </summary>
    public IEnumerable<MaskMapPointLabel>? LabelsSource
    {
        get => (IEnumerable<MaskMapPointLabel>?)GetValue(LabelsSourceProperty);
        set => SetValue(LabelsSourceProperty, value);
    }

    #endregion

    private static void OnPointsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (PointsCanvas)d;
        canvas.UpdatePoints(e.NewValue as ObservableCollection<MaskMapPoint>);
    }

    private static void OnLabelsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (PointsCanvas)d;
        canvas.UpdateLabels(e.NewValue as IEnumerable<MaskMapPointLabel>);
    }

    public PointsCanvas()
    {
        _children = new VisualCollection(this);
        _drawingVisual = new DrawingVisual();
        _children.Add(_drawingVisual);
        _colorBrushCache = new Dictionary<string, Brush>();

        // 启用命中测试
        IsHitTestVisible = true;
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
        // 点位属性变化时重绘
        Refresh();
    }

    #endregion

    #region 渲染逻辑

    /// <summary>
    /// 渲染所有点位
    /// </summary>
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

        // 扩展可视区域，避免边缘闪烁
        var expandedViewport = _viewportRect;
        expandedViewport.Inflate(MaskMapPointStatic.Width, MaskMapPointStatic.Height);

        var scaleX = aw / _viewportRect.Width;
        var scaleY = ah / _viewportRect.Height;

        foreach (var point in _allPoints)
        {
            // 检查点是否在扩展的可视区域内
            if (!expandedViewport.Contains(point.ImageX, point.ImageY))
            {
                continue;
            }

            // 计算屏幕坐标
            var localX = (point.ImageX - _viewportRect.X) * scaleX;
            var localY = (point.ImageY - _viewportRect.Y) * scaleY;

            // 绘制点位
            DrawPoint(dc, point, localX, localY, MaskMapPointStatic.Width, MaskMapPointStatic.Height);
        }
    }

    /// <summary>
    /// 绘制单个点位
    /// </summary>
    private void DrawPoint(DrawingContext dc, MaskMapPoint point, double centerX, double centerY, double width, double height)
    {
        double radius = width / 2.0;
        double strokeThickness = 2.0;

        Point circleCenter = new Point(centerX, centerY);

        // 填充颜色 #323947
        var fillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#323947"));
        fillBrush.Freeze();

        // 边框颜色 #D3BC8E
        var borderBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0xBC, 0x8E));
        borderBrush.Freeze();

        var borderPen = new Pen(borderBrush, strokeThickness);
        borderPen.Freeze();

        // 阴影效果
        var shadowBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
        shadowBrush.Freeze();

        var shadowOffset = new Point(2, 2);

        // 绘制圆形阴影
        var shadowCircleGeometry = new EllipseGeometry(
            new Point(circleCenter.X + shadowOffset.X, circleCenter.Y + shadowOffset.Y),
            radius, radius);
        dc.DrawGeometry(shadowBrush, null, shadowCircleGeometry);

        // 绘制圆形
        var circleGeometry = new EllipseGeometry(circleCenter, radius, radius);
        dc.DrawGeometry(fillBrush, borderPen, circleGeometry);

        // 如果有标签信息，尝试绘制图标或颜色
        if (_labelMap.TryGetValue(point.LabelId, out var label))
        {
            var brush = GetColorBrush(label);
            dc.DrawEllipse(brush, null, circleCenter, width / 2.0, height / 2.0);
        }
        else
        {
            // 没有标签信息，绘制默认随机颜色圆点
            var color = GenerateRandomColor(point.Id);
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            dc.DrawEllipse(brush, null, circleCenter, width / 2.0, height / 2.0);
        }
    }

    /// <summary>
    /// 获取颜色画刷（带缓存）
    /// </summary>
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

    /// <summary>
    /// 根据字符串生成一致的随机颜色
    /// </summary>
    private Color GenerateRandomColor(string seed)
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

    /// <summary>
    /// 更新视口区域
    /// </summary>
    /// <param name="x">视口左上角 X 坐标</param>
    /// <param name="y">视口左上角 Y 坐标</param>
    /// <param name="width">视口宽度</param>
    /// <param name="height">视口高度</param>
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

    /// <summary>
    /// 刷新渲染
    /// </summary>
    public void Refresh()
    {
        RenderPoints();
    }

    #endregion
}
