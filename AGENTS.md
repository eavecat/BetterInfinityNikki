本项目使用了 WPF-UI、 CommunityToolkit.Mvvm、Microsoft.Xaml.Behaviors.Wpf 来实现 MVVM 架构。在编写代码的时候请注意：

### 主要依赖框架
#### UI 框架
- **WPF-UI (4.0.2)** - 现代化 WPF UI 框架
- **gong-wpf-dragdrop(3.2.1)** - 拖拽框架

#### MVVM 框架
- **CommunityToolkit.Mvvm (8.2.2)** - 微软官方 MVVM 工具包
  - 所有 ViewModel 必须继承自 `ObservableObject`
  - 使用 `[ObservableProperty]` 特性自动生成属性
  - 使用 `[RelayCommand]` 特性自动生成命令
- **Microsoft.Xaml.Behaviors.Wpf(1.1.122)** - WPF 行为扩展库
  - 请尽量使用 Behaviors 库来实现交互，避免不符合 MVVM 规范的交互事件触发方式。

### 其他框架使用要求
1. 请优先使用 Newtonsoft.Json 作为json序列化工具，但是如果这个模型已经被System.Text.Json序列化过了，那么就直接使用System.Text.Json反序列化。
2. 所有简单的对话框弹出需求优先使用 ThemedMessageBox 弹出。而不是 WPF 自带的 MessageBox。

## MVVM 架构规则

### 基础架构

### ViewModel 编写规范

1. **继承规则**
   ```csharp
   public partial class ExampleViewModel : ViewModel
   {
       [ObservableProperty]
       private string _title = "";
       
       [RelayCommand]
       private void DoSomething()
       {
           // 实现逻辑
       }
   }
   ```

2. **属性命名**
   - 私有字段使用下划线前缀: `_fieldName`
   - 公共属性使用 PascalCase: `PropertyName`
   - 使用 `[ObservableProperty]` 自动生成属性

3. **命令实现**
   - 使用 `[RelayCommand]` 特性
   - 异步命令使用 `[RelayCommand]` + `async Task`

### View 编写规范

1. **代码后置**
   ```csharp
   public partial class ExamplePage : UserControl
   {
       public ExampleViewModel ViewModel { get; }
       
       public ExamplePage(ExampleViewModel viewModel)
       {
           ViewModel = viewModel;
           DataContext = this;
           InitializeComponent();
       }
   }
   ```

2. **XAML 绑定**
   - 使用 `{Binding}` 语法绑定 ViewModel 属性
   - 命令绑定: `Command="{Binding ExampleCommand}"`
   - 避免在 XAML 中编写复杂逻辑

### 依赖注入规范

1. **服务注册**
   ```csharp
   // 在 App.xaml.cs 中注册
   services.AddView<ExamplePage, ExampleViewModel>();
   services.AddSingleton<IExampleService, ExampleService>();
   ```

### 配置系统规则

1. **配置模型定义**
   - 所有配置类继承 `ObservableObject`，使用 `[ObservableProperty]` 定义属性
   - 嵌套配置类（如 `OtherConfig.Miyoushe`）必须定义为宿主配置类的 `public partial class` 嵌套类
   - 在宿主类中使用 `[ObservableProperty]` 持有嵌套配置实例：
     ```csharp
     [ObservableProperty]
     private MyNestedConfig _myNestedConfig = new();
     ```

2. **配置持久化（关键）**
   - 配置使用 `System.Text.Json` 序列化，采用 `CamelCase` 命名策略
   - `AllConfig.InitEvent()` 中必须为每个需要自动保存的子配置订阅 `PropertyChanged`：
     ```csharp
     // 直接属性变更
     OtherConfig.PropertyChanged += OnAnyPropertyChanged;
     // 嵌套对象的属性变更也需要单独订阅，否则嵌套属性修改不会触发保存
     OtherConfig.MyNestedConfig.PropertyChanged += OnAnyPropertyChanged;
     ```
   - 如果嵌套配置的属性变更未在此订阅，用户修改后不会自动保存到 `config.json`

3. **设置页面必须有独立 ViewModel**
   - 设置页面不能依赖从 MainWindow 继承 DataContext，必须创建独立的 ViewModel
   - ViewModel 通过 `IConfigService` 注入获取 `Config`：
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
   - Page 代码后置中设置 DataContext：
     ```csharp
     public ExampleSettingsPage(ExampleSettingsPageViewModel viewModel)
     {
         DataContext = ViewModel = viewModel;
         InitializeComponent();
     }
     ```
   - 在 `App.xaml.cs` 中使用 `AddView` 注册：
     ```csharp
     services.AddView<ExampleSettingsPage, ExampleSettingsPageViewModel>();
     ```

4. **TextBox 绑定配置项**
   - TextBox 绑定配置属性时必须加 `UpdateSourceTrigger=PropertyChanged`，否则默认 `LostFocus` 模式下用户未移出焦点时值不会推送：
     ```xml
     <ui:TextBox Text="{Binding Config.OtherConfig.MyNestedConfig.Token, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
     ```

最后，程序能够编译就认为成功，无需实际运行程序。

编译指令参考，如果出现程序占用场景，直接放弃编译验证即可
```
dotnet build BetterGenshinImpact.sln -c Debug
```