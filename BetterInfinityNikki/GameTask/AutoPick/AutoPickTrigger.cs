using System.Text.Json;
using System.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.Core.Recognition.OCR;
using BetterInfinityNikki.Core.Recognition.ONNX.SVTR;
using BetterInfinityNikki.Core.Simulator;
using BetterInfinityNikki.GameTask.AutoPick.Assets;
using BetterInfinityNikki.GameTask.Model.Area;
using BetterInfinityNikki.View.Windows;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.AutoPick;

public partial class AutoPickTrigger : ITaskTrigger
{
    private readonly ILogger<AutoPickTrigger> _logger = App.GetLogger<AutoPickTrigger>();

    public string Name => "自动拾取";
    public bool IsEnabled { get; set; }
    public int Priority => 30;
    public bool IsExclusive => false;

    private bool picking = false;

    private readonly AutoPickAssets _autoPickAssets;

    /// <summary>
    /// 拾取黑名单
    /// </summary>
    private HashSet<string> _blackList = [];

    /// <summary>
    /// 拾取黑名单(模糊匹配)
    /// </summary>
    private List<string> _fuzzyBlackList = [];

    /// <summary>
    /// 拾取白名单
    /// </summary>
    private HashSet<string> _whiteList = [];

    private RecognitionObject _pickRo;

    public AutoPickTrigger()
    {
        _autoPickAssets = AutoPickAssets.Instance;
        _pickRo = _autoPickAssets.PickRo;
    }

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoPickConfig;
        IsEnabled = config.Enabled;
        _logger.LogInformation("自动拾取 初始化: IsEnabled={Enabled}", IsEnabled);

        if (config.BlackListEnabled)
        {
            _blackList = ReadJson(@"Assets\Config\Pick\default_pick_black_lists.json");
            var userBlackList = ReadText(@"User\pick_black_lists.txt");
            if (userBlackList.Count > 0)
            {
                _blackList.UnionWith(userBlackList);
            }

            _fuzzyBlackList = ReadTextList(@"User\pick_fuzzy_black_lists.txt");
        }

