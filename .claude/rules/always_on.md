# BetterGI 项目开发规范（始终生效）

## 项目概述

BetterGI (Better Genshin Impact) 是一个基于计算机视觉技术的《原神》游戏辅助工具，通过图像识别和模拟操作实现自动化任务。

### 技术栈
- **框架**: .NET 8.0 (Windows 10.0.22621.0)
- **UI**: WPF + WPF-UI 4.3.0
- **MVVM**: CommunityToolkit.Mvvm 8.2.2
- **图像处理**: OpenCvSharp4 4.11.0
- **AI/ML**: Microsoft.ML.OnnxRuntime, YoloSharp, TorchSharp
- **OCR**: Sdcb.PaddleOCR
- **脚本引擎**: Microsoft.ClearScript.V8 (JavaScript)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **日志**: Serilog
- **JSON**: Newtonsoft.Json (优先), System.Text.Json

---

## 核心编码规范

### 1. MVVM 架构规则

#### ViewModel 编写规范

**必须遵循:**
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class ExampleViewModel : ViewModel
{
    // 可观察属性 - 自动生成属性通知
    [ObservableProperty]
    private string _title = "";
    
    [ObservableProperty]
    private bool _isEnabled = false;
    
    // 同步命令
    [RelayCommand]
    private void DoSomething()
    {
        // 业务逻辑
    }
    
    // 异步命令
    [RelayCommand]
    private async Task DoSomethingAsync()
    {
        await Task.Delay(100);
    }
}
```

**命名规范:**
- 私有字段: `_camelCase` (下划线前缀)
- 公共属性: `PascalCase` (由 `[ObservableProperty]` 自动生成)
- 命令方法: `动词开头`, 如 `StartTask`, `StopRecording`
- ViewModel 类名: `{功能名}ViewModel`

**禁止事项:**
- ❌ 不要手动实现 `INotifyPropertyChanged`
- ❌ 不要在 ViewModel 中直接操作 UI 控件
- ❌ 不要继承 `ObservableObject`，应继承项目基类 `ViewModel`

#### View 编写规范

**代码后置 (Code-behind):**
```csharp
public partial class ExamplePage : UserControl
{
    public ExampleViewModel ViewModel { get; }
    
    public ExamplePage(ExampleViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;  // 注意：DataContext 设置为 this
        InitializeComponent();
    }
}
```

**XAML 绑定:**
```xml
<!-- 属性绑定 -->
<TextBlock Text="{Binding Title}" />
<CheckBox IsChecked="{Binding IsEnabled}" />

<!-- 命令绑定 -->
<Button Content="执行" Command="{Binding DoSomethingCommand}" />
```

---

## 项目特定规范

### 1. 对话框使用规范

**优先使用 ThemedMessageBox:**
```csharp
using Wpf.Ui.Violeta.Controls;

// 信息提示
ThemedMessageBox.Show("操作成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

// 确认对话框
var result = ThemedMessageBox.Show("确定要删除吗？", "确认", 
    MessageBoxButton.YesNo, MessageBoxImage.Question);
```

**禁止使用:**
- ❌ 不要使用 `System.Windows.MessageBox`

### 2. JSON 序列化规范

**优先使用 Newtonsoft.Json:**
```csharp
using Newtonsoft.Json;

// 序列化
var json = JsonConvert.SerializeObject(obj, Formatting.Indented);

// 反序列化
var obj = JsonConvert.DeserializeObject<MyClass>(json);
```

**特殊情况使用 System.Text.Json:**
```csharp
// 仅当对象已被 System.Text.Json 序列化过时使用
using System.Text.Json;
var obj = JsonSerializer.Deserialize<MyClass>(json);
```

---

## 编译和构建

### 开发环境要求
- **IDE**: Visual Studio 2022 或 Rider（推荐）
- **.NET SDK**: .NET 8.0
- **Windows SDK**: 10.0.22621.0 或更高
- **平台**: x64

### 编译命令
```bash
# Debug 模式
dotnet build BetterGenshinImpact.sln -c Debug

# Release 模式
dotnet build BetterGenshinImpact.sln -c Release
```

### 注意事项
- ✅ 程序能够编译即认为成功，无需实际运行
- ✅ 如果出现程序占用，放弃编译验证
- ❌ 不要提交 `bin/` 和 `obj/` 目录

---

## 常见陷阱和最佳实践

### 1. 线程安全
**UI 线程操作:**
```csharp
// 在 ViewModel 中更新 UI 绑定的属性是安全的（CommunityToolkit 自动处理）
[ObservableProperty]
private string _status = "";
```

### 2. 内存泄漏预防
**资源释放:**
```csharp
// 实现 IDisposable
public class MyResource : IDisposable
{
    private bool _disposed = false;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
            }
            _disposed = true;
        }
    }
}
```

---

## 代码审查清单

在提交代码前，请检查以下事项：

### 功能性
- [ ] 代码能够成功编译
- [ ] 实现了需求功能
- [ ] 没有引入明显的 bug

### 代码质量
- [ ] 遵循 MVVM 架构
- [ ] 使用了 `[ObservableProperty]` 和 `[RelayCommand]`
- [ ] 变量命名清晰、符合规范
- [ ] 添加了必要的注释
- [ ] 没有重复代码

### 性能
- [ ] 正确释放了资源（Mat、Stream 等）
- [ ] 避免了不必要的对象创建
- [ ] 使用了异步操作处理耗时任务

### 安全性
- [ ] 正确处理了异常
- [ ] 没有硬编码敏感信息
- [ ] 输入验证完整

### 可维护性
- [ ] 代码易于理解和修改
- [ ] 遵循单一职责原则
- [ ] 依赖注入使用正确