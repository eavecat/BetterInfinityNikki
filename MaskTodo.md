# 大地图遮罩层功能实现清单

> 对比 BGI (BetterGenshinImpact) 和 BetterInfinityNikki 的大地图遮罩层功能，列出未实现的功能项。

## 📊 当前状态

- ✅ 已实现：基础窗口、透明覆盖、PointsCanvas 控件、状态栏、日志框、点击穿透控制
- ✅ 已实现：视口更新机制、大地图界面检测、点位服务集成（含缓存与图标加载）
- ✅ 已实现：点位选择器（分类树、搜索、标签选择、异步图标加载）
- ✅ 已实现：点位在遮罩层上的渲染、Web/Game/Image 三坐标转换与校准
- ❌ 未实现：点位交互命令、点位信息弹窗
- ❌ 未实现：增强功能（小地图、方位指示器、FPS、UID遮盖等）

---

## 🔴 高优先级（核心功能）

### 1. 视口更新机制集成
**状态**: ✅ 已完成

**已实现**:
- [x] 在 `MapMaskTrigger.OnCapture` 中检测到大地图视口变化时，调用 `PointsCanvas.UpdateViewport()`
- [x] 参考 BGI 的 `QueueUiUpdate` 机制，异步更新 UI
- [x] 确保视口坐标正确转换（地图坐标 → 屏幕坐标）
- [x] 防抖机制：使用 `_uiApplyScheduled` 防止重复调度
- [x] 合并更新：使用 `Interlocked.Exchange` 合并多次更新

**相关文件**:
- `GameTask/MapMask/MapMaskTrigger.cs` - 完整的视口更新机制（第 226-284 行）
- `View/Controls/PointsCanvas.cs` - UpdateViewport 方法（第 346-356 行）
- `View/MaskWindow.xaml.cs` - MapPointsCanvas 实例

**BGI 参考**:
- `MapMaskTrigger.cs` 中的 `CalculateBigMapViewport` 和 `QueueUiUpdate`
- `MaskWindow.xaml.cs` 中的 `ApplyPendingUiUpdate`

---

### 2. 大地图界面检测与条件显示
**状态**: ✅ 已完成

**已实现**:
- [x] 在 `MaskWindowViewModel` 中添加 `IsInBigMapUi` 属性
- [x] 在 `MapMaskTrigger` 中检测是否处于大地图界面（已有 `DetectBigMap` 方法）
- [x] 根据 `IsInBigMapUi` 控制 PointsCanvas 的可见性（XAML 绑定）
- [x] 根据 `IsInBigMapUi` 动态调整点击穿透状态

**相关文件**:
- `ViewModel/MaskWindowViewModel.cs` - 添加了 `IsInBigMapUi` 属性
- `GameTask/MapMask/MapMaskTrigger.cs` - 更新 ViewModel 状态
- `View/MaskWindow.xaml` - 绑定 Visibility
- `View/MaskWindow.xaml.cs` - 监听属性变化并更新点击穿透

**BGI 参考**:
- `MaskWindowViewModel.cs` 中的 `IsInBigMapUi` 属性
- `MaskWindow.xaml.cs` 中的 `UpdateClickThroughState` 方法

---

### 3. 点位服务集成与数据加载
**状态**: ✅ 已完成（官方地图API已集成，缓存和图标加载已实现）

**已实现**:
- [x] 创建 `IMaskMapPointService` 接口
- [x] 创建 `NikkiMapApiService` API服务类（支持官方地图）
- [x] 创建 `MaskMapPointService` 业务逻辑层
- [x] 在 `App.xaml.cs` 中注册服务
- [x] 模型类已存在：`MaskMapPoint`, `MaskMapPointLabel`, `MaskMapPointsResult`, `MaskMapPointInfo`
- [x] 实现获取点位分类树（`catalog/list` API）
- [x] 实现获取点位列表（`spawner/list` API，支持Snappy解压）
- [x] 实现获取点位详情（`spawner/info` API）
- [x] 多语言文本解析（JSON格式）
- [x] 缓存机制：集成 LazyCache (`IAppCache`) + `MemoryFileCache` 文件缓存
- [x] 图标加载和缓存：`MapIconImageCache` 实现异步图标加载（带并发控制 SemaphoreSlim）
- [x] 坐标转换逻辑（Web坐标 → 游戏坐标 → 图片像素坐标，含非线性校正）