        if (config.WhiteListEnabled)
        {
            _whiteList = ReadText(@"User\pick_white_lists.txt");
        }
    }

    private HashSet<string> ReadJson(string jsonFilePath)
    {
        try
        {
            var json = Global.ReadAllTextIfExist(jsonFilePath);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "读取拾取黑/白名单失败");
            ThemedMessageBox.Error("读取拾取黑/白名单失败，请确认修改后的拾取黑/白名单内容格式是否正确！");
        }

        return [];
    }

    private HashSet<string> ReadText(string textFilePath)
    {
        try
        {
            var txt = Global.ReadAllTextIfExist(textFilePath);
            if (!string.IsNullOrEmpty(txt))
            {
                return new HashSet<string>(txt.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "读取拾取黑/白名单失败");
            ThemedMessageBox.Error("读取拾取黑/白名单失败，请确认修改后的拾取黑/白名单内容格式是否正确！");
        }

        return [];
    }

    private List<string> ReadTextList(string textFilePath)
    {
        try
        {
            var txt = Global.ReadAllTextIfExist(textFilePath);
            if (!string.IsNullOrEmpty(txt))
            {
                return [..txt.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)];
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "读取拾取黑/白名单失败");
            ThemedMessageBox.Error("读取拾取黑/白名单失败，请确认修改后的拾取黑/白名单内容格式是否正确！");
        }

        return [];
    }

    /// <summary>
    /// 用于日志只输出一次
    /// </summary>
    private string _lastText = string.Empty;

    /// <summary>
    /// 用于日志只输出一次
    /// </summary>
    private int _prevClickFrameIndex = -1;

    public void OnCapture(CaptureContent content)
    {
        if (picking) return;
        // 检查停止计数器
        while (RunnerContext.Instance.AutoPickTriggerStopCount > 0)
        {
            Thread.Sleep(1000);
        }

        // 检查触发器是否启用
        if (!IsEnabled)
        {
            _logger.LogWarning("自动拾取: 触发器已禁用，跳过处理");
            return;
        }

        picking = true;
        using var foundRectArea = content.CaptureRectArea.Find(_pickRo);

        // 未匹配到拾取键
        if (foundRectArea.IsEmpty())
        {
            picking = false;
            return;
        }

        var config = TaskContext.Instance().Config.AutoPickConfig;
        string itemName = "";

        // 如果启用了白名单或黑名单，则需要进行OCR识别
        if (config.WhiteListEnabled || config.BlackListEnabled)
        {
            // 进行OCR识别获取物品名称
            itemName = OcrItemName(content, foundRectArea);

            // 处理OCR识别结果
            itemName = ProcessOcrText(itemName);

            // 未识别到结果
            if (string.IsNullOrEmpty(itemName))
            {
                picking = false;
                return;
            }

            // 白名单判断
            if (config.WhiteListEnabled && _whiteList.Contains(itemName))
            {
                goto Pick;
            }

            // 黑名单判断
            if (config.BlackListEnabled)
            {
                if (_blackList.Contains(itemName))
                {
                    _logger.LogDebug("自动拾取: 精确黑名单匹配，跳过拾取: {ItemName}", itemName);
                    picking = false;
                    return;
                }

                if (_fuzzyBlackList.Count > 0 && _fuzzyBlackList.Any(item => itemName.Contains(item)))
                {
                    _logger.LogDebug("自动拾取: 模糊黑名单匹配，跳过拾取: {itemName}", itemName);
                    picking = false;
                    return;
                }
            }

            // 如果启用了白名单但没有匹配到，则不拾取
            // if (config.WhiteListEnabled)
            // {
            //     _logger.LogDebug($"自动拾取: 白名单启用但未匹配，跳过拾取: {itemName}");
            //     return;
            // }
        }

        // 如果没有启用黑白名单，或者物品不在黑名单中，则执行拾取
        Pick:

        // 如果启用了芳间巡游，先点击右键触发范围采集
        if (config.FangJianXunYouEnabled)
        {
            Simulation.SendInput.Mouse.RightButtonDown();
            Thread.Sleep(40);
            Simulation.SendInput.Mouse.RightButtonUp();
            Thread.Sleep(100); // 等待范围采集动画
        }
        else
        {
            // 执行拾取按键
            Simulation.SendInput.Keyboard.KeyDown(_autoPickAssets.PickVk);
            Thread.Sleep(40);
            Simulation.SendInput.Keyboard.KeyUp(_autoPickAssets.PickVk);
        }

        LogPick(content, itemName);
        picking = false;
    }

    /// <summary>
    /// OCR识别拾取物品名称
    /// </summary>
    private string OcrItemName(CaptureContent content, Region pickKeyArea)
    {
        try
        {
            var config = TaskContext.Instance().Config.AutoPickConfig;

            // 计算识别区域：拾取键上方，高度50px，宽度200px
            var textRect = new Rect(
                pickKeyArea.X,
                pickKeyArea.Y - 50,
                200,
                50
            );

            // 边界检查
            if (textRect.X < 0 || textRect.Y < 0 ||
                textRect.X + textRect.Width > content.CaptureRectArea.SrcMat.Width ||
                textRect.Y + textRect.Height > content.CaptureRectArea.SrcMat.Height)
            {
                _logger.LogDebug("自动拾取: OCR区域超出边界");
                return string.Empty;
            }

            string text;

            // 根据配置的OCR引擎执行识别
            if (config.OcrEngine == nameof(PickOcrEngineEnum.Yap))
            {
                using var textMat = new Mat(content.CaptureRectArea.CacheGreyMat, textRect);
                text = TextInferenceFactory.Pick.Value.Inference(textMat);
            }
            else
            {
                // 默认使用Paddle
                using var textMat = new Mat(content.CaptureRectArea.SrcMat, textRect);
                text = OcrFactory.Paddle.OcrWithoutDetector(textMat);
            }

            if (!string.IsNullOrEmpty(text))
            {
                // 清理空白字符
                text = text.Trim();
                // _logger.LogDebug($"自动拾取: OCR识别结果({config.OcrEngine}): '{text}'");
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "自动拾取: OCR识别失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 相同文字前后3帧内只输出一次
    /// </summary>
    /// <param name="content"></param>
    /// <param name="text"></param>
    private void LogPick(CaptureContent content, string text)
    {
        if (_lastText != text || (_lastText == text && Math.Abs(content.FrameIndex - _prevClickFrameIndex) >= 5))
        {
            _logger.LogInformation("交互或拾取：{Text}", text);
        }

        _lastText = text;
        _prevClickFrameIndex = content.FrameIndex;
    }

    /// <summary>
    /// 高性能处理OCR识别的文字结果
    /// 1. 替换【、[ 为「，替换】、] 为」
    /// 2. 清理左边非「字符和中文的字符
    /// 3. 清理右边非」字符和中文的字符  
    /// 4. 确保引号配对：有「必有」，有」必有「
    /// </summary>
    /// <param name="text">OCR识别的原始文字</param>
    /// <returns>处理后的文字</returns>
    private static string ProcessOcrText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 0. 首先替换相似的括号字符并删除换行符、空格，使用Span<char>进行原地替换以获得最佳性能
        Span<char> chars = stackalloc char[text.Length];
        text.AsSpan().CopyTo(chars);

        int writeIndex = 0;
        bool hasChanges = false;

        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];

            // 跳过换行符、回车符、空格、制表符等空白字符
            if (char.IsWhiteSpace(c))
            {
                hasChanges = true;
                continue;
            }

            // 替换括号字符
            if (c == '【' || c == '[')
            {
                chars[writeIndex++] = '「';
                hasChanges = true;
            }
            else if (c == '】' || c == ']')
            {
                chars[writeIndex++] = '」';
                hasChanges = true;
            }
            else
            {
                chars[writeIndex++] = c;
            }
        }

        // 如果有变化，使用处理后的字符；否则使用原字符串的Span
        ReadOnlySpan<char> span = hasChanges ? chars.Slice(0, writeIndex) : text.AsSpan();
        int start = 0;
        int end = span.Length - 1;

        // 1. 从左边开始，删除非「字符和中文的字符
        while (start <= end)
        {
            char c = span[start];
            if (c == '「' || (c >= 0x4E00 && c <= 0x9FFF)) // 「字符或中文字符
                break;
            start++;
        }

        // 2. 从右边开始，删除非」字符和中文的字符
        while (end >= start)
        {
            char c = span[end];
            if (c == '」' || c == '！' || (c >= 0x4E00 && c <= 0x9FFF)) // 」字符或中文字符
                break;
            end--;
        }

        // 如果所有字符都被删除了
        if (start > end)
            return string.Empty;

        // 获取清理后的文字
        var cleanedSpan = span.Slice(start, end - start + 1);

        // 3. 检查并补充引号配对
        bool hasLeftQuote = false;
        bool hasRightQuote = false;

        // 快速扫描是否存在引号
        for (int i = 0; i < cleanedSpan.Length; i++)
        {
            if (cleanedSpan[i] == '「')
                hasLeftQuote = true;
            else if (cleanedSpan[i] == '」')
                hasRightQuote = true;
        }

        // 根据引号配对规则补充
        if (hasLeftQuote && !hasRightQuote)
        {
            // 有「但没有」，在末尾补充」
            return string.Concat(cleanedSpan, "」");
        }
        else if (hasRightQuote && !hasLeftQuote)
        {
            // 有」但没有「，在开头补充「
            return string.Concat("「", cleanedSpan);
        }

        return cleanedSpan.ToString();
    }
}