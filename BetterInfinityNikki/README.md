# BetterIN - 更好的无限暖暖

Better Infinity Nikki 的自动化辅助工具，采用与 BetterGI 相同的架构和视觉风格。

## ✨ 主要功能

- 🎮 **游戏联动启动**：自动检测并启动无限暖暖游戏
- 📸 **多种截图模式**：支持 BitBlt、WindowsGraphicsCapture 等多种捕获方式
- 🎨 **现代化 UI**：基于 WPF-UI 的 Fluent Design 设计
- 🌙 **主题切换**：支持 6 种主题（Light、Dark、HighContrast 等）
- ⚙️ **灵活配置**：可自定义触发器间隔、截图模式等参数
- 🔍 **窗口管理**：自动查找游戏窗口，支持手动选择

## 📖 快速开始

### 1. 配置游戏路径

首次使用前，需要配置无限暖暖的安装路径：

1. 打开应用，进入“启动”页面
2. 展开“同时启动无限暖暖”选项
3. 点击“选择游戏路径”按钮
4. 选择 `InfinityNikki.exe` 文件

### 2. 启动截图器

有两种方式启动：

**方式一：联动启动（推荐）**
1. 确保已配置游戏路径
2. 启用“同时启动无限暖暖”开关
3. 点击“启动”按钮
4. 应用会自动启动游戏并开始截图

**方式二：手动启动**
1. 先手动启动无限暖暖游戏
2. 等待游戏完全加载
3. 点击“启动”按钮
4. 应用会自动找到游戏窗口并开始截图

### 3. 停止截图器

点击“停止”按钮即可停止截图器。

## 🛠️ 使用说明

### 截图模式选择

- **BitBlt**（推荐）：兼容性好，问题少，适合大多数用户
- **WindowsGraphicsCapture**：性能更好，但需要 Windows 10 18362 或更高版本
- **DwmSharedSurface**：备选方案
- **PrintWindow**：备选方案

### 触发器间隔

- 默认值：50ms
- 普通用户不建议调整
- 较低的值会提高响应速度，但会增加 CPU 占用

### 测试图像捕获

用于测试不同截图模式的效果：
1. 点击“测试图像捕获”按钮
2. 选择游戏窗口
3. 查看实时预览和性能数据

### 手动选择窗口

如果自动查找失败，可以手动选择窗口：
1. 点击“选择捕获窗口”按钮
2. 使用取色器选择游戏窗口
3. 应用会使用该窗口进行截图

## ❓ 常见问题

### Q: 点击“启动”后提示“未找到游戏窗口”

A: 有以下几种解决方案：
1. 确保游戏已经启动并完全加载
2. 检查游戏路径是否配置正确
3. 启用“同时启动无限暖暖”选项，让应用自动启动游戏
4. 使用“手动选择窗口”功能

### Q: 游戏启动了但截图器无法工作

A: 尝试以下方法：
1. 以管理员身份运行应用
2. 切换截图模式（推荐使用 BitBlt）
3. 检查游戏是否处于窗口化或无边框窗口模式
4. 重启应用和游戏

### Q: 如何更改背景图片？

A: 
1. 在 Banner 区域右键点击
2. 选择“更换背景图片”
3. 选择你喜欢的图片

### Q: 应用日志在哪里查看？

A: 日志文件位于 `log/better-infinity-nikki.log`

## 🏗️ 项目结构

```
BetterInfinityNikki/
├── Core/                    # 核心功能
│   └── Config/             # 配置管理
├── View/                   # 视图层 (XAML)
│   ├── Pages/              # 页面
│   └── MainWindow.xaml     # 主窗口
├── ViewModel/              # 视图模型层
├── Service/                # 服务层
│   └── Interface/          # 服务接口
├── Helpers/                # 辅助类
├── Resources/              # 资源文件
│   ├── Images/             # 图片资源
│   └── Fonts/              # 字体资源
└── User/                   # 用户数据目录
```

## 技术栈

- **.NET 8** + **WPF**
- **WPF-UI** - Fluent Design UI 框架
- **CommunityToolkit.Mvvm** - MVVM 框架
- **Microsoft.Extensions** - 依赖注入、日志
- **Serilog** - 结构化日志
- **Newtonsoft.Json** - JSON 序列化

## 开发环境要求

- Windows 10/11 (推荐 11)
- .NET 8 SDK
- Visual Studio 2022 或 JetBrains Rider
- 1920x1080 或更高分辨率显示器

## 构建项目

```bash
# 恢复 NuGet 包
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run
```

## 架构说明

本项目采用经典的 MVVM 架构：

- **Model**: 配置和数据模型 (`Core/Config`)
- **View**: XAML 界面 (`View/`)
- **ViewModel**: 视图模型 (`ViewModel/`)
- **Service**: 业务逻辑服务 (`Service/`)

通过依赖注入容器管理服务生命周期，使用 WPF-UI 提供现代化的 Fluent Design 界面。

## 扩展开发

### 添加新页面

1. 在 `View/Pages/` 创建新的 Page XAML
2. 在 `App.xaml.cs` 中注册页面
3. 在 `MainWindow.xaml` 中添加导航菜单项

### 添加新功能

1. 定义服务接口 (`Service/Interface/`)
2. 实现服务类 (`Service/`)
3. 在 `App.xaml.cs` 中注册服务
4. 通过构造函数注入使用

## 许可证

本项目遵循与 BetterGI 相同的开源协议。

## 致谢

- [BetterGI](https://github.com/babalae/better-genshin-impact) - 灵感和架构来源
- [WPF-UI](https://github.com/lepoco/wpfui) - UI 框架
- [CommunityToolkit](https://github.com/CommunityToolkit/dotnet) - MVVM 工具包