**相关文件**:
- `Service/Interface/IMaskMapPointService.cs` - 服务接口（新建）
- `Service/NikkiMapApiService.cs` - API服务（新建）
- `Service/MaskMapPointService.cs` - 业务逻辑（新建）
- `Service/MemoryFileCache.cs` - 文件缓存（新建）
- `Service/Model/NikkiMap/Requests/*.cs` - 请求模型（新建）
- `Service/Model/NikkiMap/Responses/*.cs` - 响应模型（新建）
- `Model/MaskMap/*.cs` - 数据模型（已存在）
- `Model/MaskMap/GameWebMapCoordinateConverter.cs` - Web/游戏/图像坐标转换（含非线性校正）
- `ViewModel/MapIconImageCache.cs` - 图标缓存（新建）
- `App.xaml.cs` - 服务注册（已添加）

**BGI 参考**:
- `Service/MaskMapPointService.cs` (BGI版本)
- `Service/Interface/IMaskMapPointService.cs`

**API接口信息**:
1. ✅ 获取点位分类树: `POST /v1/strategy/map/catalog/list`
2. ✅ 获取点位列表: `POST /v1/strategy/map/spawner/list` (Snappy压缩)
3. ✅ 获取点位详情: `POST /v1/strategy/map/spawner/info`

**依赖包**:
- Snappier v1.1.6 - Snappy压缩/解压缩库

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
**状态**: ✅ 已完成

**已实现**:
- [x] 创建左侧点位分类选择器 UI（MaskWindow.xaml 中添加完整选择器面板）
- [x] 实现分类树形结构（一级分类 `MapLabelCategories`、二级子分类 `SelectedCategory.Children`）
- [x] 添加搜索框，支持按名称搜索点位（`SearchLabelsAsync` 命令）
- [x] 实现分类筛选逻辑（`SelectCategoryAsync` / `SelectLabelItemAsync` 命令）
- [x] 添加加载状态指示器（`IsMapLabelTreeLoading` / `IsLoadingPoints`）
- [x] 切换面板开关（`ToggleMapPointPickerAsync` 命令）
- [x] 已选标签管理（`SelectedMapLabelItems` 集合，支持添加/移除）
- [x] 清除选中（`ClearSelectedLabelsAsync` / `ResetSelectedMapLabelSelectionAsync` 命令）
- [x] 异步图标加载（`LoadIconAsync` 带 SemaphoreSlim 并发控制）
- [x] `MaskMapPointLabel` 实现 `INotifyPropertyChanged`，添加 `IconImage` 属性
- [x] 点击穿透联动：在大地图界面且选择器打开时不穿透点击

**相关文件**:
- `View/MaskWindow.xaml` - 点位选择器 UI（新增约 430 行 XAML）
- `ViewModel/MaskWindowViewModel.cs` - 选择器逻辑（新增约 300 行）
- `Model/MaskMap/MaskMapPointLabel.cs` - 添加 `INotifyPropertyChanged` 和 `IconImage`
- `View/MaskWindow.xaml.cs` - `ViewModelOnPropertyChanged` 监听大地图状态

**BGI 参考**:
- `MaskWindow.xaml` 中的点位选择器部分
- `View/Windows/MapLabelSearchWindow.xaml`

---

## 🟢 低优先级（增强功能）

### 8. 小地图点位渲染 (MiniMapPointsCanvas)
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

### 9. 方位指示器 (Directions)
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

### 10. FPS 显示
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

