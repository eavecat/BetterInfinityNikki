# 大地图遮罩层功能实现清单

> 对比 BGI (BetterGenshinImpact) 和 BetterInfinityNikki 的大地图遮罩层功能，列出未实现的功能项。

## 📊 当前状态

- ✅ 已实现：基础窗口、透明覆盖、PointsCanvas 控件、状态栏、日志框、点击穿透控制
- ❌ 未实现：核心业务逻辑、用户交互、UI 控制面板

---

## 🔴 高优先级（核心功能）

### 1. 视口更新机制集成
**状态**: PointsCanvas 有 `UpdateViewport` 方法，但未被调用

**需要实现**:
- [ ] 在 `MapMaskTrigger.OnCapture` 中检测到大地图视口变化时，调用 `PointsCanvas.UpdateViewport()`
- [ ] 参考 BGI 的 `QueueUiUpdate` 机制，异步更新 UI
- [ ] 确保视口坐标正确转换（地图坐标 → 屏幕坐标）

**相关文件**:
- `GameTask/MapMask/MapMaskTrigger.cs`
- `View/Controls/PointsCanvas.cs`
- `View/MaskWindow.xaml.cs`

**BGI 参考**:
- `MapMaskTrigger.cs` 中的 `CalculateBigMapViewport` 和 `QueueUiUpdate`
- `MaskWindow.xaml.cs` 中的 `ApplyPendingUiUpdate`

---

### 2. 大地图界面检测与条件显示
**状态**: 缺少 `IsInBigMapUi` 属性

**需要实现**:
- [ ] 在 `MaskWindowViewModel` 中添加 `IsInBigMapUi` 属性
- [ ] 在 `MapMaskTrigger` 中检测是否处于大地图界面
- [ ] 根据 `IsInBigMapUi` 控制 PointsCanvas 的可见性
- [ ] 根据 `IsInBigMapUi` 动态调整点击穿透状态

**相关文件**:
- `ViewModel/MaskWindowViewModel.cs`
- `GameTask/MapMask/MapMaskTrigger.cs`
- `View/MaskWindow.xaml`
- `View/MaskWindow.xaml.cs`

**BGI 参考**:
- `MaskWindowViewModel.cs` 中的 `IsInBigMapUi` 属性
- `MaskWindow.xaml.cs` 中的 `UpdateClickThroughState` 方法

---

### 3. 点位服务集成与数据加载
**状态**: 虽然有 `IMaskMapPointService` 接口，但没有实际调用

**需要实现**:
- [ ] 实现或移植 `MaskMapPointService`（从 API 获取点位数据）
- [ ] 在 `MaskWindowViewModel` 中添加加载点位的命令
- [ ] 添加加载状态指示器（`IsMapPointsLoading`、`MapPointsLoadingText`）
- [ ] 实现点位分类选择功能
- [ ] 将加载的点位数据绑定到 `MapPoints` 和 `MapPointLabels`

**相关文件**:
- `Service/Interface/IMaskMapPointService.cs`
- `Service/MaskMapPointService.cs`（需要创建或移植）
- `ViewModel/MaskWindowViewModel.cs`
- `Model/MaskMap/MaskMapPoint.cs`
- `Model/MaskMap/MaskMapPointLabel.cs`

**BGI 参考**:
- `Service/MaskMapPointService.cs`
- `ViewModel/MaskWindowViewModel.cs` 中的点位加载逻辑

---

## 🟡 中优先级（交互功能）

### 4. 点位点击/右键/悬停命令
**状态**: PointsCanvas 支持这些事件，但未绑定命令

**需要实现**:
- [ ] 在 `MaskWindowViewModel` 中添加三个命令：
  - `PointClickCommand` - 点位左键点击
  - `PointRightClickCommand` - 点位右键点击
  - `PointHoverCommand` - 点位悬停
- [ ] 在 XAML 中绑定这些命令到 PointsCanvas
- [ ] 实现命令的具体逻辑（如显示弹窗、标记路径等）

**相关文件**:
- `ViewModel/MaskWindowViewModel.cs`
- `View/MaskWindow.xaml`
- `View/Controls/PointsCanvas.cs`

**BGI 参考**:
- `MaskWindow.xaml` 中的命令绑定
- `MaskWindowViewModel.cs` 中的命令实现

---

### 5. 点位信息弹窗 (PointInfoPopup)
**状态**: 完全缺失

**需要实现**:
- [ ] 创建点位信息弹窗 UI（Popup 控件）
- [ ] 显示点位名称、坐标、视频教程链接等信息
- [ ] 实现弹窗的打开/关闭逻辑
- [ ] 添加视频链接跳转功能
- [ ] 添加关闭按钮

