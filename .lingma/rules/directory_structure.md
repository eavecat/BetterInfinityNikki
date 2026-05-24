# 代码目录结构规范

## 项目整体结构

```
BetterGenshinImpact/
├── Core/                    # 核心功能模块（不依赖 UI）
├── GameTask/               # 游戏任务模块（自动化功能）
├── View/                   # 视图层（XAML 界面）
├── ViewModel/              # 视图模型层（MVVM）
├── Service/                # 服务层（业务逻辑）
├── Model/                  # 数据模型
├── Helpers/                # 辅助工具类
├── Genshin/                # 原神特定配置
├── Hutao/                  # 胡桃相关功能
├── Markup/                 # XAML 标记扩展
├── Resources/              # 资源文件（图片、字体等）
├── User/                   # 用户数据和配置
├── Wine/                   # Wine 平台适配
├── App.xaml                # 应用程序入口
├── App.xaml.cs             # 应用程序逻辑
└── GlobalUsing.cs          # 全局 using 声明
```

---

## Core 核心模块

**职责**: 提供底层功能，不依赖 UI 层

```
Core/
├── Config/                 # 配置管理
├── Recognition/            # 图像识别（OCR、ONNX、OpenCV）
├── Simulator/              # 输入模拟
├── Script/                 # JavaScript 脚本引擎
├── Recorder/               # 键鼠录制
└── Monitor/                # 输入监控
```

