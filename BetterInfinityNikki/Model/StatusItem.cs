using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using Wpf.Ui.Controls;

namespace BetterInfinityNikki.Model;

/// <summary>
/// 遮罩窗口状态项（与 BGI 实现一致，自动绑定配置属性）
/// </summary>
public partial class StatusItem : ObservableObject
{
    public string Name { get; set; }
    public SymbolRegular Symbol { get; set; }
    private INotifyPropertyChanged _sourceObject { get; set; }
    private string _propertyName { get; set; }

    [ObservableProperty] private bool _isEnabled;

    /// <summary>
    /// 创建状态项，自动与配置属性绑定
    /// </summary>
    /// <param name="name">显示名称</param>
    /// <param name="symbol">图标</param>
    /// <param name="sourceObject">配置对象</param>
    /// <param name="propertyName">属性名称</param>
    public StatusItem(string name, SymbolRegular symbol, INotifyPropertyChanged sourceObject, string propertyName = "Enabled")
    {
        Name = name;
        Symbol = symbol;
        _sourceObject = sourceObject;
        _propertyName = propertyName;

        _sourceObject.PropertyChanged += OnSourcePropertyChanged;
        IsEnabled = GetSourceValue();
    }

    private bool GetSourceValue()
    {
        var property = _sourceObject.GetType().GetProperty(_propertyName);
        ArgumentNullException.ThrowIfNull(property);
        var value = property.GetValue(_sourceObject);
        ArgumentNullException.ThrowIfNull(value);
        return (bool)value;
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyName)
        {
            var newValue = GetSourceValue();
            IsEnabled = newValue;
        }
    }
}
