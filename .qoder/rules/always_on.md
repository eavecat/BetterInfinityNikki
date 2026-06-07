# BetterGI 项目开发规范（始终生效）

## 项目概述

BetterGI (Better Genshin Impact) 是一个基于计算机视觉技术的《原神》游戏辅助工具，通过图像识别和模拟操作实现自动化任务。

### 技术栈
- **框架**: .NET 8.0 (Windows 10.0.22621.0)
- **UI**: WPF + WPF-UI 4.3.0
- **MVVM**: CommunityToolkit.Mvvm 8.2.2
- **图像处理**: OpenCvSharp4 4.11.0
- **AI/ML**: Microsoft.ML.OnnxRuntime, YoloSharp, TorchSharp
- **OCR**: PaddleOCR (Sdcb.PaddleOCR)
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

<!-- 使用 Behaviors 处理事件（符合 MVVM） -->
<i:Interaction.Triggers>
    <i:EventTrigger EventName="Loaded">
        <i:InvokeCommandAction Command="{Binding LoadedCommand}" />
    </i:EventTrigger>
</i:Interaction.Triggers>
```

**推荐使用的交互方式:**
- ✅ 使用 `Microsoft.Xaml.Behaviors.Wpf` 处理事件
- ✅ 使用 `WPF-UI` 提供的现代化控件
- ✅ 使用 `{Binding}` 进行数据绑定
- ❌ 避免在 XAML 中编写复杂逻辑
- ❌ 避免在 Code-behind 中编写业务逻辑

### 2. 依赖注入规范

**服务注册 (App.xaml.cs):**
```csharp
// 单例服务
services.AddSingleton<IConfigService, ConfigService>();
services.AddSingleton<TaskTriggerDispatcher>();

// 视图和 ViewModel 配对注册
services.AddView<HomePage, HomePageViewModel>();
services.AddView<ScriptControlPage, ScriptControlViewModel>();

// 宿主服务（后台运行）
services.AddHostedService<ApplicationHostService>();
```

**服务获取:**
```csharp
// 在 ViewModel 中通过构造函数注入
public class MyViewModel : ViewModel
{
    private readonly IConfigService _configService;
    
    public MyViewModel(IConfigService configService)
    {
        _configService = configService;
    }
}

// 静态获取（仅在必要时使用）
var service = App.GetService<IConfigService>();
```

### 3. 配置管理规范

**配置类定义:**
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MyConfig : ObservableObject
{
    [ObservableProperty]
    private bool _enabled = false;
    
    [ObservableProperty]
    private int _timeout = 3000;
}
```

**配置持久化:**
- 使用 `Newtonsoft.Json` 序列化配置
- 配置文件保存在 `User/` 目录
- 配置更改后自动保存到 JSON 文件

### 4. 图像识别规范

**识别对象定义:**
```csharp
using BetterGenshinImpact.Core.Recognition;
using OpenCvSharp;

// 模板匹配识别
var recognitionObject = new RecognitionObject
{
    Name = "确认按钮",
    RecognitionType = RecognitionTypes.TemplateMatch,
    TemplateImageMat = Cv2.ImRead("Assets/confirm.png"),
    RegionOfInterest = new Rect(0, 0, 1920, 1080),
    Threshold = 0.8
};

// OCR 识别
var ocrObject = new RecognitionObject
{
    Name = "文本识别",
    RecognitionType = RecognitionTypes.Ocr,
    OcrEngine = OcrEngineTypes.Paddle,
    RegionOfInterest = new Rect(100, 100, 500, 200)
};
```

**注意事项:**
- ✅ 识别资源应在 Assets 类中统一管理
- ✅ 使用 `BaseAssets<T>` 作为资产基类
- ✅ 模板图片放在对应任务的 `Assets/1920x1080/` 目录
- ❌ 不要在每次识别时重新加载图片
- ❌ 不要忘记释放 `Mat` 对象资源

### 5. 输入模拟规范

**键盘模拟:**
```csharp
using BetterGenshinImpact.Core.Simulator;

// 按键点击（按下+释放）
Simulation.SendInput.Keyboard.KeyPress(VirtualKeyCode.Escape);

// 组合键
Simulation.SendInput.Keyboard.KeyDown(VirtualKeyCode.LControl);
Simulation.SendInput.Keyboard.KeyPress(VirtualKeyCode.C);
Simulation.SendInput.Keyboard.KeyUp(VirtualKeyCode.LControl);
```

**鼠标模拟:**
```csharp
// 移动鼠标
Simulation.SendInput.Mouse.MoveMouseTo(x, y);

// 左键点击
Simulation.SendInput.Mouse.LeftButtonClick();
```

**注意事项:**
- ✅ 优先使用 `PostMessageSimulator` 进行后台模拟
- ✅ 使用 `Simulation.SendInput` 统一接口
- ✅ 模拟操作后添加适当延迟 (`Task.Delay`)
- ❌ 避免过于频繁的模拟操作

### 6. 日志记录规范

**使用 Serilog:**
```csharp
using Microsoft.Extensions.Logging;

public class MyClass
{
    private readonly ILogger<MyClass> _logger;
    
    public MyClass(ILogger<MyClass> logger)
    {
        _logger = logger;
    }
    
    public void DoWork()
    {
        _logger.LogDebug("调试信息");
        _logger.LogInformation("普通信息");
        _logger.LogWarning("警告信息");
        _logger.LogError("错误信息");
        
        try
        {
            // 可能抛出异常的代码
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发生异常: {Message}", ex.Message);
        }
    }
}
```

### 7. 异步编程规范

**异步方法:**
```csharp
// 正确的异步方法签名
public async Task<string> GetDataAsync(CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();
    var result = await httpClient.GetStringAsync(url, ct);
    return result;
}

// 异步命令
[RelayCommand]
private async Task ExecuteAsync()
{
    await DoWorkAsync(CancellationToken.None);
}
```

**注意事项:**
- ✅ 异步方法以 `Async` 结尾
- ✅ 传递 `CancellationToken` 支持取消
- ❌ 避免 `async void`（除事件处理器外）
- ❌ 避免 `.Result` 或 `.Wait()`（会导致死锁）

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

### 3. 资源管理规范

**资源释放:**
```csharp
// Mat 对象必须释放
using var mat = new Mat();
// 或使用 try-finally
try
{
    var mat = new Mat();
    // 使用 mat
}
finally
{
    mat?.Dispose();
}
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

### 3. 性能优化

**图像识别优化:**
```csharp
// ✅ 减少识别区域
RegionOfInterest = new Rect(100, 100, 200, 200)  // 而不是全屏

// ✅ 缓存 Mat 对象
private Mat? _cachedImage;

// ✅ 使用异步操作避免阻塞 UI
await Task.Run(() => ProcessImage(image));
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
