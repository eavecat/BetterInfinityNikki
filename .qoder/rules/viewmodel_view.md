# C# ViewModel 和 View 规范

## ViewModel 编写规范

### 必须遵守的规则

1. **继承规则**
   - 所有 ViewModel 必须继承自 `ViewModel` 基类（不是直接继承 `ObservableObject`）
   - 使用 `public partial class` 声明

2. **属性定义**
   ```csharp
   // ✅ 正确写法
   [ObservableProperty]
   private string _title = "";
   
   [ObservableProperty]
   private bool _isEnabled = false;
   ```
   
   - 私有字段使用下划线前缀：`_fieldName`
   - 使用 `[ObservableProperty]` 特性，不要手动实现属性
   - 公共属性会自动生成为 PascalCase 格式

3. **命令定义**
   ```csharp
   // ✅ 同步命令
   [RelayCommand]
   private void DoSomething()
   {
       // 业务逻辑
   }
   
   // ✅ 异步命令
   [RelayCommand]
   private async Task DoSomethingAsync()
   {
       await Task.Delay(100);
   }
   ```
   
   - 使用 `[RelayCommand]` 特性
   - 命令方法名以动词开头：`Start`, `Stop`, `Execute`, `Save` 等
   - 异步命令返回 `Task`

4. **禁止事项**
   - ❌ 不要手动实现 `INotifyPropertyChanged`
   - ❌ 不要在 ViewModel 中直接操作 UI 控件
   - ❌ 不要继承 `ObservableObject`，应继承 `ViewModel`

### ViewModel 完整示例

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.ViewModel.Pages
{
    public partial class ExampleViewModel : ViewModel
    {
        private readonly ILogger<ExampleViewModel> _logger;
        private readonly IConfigService _configService;
        
        [ObservableProperty]
        private string _title = "示例页面";
        
        [ObservableProperty]
        private bool _isLoading = false;
        
        public ExampleViewModel(ILogger<ExampleViewModel> logger, IConfigService configService)
        {
            _logger = logger;
            _configService = configService;
        }
        
        [RelayCommand]
        private async Task ExecuteAsync()
        {
            try
            {
                IsLoading = true;
                await DoWorkAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行失败");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

---

## View 代码后置规范

### 标准写法

```csharp
using System.Windows.Controls;
using BetterGenshinImpact.ViewModel.Pages;

namespace BetterGenshinImpact.View.Pages
{
    public partial class ExamplePage : UserControl
    {
        public ExampleViewModel ViewModel { get; }
        
        public ExamplePage(ExampleViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;  // 重要：DataContext 设置为 this
            InitializeComponent();
        }
    }
}
```

### 禁止事项
- ❌ 不要在 Code-behind 中编写业务逻辑
- ❌ 不要直接操作其他 ViewModel
- ❌ 不要忘记设置 `DataContext = this`

---

## 设置页面 ViewModel 模式

设置页面（需要绑定 `Config` 属性的页面）必须有独立 ViewModel，**不能依赖从 MainWindow 继承 DataContext**：

### ViewModel
```csharp
public partial class ExampleSettingsPageViewModel : ViewModel
{
    public ExampleSettingsPageViewModel(IConfigService configService)
    {
        Config = configService.Get();
    }
    public AllConfig Config { get; set; }
}
```

### Code-behind
```csharp
public partial class ExampleSettingsPage : Page
{
    private ExampleSettingsPageViewModel ViewModel { get; }

    public ExampleSettingsPage(ExampleSettingsPageViewModel viewModel)
    {
        DataContext = ViewModel = viewModel;  // DataContext 设为 ViewModel 本身
        InitializeComponent();
    }
}
```

### DI 注册
```csharp
services.AddView<ExampleSettingsPage, ExampleSettingsPageViewModel>();
```

### XAML 绑定
```xml
<!-- 通过 ViewModel 的 Config 属性访问配置 -->
<ui:ToggleSwitch IsChecked="{Binding Config.CommonConfig.ExitToTray}" />
<ui:TextBox Text="{Binding Config.OtherConfig.MyFeature.Token, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

---

## TextBox 绑定配置项

TextBox 绑定配置属性时**必须加 `UpdateSourceTrigger=PropertyChanged`**，否则默认 `LostFocus` 模式下用户未移出焦点时值不会推送到源：
```xml
<!-- ✅ 正确：即时推送 -->
<ui:TextBox Text="{Binding Config.OtherConfig.MyFeature.Token, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- ❌ 错误：需要失去焦点才推送，可能导致配置未保存 -->
<ui:TextBox Text="{Binding Config.OtherConfig.MyFeature.Token, Mode=TwoWay}" />
```

---

## XAML 绑定规范

### 基本绑定
```xml
<!-- 单向绑定（默认） -->
<TextBlock Text="{Binding Title}" />

<!-- 双向绑定 -->
<TextBox Text="{Binding InputText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- 命令绑定 -->
<Button Content="执行" Command="{Binding ExecuteCommand}" />
```

### 事件转命令（Behaviors）
```xml
xmlns:i="http://schemas.microsoft.com/xaml/behaviors"

<i:Interaction.Triggers>
    <i:EventTrigger EventName="Loaded">
        <i:InvokeCommandAction Command="{Binding LoadedCommand}" />
    </i:EventTrigger>
</i:Interaction.Triggers>
```

### 禁止事项
- ❌ 不要在 XAML 中编写复杂逻辑
- ❌ 不要使用 Code-behind 事件处理器（除非必要）