### 命名规范
- **Config/**: 所有配置类以 `Config` 结尾
- **Recognition/**: 识别器以 `Recognizer` 或 `Service` 结尾
- **Simulator/**: 模拟器以 `Simulator` 结尾

---

## GameTask 游戏任务模块

**职责**: 实现各种自动化游戏任务

### 主要任务模块
- `AutoFight/` - 自动战斗
- `AutoPathing/` - 自动路径追踪
- `AutoFishing/` - 自动钓鱼
- `AutoSkip/` - 自动跳过剧情
- `AutoDomain/` - 自动秘境
- `AutoWood/` - 自动伐木
- `AutoPick/` - 自动拾取
- `AutoCook/` - 自动烹饪
- `Common/` - 通用任务组件

### 任务模块标准结构

每个独立任务应包含：

```
Auto{TaskName}/
├── Assets/                 # 识别资源（模板图片等）
│   └── 1920x1080/          # 分辨率特定资源
├── Model/                  # 任务相关模型（可选）
├── Config/                 # 任务配置（可选）
├── {TaskName}Task.cs       # 任务主类（ISoloTask）
├── {TaskName}Trigger.cs    # 触发器（ITaskTrigger，可选）
├── {TaskName}Config.cs     # 配置类
└── {TaskName}Param.cs      # 参数类（可选）
```

### 命名规范
- **任务类**: `{TaskName}Task.cs`（实现 `ISoloTask`）
- **触发器**: `{TaskName}Trigger.cs`（实现 `ITaskTrigger`）
- **配置类**: `{TaskName}Config.cs`（继承 `ObservableObject`）
- **参数类**: `{TaskName}Param.cs`（继承 `BaseTaskParam`）
- **Assets 类**: `{TaskName}Assets.cs`（继承 `BaseAssets<T>`）

---

## View 视图层

**职责**: UI 界面定义（XAML）

```
View/
├── Pages/                  # 页面控件
├── Windows/                # 窗口
├── Dialogs/                # 对话框
├── Controls/               # 自定义控件
└── Converters/             # 值转换器
```

### 命名规范
- **页面**: `{Name}Page.xaml`
- **窗口**: `{Name}Window.xaml`
- **对话框**: `{Name}Dialog.xaml`

---

## ViewModel 视图模型层

**职责**: 连接 View 和 Service，实现 MVVM

```
ViewModel/
├── Pages/                  # 页面对应的 ViewModel
├── Windows/                # 窗口的 ViewModel
└── Dialogs/                # 对话框的 ViewModel
```

### 命名规范
- **ViewModel**: `{Name}ViewModel.cs`
- **必须继承**: `ViewModel` 基类
- **使用特性**: `[ObservableProperty]`, `[RelayCommand]`

---

## Service 服务层

**职责**: 业务逻辑和数据访问

```
Service/
├── Interface/              # 服务接口（I{Name}Service.cs）
├── ConfigService.cs        # 配置服务
├── ScriptService.cs        # 脚本服务
├── UpdateService.cs        # 更新服务
├── Notification/           # 通知服务
└── Tavern/                 # 酒馆 API 服务
```

### 命名规范
- **接口**: `I{Name}Service.cs`
- **实现**: `{Name}Service.cs`
- **注册方式**: 在 `App.xaml.cs` 中通过依赖注入注册

---

## Model / Helpers / Resources

### Model 数据模型
- **位置**: `Model/`
- **命名**: 使用名词，如 `CombatAvatar`, `FishType`
- **枚举**: `{Name}Enum.cs` 或 `{Name}Type.cs`

### Helpers 辅助工具
- **位置**: `Helpers/`
- **扩展方法**: `{Name}Extension.cs`
- **工具类**: `{Name}Helper.cs` 或 `{Name}Utils.cs`

### Resources 资源文件
- **位置**: `Resources/`
- **图片**: `Images/*.png`, `Images/*.jpg`
- **字体**: `Fonts/*.ttf`
- **注意**: 在 `.csproj` 中设置为 `<Resource>`

### User 用户数据
- **位置**: `User/`
- **配置**: `Config/*.json`
- **国际化**: `I18n/*.json`
- **注意**: 不应提交到 Git

---

## 新模块创建指南

### 创建新的游戏任务模块

1. **在 GameTask/ 下创建文件夹**
   ```
   GameTask/AutoNewTask/
   ```

2. **创建标准文件结构**
   ```
   AutoNewTask/
   ├── Assets/
   │   └── 1920x1080/
   ├── AutoNewTaskTask.cs      # 实现 ISoloTask
   ├── AutoNewTaskConfig.cs    # 配置类
   └── AutoNewTaskAssets.cs    # 资源类
   ```

3. **实现任务类**
   ```csharp
   public class AutoNewTaskTask : ISoloTask
   {
       public string Name => "新任务";
       
       public async Task RunAsync(CancellationToken ct)
       {
           // 任务逻辑
       }
   }
   ```

4. **在 App.xaml.cs 中注册（如需要）**
   ```csharp
   services.AddSingleton<AutoNewTaskTask>();
   ```

5. **创建对应的 ViewModel 和 View（如需要 UI）**
   ```
   ViewModel/Pages/NewTaskViewModel.cs
   View/Pages/NewTaskPage.xaml
   ```

6. **在 App.xaml.cs 中注册页面**
   ```csharp
   services.AddView<NewTaskPage, NewTaskViewModel>();
   ```

---

## 目录命名约定

### ✅ 推荐的命名方式

- **单数形式**: `Model/`, `Service/`, `View/`
- **复数形式**: `Helpers/`, `Resources/`, `Assets/`
- **PascalCase**: 所有目录使用 PascalCase
- **简洁明了**: 避免过长的目录名

### ❌ 避免的命名方式

- 不要使用缩写（除非是广泛认可的，如 `UI`, `API`）
- 不要使用中文目录名
- 不要使用特殊字符或空格
- 不要使用下划线分隔（使用 PascalCase）

---

## 文件组织原则

### 1. 单一职责
每个目录应该有明确的职责范围：
- `Core/` - 核心功能
- `GameTask/` - 游戏任务
- `View/` - 界面
- `ViewModel/` - 视图模型
- `Service/` - 业务逻辑

### 2. 高内聚低耦合
- 相关功能放在同一目录
- 减少跨目录依赖
- 使用接口解耦

### 3. 易于查找
- 按功能分组
- 按类型分组
- 保持一致的命名

### 4. 可扩展性
- 预留扩展空间
- 避免硬编码路径
- 使用相对路径

---

## 代码审查清单

在添加新文件或目录时，请确认：

- [ ] 目录位置符合职责划分
- [ ] 文件名遵循命名规范
- [ ] 文件放在正确的目录中
- [ ] 没有重复的功能模块
- [ ] 目录结构清晰易懂
- [ ] 必要时更新了 `.csproj` 文件
- [ ] 添加了必要的注释说明

---

## 常见问题

### Q: 如何决定文件放在哪个目录？
A: 根据文件的职责：
- UI 相关 → `View/`
- 数据绑定 → `ViewModel/`
- 业务逻辑 → `Service/` 或 `GameTask/`
- 底层功能 → `Core/`
- 工具类 → `Helpers/`

### Q: 可以创建子目录吗？
A: 可以，但要保持层次不超过 3 层。如果超过，考虑重构。

### Q: 如何处理跨模块的共享代码？
A: 放在 `Common/` 或 `Helpers/` 目录，或使用接口解耦。

### Q: Assets 应该放在哪里？
A: 
- 任务特定的 Assets → `GameTask/{TaskName}/Assets/`
- 通用 Assets → `Core/Recognition/Assets/`

---

## 参考示例

查看现有模块的结构作为参考：
- `GameTask/AutoFight/` - 完整的任务模块示例
- `Core/Recognition/` - 核心功能模块示例
- `ViewModel/Pages/` - ViewModel 组织示例