**相关文件**:
- `View/MaskWindow.xaml`（添加 Popup）
- `ViewModel/MaskWindowViewModel.cs`（添加 PointInfoPopup 状态）
- `Model/MaskMap/MaskMapPoint.cs`（可能需要扩展字段）

**BGI 参考**:
- `MaskWindow.xaml` 第 491-679 行的 Popup 实现

---

### 6. 点位选择器（分类和搜索）
**状态**: 完全缺失

**需要实现**:
- [ ] 创建左侧点位分类选择器 UI
- [ ] 实现分类树形结构（一级分类、二级分类）
- [ ] 添加搜索框，支持按名称搜索点位
- [ ] 实现分类筛选逻辑（只显示选中分类的点位）
- [ ] 添加加载状态指示器

**相关文件**:
- `View/MaskWindow.xaml`（添加选择器 UI）
- `ViewModel/MaskWindowViewModel.cs`（添加相关属性和命令）
- `View/Windows/MapLabelSearchWindow.xaml`（可能需要创建）

**BGI 参考**:
- `MaskWindow.xaml` 中的点位选择器部分
- `View/Windows/MapLabelSearchWindow.xaml`

---

## 🟢 低优先级（增强功能）

### 7. 小地图点位渲染 (MiniMapPointsCanvas)
**状态**: 完全缺失

**需要实现**:
- [ ] 创建 `MiniMapPointsCanvas` 控件（类似 PointsCanvas，但针对小地图优化）
- [ ] 在小地图位置显示简化的点位标记
- [ ] 实现小地图视口的坐标映射
- [ ] 添加配置项控制小地图点位显示

**相关文件**:
- `View/Controls/MiniMapPointsCanvas.cs`（需要创建）
- `View/MaskWindow.xaml`（添加控件）
- `Core/Config/MapMaskConfig.cs`（添加配置项）

**BGI 参考**:
- `View/Controls/MiniMapPointsCanvas.cs`
- `MaskWindow.xaml` 第 89-118 行

---

### 8. 方位指示器 (Directions)
**状态**: 完全缺失

**需要实现**:
- [ ] 在左上角显示东南西北方向标识
- [ ] 添加配置项控制显示/隐藏
- [ ] 支持透明度调节

**相关文件**:
- `View/MaskWindow.xaml`（添加方位指示器 UI）
- `Core/Config/MaskWindowConfig.cs`（添加配置项）

**BGI 参考**:
- `MaskWindow.xaml` 第 341-416 行

---

### 9. FPS 显示
**状态**: 完全缺失

**需要实现**:
- [ ] 集成 FPS 计数器
- [ ] 在遮罩层上显示实时 FPS
- [ ] 添加配置项控制显示/隐藏

**相关文件**:
- `ViewModel/MaskWindowViewModel.cs`（添加 Fps 属性）
- `View/MaskWindow.xaml`（添加 FPS 显示）
- 需要集成 FPS 监测库（如 PresentMon）

**BGI 参考**:
- `MaskWindow.xaml` 第 419-436 行
- `MaskWindowViewModel.cs` 中的 Fps 属性

---

### 10. UID 遮盖
**状态**: 完全缺失

**需要实现**:
- [ ] 添加白色矩形遮盖游戏内 UID
- [ ] 添加配置项控制启用/禁用
- [ ] 可配置遮盖位置

**相关文件**:
- `View/MaskWindow.xaml`（添加遮盖矩形）
- `Core/Config/MaskWindowConfig.cs`（添加配置项）

**BGI 参考**:
- `MaskWindow.xaml` 第 334-338 行

---

### 11. 编辑模式 UI 提示
**状态**: 有编辑模式功能，但缺少视觉提示

**需要实现**:
- [ ] 在编辑模式下显示大号提示文字
- [ ] 提示内容："当前处于编辑模式，可以调整控件位置和大小"
- [ ] 添加退出编辑模式的说明

**相关文件**:
- `View/MaskWindow.xaml`（添加提示 UI）

**BGI 参考**:
- `MaskWindow.xaml` 第 293-331 行

---

### 12. Wine 兼容支持
**状态**: 完全缺失

**需要实现**:
- [ ] 检测是否在 Wine 环境下运行
- [ ] 在 Wine 环境下添加半透明背景（提高兼容性）
- [ ] 添加相关的平台检测逻辑

**相关文件**:
- `View/MaskWindow.xaml`（添加 Wine 背景层）
- `Platform/Wine/WinePlatformAddon.cs`（可能需要创建）

**BGI 参考**:
- `MaskWindow.xaml` 第 438-456 行

---

### 13. 技能 CD 特殊渲染
**状态**: 只有基础的矩形/线条/文本绘制

**需要实现**:
- [ ] 在 `OnRender` 中添加技能 CD 文本的特殊渲染逻辑
- [ ] 使用自定义字体（Fgi-Regular）
- [ ] 支持不同的颜色配置（就绪状态 vs 冷却中）
- [ ] 添加圆角矩形背景