### 11. UID 遮盖
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

### 12. 编辑模式 UI 提示
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

### 13. Wine 兼容支持
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

### 14. 技能 CD 特殊渲染
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

### 15. 可调整布局控件 (AdjustableOverlayItem)
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

## 📝 ViewModel 已添加的属性

在 `MaskWindowViewModel.cs` 中已添加以下属性：

```csharp
// 大地图界面状态
[ObservableProperty]
private bool _isInBigMapUi;

// 点位选择器状态
[ObservableProperty]
private bool _isMapPointPickerOpen;

// 点位分类树（一级分类）
[ObservableProperty]
private ObservableCollection<MaskMapPointLabel> _mapLabelCategories = new();

// 当前选中的分类
[ObservableProperty]
private MaskMapPointLabel? _selectedCategory;

// 搜索关键词
[ObservableProperty]
private string _mapLabelSearchText = string.Empty;

// 右侧显示的标签列表（子分类或搜索结果）
[ObservableProperty]
private ObservableCollection<MaskMapPointLabel> _mapLabelItems = new();

// 已选中的标签列表
[ObservableProperty]
private ObservableCollection<MaskMapPointLabel> _selectedMapLabelItems = new();

// 是否正在加载分类树
[ObservableProperty]
private bool _isMapLabelTreeLoading;

// 是否正在加载点位
[ObservableProperty]
private bool _isLoadingPoints;
```

## 📝 ViewModel 待添加的属性

```csharp
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

## 📝 ViewModel 已添加的命令

在 `MaskWindowViewModel.cs` 中已添加以下命令：

```csharp
// 切换点位选择器面板
[RelayCommand]
private async Task ToggleMapPointPickerAsync();

// 选中/取消选中分类
[RelayCommand]
private async Task SelectCategoryAsync(MaskMapPointLabel? category);

// 选中/取消选中具体标签
[RelayCommand(AllowConcurrentExecutions = true)]
private async Task SelectLabelItemAsync(MaskMapPointLabel? label);

// 搜索标签
[RelayCommand]
private async Task SearchLabelsAsync();

// 清除所有选中标签
[RelayCommand]
private async Task ClearSelectedLabelsAsync();

// 重置已选标签
[RelayCommand]
private async Task ResetSelectedMapLabelSelectionAsync();
```

## 📝 ViewModel 待添加的命令

```csharp
// 点位交互命令
[RelayCommand]
private async Task OnPointClick(MaskMapPoint point);

[RelayCommand]
private async Task OnPointRightClick(MaskMapPoint point);

[RelayCommand]
private async Task OnPointHover(MaskMapPoint point);

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
- `Service/Interface/IMaskMapPointService.cs` - 点位服务接口（已创建）
- `Service/MaskMapPointService.cs` - 点位服务实现（已创建）
- `Service/NikkiMapApiService.cs` - 官方地图API服务（已创建）
- `Service/MemoryFileCache.cs` - 文件缓存服务（已创建）
- `ViewModel/MapIconImageCache.cs` - 图标缓存加载器（已创建）

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

### 第一阶段：核心功能（已完成 ✅）
1. ✅ 视口更新机制集成
2. ✅ 大地图界面检测
3. ✅ 点位服务集成

**目标**: 让点位能够正确显示并跟随地图移动 ✅

### 第二阶段：交互功能（部分完成）
4. ❌ 点位点击/右键/悬停命令
5. ❌ 点位信息弹窗
6. ✅ 点位选择器
7. ✅ 点位渲染（PointsCanvas 绘制 + 坐标转换校准）

**目标**: 提供完整的用户交互体验（进行中）

### 第三阶段：增强功能（未开始）
8-15. 其他增强功能（小地图、方位指示器、FPS、UID遮盖、编辑模式、Wine 兼容、技能 CD、可调整布局）

**目标**: 提升用户体验和功能完整性

---

## 📅 更新时间

最后更新: 2026-06-07
