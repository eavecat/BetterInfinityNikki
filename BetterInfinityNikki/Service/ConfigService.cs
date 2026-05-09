using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterInfinityNikki.Core.Config;

namespace BetterInfinityNikki.Service;

public class ConfigService : Interface.IConfigService
{
    private readonly string _configPath;
    private AllConfig? _config;

    public ConfigService()
    {
        _configPath = Path.Combine(Global.StartUpPath, "User", "config.json");
    }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            // new OpenCvPointJsonConverter(),
            // new OpenCvRectJsonConverter(),
        },
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// 写入只有UI线程会调用
    /// 多线程只会读，放心用static，不会丢失数据
    /// </summary>
    public static AllConfig? Config { get; private set; }

    public AllConfig Get()
    {
        if (_config != null)
        {
            return _config;
        }

        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<AllConfig>(json, JsonOptions) ?? new AllConfig();
            }
            else
            {
                _config = new AllConfig();
                Save();
            }
            
            // 注册自动保存（对齐 BetterGI）
            _config.OnAnyChangedAction = Save;
            _config.InitEvent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取配置文件失败: {ex.Message}");
            _config = new AllConfig();
        }

        return _config;
    }

    public void Save()
    {
        try
        {
            // 确保配置已加载
            if (_config == null)
            {
                _config = new AllConfig();
            }

            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_config, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置文件失败: {ex.Message}");
        }
    }
}