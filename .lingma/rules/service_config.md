# C# 服务和配置规范

## 服务类规范

### 接口定义
```csharp
namespace BetterGenshinImpact.Service.Interface
{
    public interface IExampleService
    {
        Task<string> GetDataAsync(CancellationToken ct = default);
        Task SaveDataAsync(string data, CancellationToken ct = default);
    }
}
```

### 服务实现
```csharp
using Microsoft.Extensions.Logging;

public class ExampleService : IExampleService
{
    private readonly ILogger<ExampleService> _logger;
    
    public ExampleService(ILogger<ExampleService> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> GetDataAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("获取数据");
        return await FetchDataAsync(ct);
    }
}
```

---

## 配置类规范

### 配置类定义
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

### 配置持久化
```csharp
using Newtonsoft.Json;

public void SaveConfig(MyConfig config, string path)
{
    var json = JsonConvert.SerializeObject(config, Formatting.Indented);
    File.WriteAllText(path, json);
}

public MyConfig LoadConfig(string path)
{
    if (!File.Exists(path)) return new MyConfig();
    var json = File.ReadAllText(path);
    return JsonConvert.DeserializeObject<MyConfig>(json) ?? new MyConfig();
}
```

---

## 异常处理规范

### 自定义异常
```csharp
// 正常结束异常（用于控制流程）
public class NormalEndException : Exception
{
    public NormalEndException(string message) : base(message) { }
}

// 重试异常
public class RetryException : Exception
{
    public RetryException(string message) : base(message) { }
}
```

### 异常处理示例
```csharp
public async Task ExecuteWithRetryAsync(int maxRetries = 3)
{
    int retryCount = 0;
    
    while (retryCount < maxRetries)
    {
        try
        {
            await DoWorkAsync();
            return;
        }
        catch (NormalEndException)
        {
            throw;  // 正常结束，不重试
        }
        catch (RetryException ex)
        {
            retryCount++;
            if (retryCount >= maxRetries)
                throw new RetryNoCountException("重试次数已用尽");
            
            await Task.Delay(1000 * retryCount);
        }
    }
}
```

---

## 资源管理规范

### Mat 对象释放
```csharp
using OpenCvSharp;

// ✅ 推荐：使用 using 语句
public void ProcessImage()
{
    using var src = Cv2.ImRead("image.png");
    using var dst = new Mat();
    Cv2.CvtColor(src, dst, ColorConversionCodes.BGR2GRAY);
}

// ❌ 禁止：忘记释放
public void ProcessImageBad()
{
    var mat = new Mat();
    // 使用 mat
    // 忘记调用 Dispose()
}
```

### IDisposable 实现
```csharp
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

## 日志记录规范

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
        _logger.LogDebug("调试信息：{Param}", value);
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

### 日志级别
- `LogDebug`: 详细的调试信息
- `LogInformation`: 一般信息（默认显示在遮罩窗口）
- `LogWarning`: 警告信息
- `LogError`: 错误信息

---

## 编译检查清单

- [ ] 代码能够成功编译：`dotnet build BetterGenshinImpact.sln -c Debug`
- [ ] 所有 ViewModel 使用了 `[ObservableProperty]` 和 `[RelayCommand]`
- [ ] 正确释放了所有 `Mat`、`Stream` 等资源
- [ ] 使用了 `ThemedMessageBox` 而不是 `MessageBox`
- [ ] 优先使用 `Newtonsoft.Json` 进行序列化
- [ ] 添加了适当的日志记录
- [ ] 正确处理了异常
- [ ] 异步方法以 `Async` 结尾并支持 `CancellationToken`