**相关文件**:
- `View/MaskWindow.xaml.cs`（扩展 OnRender 方法）
- `Core/Config/SkillCdConfig.cs`（添加颜色配置）

**BGI 参考**:
- `MaskWindow.xaml.cs` 第 511-556 行

---

### 14. 可调整布局控件 (AdjustableOverlayItem)
**状态**: 使用普通 Canvas + Border，不支持拖拽调整

**需要实现**:
- [ ] 移植 `AdjustableOverlayItem` 控件
- [ ] 用该控件包装状态栏和日志框
- [ ] 实现拖拽调整位置和大小的功能
- [ ] 实现布局保存/加载

**相关文件**:
- `View/Controls/Overlay/AdjustableOverlayItem.cs`（需要移植）
- `View/MaskWindow.xaml`（替换现有布局）
- `ViewModel/MaskWindowViewModel.cs`（添加布局相关命令）

**BGI 参考**:
- `View/Controls/Overlay/AdjustableOverlayItem.cs`
- `MaskWindow.xaml` 中的 AdjustableOverlayItem 使用

---

## 📝 ViewModel 需要添加的属性

在 `MaskWindowViewModel.cs` 中需要添加以下属性：

```csharp
// 大地图界面状态
[ObservableProperty]
private bool _isInBigMapUi;

// 点位选择器状态
[ObservableProperty]
private bool _isMapPointPickerOpen;

// 点位加载状态
[ObservableProperty]
private bool _isMapPointsLoading;

[ObservableProperty]
private string _mapPointsLoadingText;

// 点位信息弹窗
[ObservableProperty]
private PointInfoPopupState _pointInfoPopup;

// 窗口尺寸（已有）
[ObservableProperty]
private double _maskWindowWidth;

[ObservableProperty]
private double _maskWindowHeight;
```

---

## 📝 ViewModel 需要添加的命令

在 `MaskWindowViewModel.cs` 中需要添加以下命令：

```csharp
// 点位交互命令
[RelayCommand]
private async Task OnPointClick(MaskMapPoint point);

[RelayCommand]
private async Task OnPointRightClick(MaskMapPoint point);

[RelayCommand]
private async Task OnPointHover(MaskMapPoint point);

// 点位分类选择
[RelayCommand]
private async Task OnSelectMapLabelCategory(MapLabelCategoryVm category);

// 退出编辑模式
[RelayCommand]
private void OnExitOverlayLayoutEditMode();
```

---

## 🔗 相关文件索引

### 核心文件
- `View/MaskWindow.xaml` - 遮罩窗口 UI
- `View/MaskWindow.xaml.cs` - 遮罩窗口逻辑
- `ViewModel/MaskWindowViewModel.cs` - 遮罩窗口 ViewModel
- `View/Controls/PointsCanvas.cs` - 点位画布控件
- `GameTask/MapMask/MapMaskTrigger.cs` - 地图遮罩触发器

### 数据模型
- `Model/MaskMap/MaskMapPoint.cs` - 点位数据模型
- `Model/MaskMap/MaskMapPointLabel.cs` - 点位标签模型
- `Model/MaskMap/MaskMapLink.cs` - 点位链接模型

### 服务层
- `Service/Interface/IMaskMapPointService.cs` - 点位服务接口
- `Service/MaskMapPointService.cs` - 点位服务实现（需创建）

### 配置
- `Core/Config/MaskWindowConfig.cs` - 遮罩窗口配置
- `Core/Config/MapMaskConfig.cs` - 地图遮罩配置

---

## 📚 BGI 参考文件

- `BetterGenshinImpact/View/MaskWindow.xaml`
- `BetterGenshinImpact/View/MaskWindow.xaml.cs`
- `BetterGenshinImpact/ViewModel/MaskWindowViewModel.cs`
- `BetterGenshinImpact/View/Controls/PointsCanvas.cs`
- `BetterGenshinImpact/View/Controls/MiniMapPointsCanvas.cs`
- `BetterGenshinImpact/Service/MaskMapPointService.cs`
- `BetterGenshinImpact/GameTask/MapMask/MapMaskTrigger.cs`

---

## 🎯 实施建议

### 第一阶段：核心功能（必须）
1. 视口更新机制集成
2. 大地图界面检测
3. 点位服务集成

**目标**: 让点位能够正确显示并跟随地图移动

### 第二阶段：交互功能（重要）
4. 点位点击/右键/悬停命令
5. 点位信息弹窗
6. 点位选择器

**目标**: 提供完整的用户交互体验

### 第三阶段：增强功能（可选）
7-14. 其他增强功能

**目标**: 提升用户体验和功能完整性

---

## 📅 更新时间

最后更新: 2026-05-30
