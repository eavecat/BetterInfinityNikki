# 半自动钓鱼功能实现说明

## 概述
参考BGI（BetterGenshinImpact）实现了半自动钓鱼功能的基础框架，包括UI界面和配置系统。

## 已完成的工作

### 1. 配置类 (AutoFishingConfig)
**文件位置**: `BetterInfinityNikki/GameTask/AutoFishing/AutoFishingConfig.cs`

- 创建了自动钓鱼配置类
- 包含启用/禁用开关
- 预留了鱼儿上钩文字识别区域配置（暂未使用）

### 2. 配置集成 (AllConfig)
**文件位置**: `BetterInfinityNikki/Core/Config/AllConfig.cs`

- 在AllConfig中添加了AutoFishingConfig属性
- 注册了PropertyChanged事件监听
- 确保配置变更能够被正确追踪

### 3. UI界面 (TriggerSettingsPage.xaml)
**文件位置**: `BetterInfinityNikki/View/Pages/TriggerSettingsPage.xaml`

添加了半自动钓鱼配置卡片，包含：
- 标题："半自动钓鱼"
- 描述："半自动钓鱼需要手动抛竿，自动提竿和拉条"
- 启用/禁用切换开关
- 图标：钓鱼图标 (Glyph: &#xe3a8;)

### 4. 触发器框架 (AutoFishingTrigger)
**文件位置**: `BetterInfinityNikki/GameTask/AutoFishing/AutoFishingTrigger.cs`

创建了自动钓鱼触发器基础框架：
- 实现了ITaskTrigger接口
- 设置了执行间隔为67ms（与BGI保持一致）
- 优先级设为15
- 非独占模式
- 包含基本的异常处理
- 预留了钓鱼检测逻辑的实现位置

### 5. 资源管理 (AutoFishingAssets)
**文件位置**: `BetterInfinityNikki/GameTask/AutoFishing/Assets/AutoFishingAssets.cs`

创建了钓鱼资源管理类：
- 单例模式实现
- 继承自GameAssets基类
- 提供了Instance()和DestroyInstance()方法
- 预留了钓鱼相关识别对象的添加位置

### 6. 任务管理器集成 (GameTaskManager)
**文件位置**: `BetterInfinityNikki/GameTask/GameTaskManager.cs`

- 在LoadInitialTriggers()中注册了AutoFishingTrigger
- 在AddTrigger()中添加了AutoFishing的case分支
- 在RefreshTriggerConfigs()中添加了Init调用
- 在ReloadAssets()中添加了DestroyInstance调用
- 添加了必要的using引用

## 待实现的功能

### 核心识别逻辑
1. **钓鱼界面检测**
   - 检测是否进入钓鱼状态
   - 识别钓鱼按钮

2. **鱼上钩检测**
   - 图像识别或OCR识别鱼上钩提示
   - 参考BGI的实现方式

3. **自动提竿**
   - 检测到鱼上钩后自动点击提竿

4. **自动拉条**
   - 识别钓鱼拉条界面
   - 控制拉条操作

### 资源配置
需要在以下位置添加相应的资源文件：
```
BetterInfinityNikki/GameTask/AutoFishing/Assets/
├── 1920x1080/          # 1080p分辨率的资源
│   ├── fishing_button.png
│   ├── bite_indicator.png
│   └── ...
└── [其他分辨率]/        # 其他分辨率的资源
```

### 可能的优化
1. 添加更多配置选项（如超时时间、识别区域等）
2. 支持不同分辨率的适配
3. 添加调试模式和日志输出
4. 实现统计功能（钓鱼数量、成功率等）

## 参考BGI的实现

BGI中的相关文件：
- `BetterGenshinImpact/GameTask/AutoFishing/AutoFishingConfig.cs` - 配置类
- `BetterGenshinImpact/GameTask/AutoFishing/AutoFishingTrigger.cs` - 触发器
- `BetterGenshinImpact/GameTask/AutoFishing/Assets/AutoFishingAssets.cs` - 资源管理
- `BetterGenshinImpact/GameTask/AutoFishing/Behaviours.cs` - 行为树实现
- `BetterGenshinImpact/GameTask/AutoFishing/AutoFishingImageRecognition.cs` - 图像识别
- `BetterGenshinImpact/View/Pages/TriggerSettingsPage.xaml` - UI界面（半自动钓鱼部分）
- `BetterGenshinImpact/View/Pages/TaskSettingsPage.xaml` - UI界面（全自动钓鱼部分）

## 注意事项

1. **当前状态**: 仅完成了UI和配置框架，核心识别逻辑尚未实现
2. **编译验证**: 由于环境中未安装.NET SDK，未能进行编译验证
3. **后续开发**: 需要参考BGI的具体实现来完成识别逻辑
4. **测试**: 实现核心逻辑后需要进行充分的游戏内测试

## 下一步计划

1. 研究BGI的钓鱼识别算法
2. 实现钓鱼界面检测
3. 实现鱼上钩检测
4. 实现自动提竿功能
5. 实现自动拉条功能
6. 添加必要的资源文件
7. 测试和优化
