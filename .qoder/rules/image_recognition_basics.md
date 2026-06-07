# 图像识别开发规范

## RecognitionObject 定义

### 模板匹配识别
```csharp
using BetterGenshinImpact.Core.Recognition;
using OpenCvSharp;

var recognitionObject = new RecognitionObject
{
    Name = "确认按钮",
    RecognitionType = RecognitionTypes.TemplateMatch,
    TemplateImageMat = Cv2.ImRead("Assets/confirm.png"),
    RegionOfInterest = new Rect(1700, 950, 200, 100),
    Threshold = 0.8
};

// 使用
var result = captureArea.Find(recognitionObject);
if (result.IsSuccess)
{
    result.Click();
}
```

### OCR 识别
```csharp
using BetterGenshinImpact.Core.Recognition.OCR;

var ocrService = OcrFactory.Create(OcrEngineTypes.Paddle);
string text = ocrService.Ocr(captureArea.Mat);
```

---

## Assets 资产管理

```csharp
public class AutoFightAssets : BaseAssets<AutoFightAssets>
{
    public RecognitionObject AttackButton { get; private set; }
    
    protected override void InitAssets()
    {
        AttackButton = new RecognitionObject
        {
            Name = "攻击按钮",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GetImageMat("attack.png"),
            RegionOfInterest = new Rect(1600, 800, 300, 200),
            Threshold = 0.85
        };
    }
}

// 使用
var assets = AutoFightAssets.Instance;
var result = captureArea.Find(assets.AttackButton);
```

---

## 捕获区域操作

```csharp
using BetterGenshinImpact.GameTask;

// 捕获屏幕
var captureArea = TaskContext.Instance().CaptureToRectArea();

// 查找识别对象
var result = captureArea.Find(recognitionObject);

// 等待直到找到
var found = captureArea.WaitUntilFound(recognitionObject, timeout: 5000);

// 点击
if (result.IsSuccess)
{
    result.Click();
}
```

---

## 输入模拟

### 键盘模拟
```csharp
using BetterGenshinImpact.Core.Simulator;

// 单个按键
Simulation.SendInput.Keyboard.KeyPress(VirtualKeyCode.F);

// 组合键（Ctrl+C）
Simulation.SendInput.Keyboard.KeyDown(VirtualKeyCode.LControl);
Simulation.SendInput.Keyboard.KeyPress(VirtualKeyCode.C);
Simulation.SendInput.Keyboard.KeyUp(VirtualKeyCode.LControl);
```

### 鼠标模拟
```csharp
// 移动鼠标
Simulation.SendInput.Mouse.MoveMouseTo(x, y);

// 左键点击
Simulation.SendInput.Mouse.LeftButtonClick();
```

### 游戏动作
```csharp
using BetterGenshinImpact.Core.Simulator.Extensions;

Simulation.SendInput.SimulateAction(GIActions.Attack);
Simulation.SendInput.SimulateAction(GIActions.ElementalSkill);
Simulation.SendInput.SimulateAction(GIActions.Jump);
```

---

## 任务触发器开发

```csharp
public class AutoPickTrigger : ITaskTrigger
{
    private readonly ILogger<AutoPickTrigger> _logger;
    private Timer? _timer;
    
    public string Name => "自动拾取";
    public bool IsEnabled { get; private set; }
    
    public void Start()
    {
        if (IsEnabled) return;
        IsEnabled = true;
        _timer = new Timer(OnTick, null, 0, 100);
    }
    
    public void Stop()
    {
        if (!IsEnabled) return;
        IsEnabled = false;
        _timer?.Dispose();
    }
    
    private void OnTick(object? state)
    {
        try
        {
            var captureArea = TaskContext.Instance().CaptureToRectArea();
            var pickPrompt = captureArea.Find(AutoPickAssets.Instance.PickPrompt);
            
            if (pickPrompt.IsSuccess)
            {
                Simulation.SendInput.Keyboard.KeyPress(VirtualKeyCode.F);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动拾取异常");
        }
    }
}
```

---

## 独立任务开发

```csharp
public class AutoCookTask : ISoloTask
{
    public string Name => "自动烹饪";
    
    public async Task RunAsync(CancellationToken ct)
    {
        // 步骤 1：打开烹饪界面
        await OpenCookInterfaceAsync(ct);
        
        // 步骤 2：开始烹饪
        await StartCookingAsync(ct);
        
        // 步骤 3：等待完成
        await WaitForCompletionAsync(ct);
    }
}
```

---

## 性能优化建议

### 1. 减少识别区域
```csharp
// ✅ 推荐：缩小 ROI
RegionOfInterest = new Rect(1600, 800, 300, 200)

// ❌ 避免：全屏识别
RegionOfInterest = new Rect(0, 0, 1920, 1080)
```

### 2. 缓存 Mat 对象
```csharp
private Mat? _cachedTemplate;

private Mat GetTemplate()
{
    if (_cachedTemplate == null)
    {
        _cachedTemplate = Cv2.ImRead("Assets/template.png");
    }
    return _cachedTemplate;
}
```

### 3. 降低识别频率
```csharp
// 每 200ms 检查一次，而不是每帧
_timer = new Timer(OnTick, null, 0, 200);
```

---

## 常见陷阱

### ❌ 忘记释放 Mat 对象
```csharp
// ❌ 错误
var mat = new Mat();
// 使用 mat
// 忘记调用 Dispose()

// ✅ 正确
using var mat = new Mat();
```

### ❌ 频繁加载图片
```csharp
// ❌ 错误：每次都加载
foreach (var item in items)
{
    var mat = Cv2.ImRead("Assets/template.png");
}

// ✅ 正确：只加载一次
var template = Cv2.ImRead("Assets/template.png");
```

---

## 代码审查清单

- [ ] 正确使用 `RecognitionObject` 定义识别对象
- [ ] 在 `BaseAssets` 中统一管理图片资源
- [ ] 正确释放所有 `Mat` 对象
- [ ] 使用适当的 ROI 区域减少识别范围
- [ ] 添加了充分的日志记录
- [ ] 避免了频繁的图像加载
- [ ] 使用了异步操作处理耗时任务
