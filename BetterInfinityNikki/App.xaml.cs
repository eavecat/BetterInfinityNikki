using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using BetterInfinityNikki.Helpers.Extensions;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition.ONNX;
using BetterInfinityNikki.Core.Recognition.OCR;
using BetterInfinityNikki.GameTask;
using BetterInfinityNikki.Service;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.View;
using BetterInfinityNikki.ViewModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LazyCache;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.RichTextBox.Abstraction;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using Vanara.PInvoke;

namespace BetterInfinityNikki;

public partial class App : Application
{
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureLogging(builder => { builder.ClearProviders(); })
        .ConfigureServices((context, services) =>
            {
                // 初始化配置服务
                var configService = new ConfigService();
                services.AddSingleton<IConfigService>(configService);
                
                // 日志配置
                var logFolder = Path.Combine(AppContext.BaseDirectory, "log");
                Directory.CreateDirectory(logFolder);
                var logFile = Path.Combine(logFolder, "better-infinity-nikki.log");

                var all = configService.Get();
                
                // 使用 Serilog.Sinks.RichTextBoxEx.Wpf 库提供的 RichTextBoxImpl
                var richTextBox = new RichTextBoxImpl();
                services.AddSingleton<IRichTextBox>(richTextBox);

                var loggerConfiguration = new LoggerConfiguration()
                    .WriteTo.File(logFile,
                        outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 31,
                        retainedFileTimeLimit: TimeSpan.FromDays(21))
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning);
                
                // 如果遮罩窗口启用，添加 RichTextBox 日志输出
                if (all.MaskWindowConfig.MaskEnabled)
                {
                    loggerConfiguration.WriteTo.RichTextBox(richTextBox, LogEventLevel.Information,
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
                }

                Log.Logger = loggerConfiguration.CreateLogger();

                services.AddLogging(c => c.AddSerilog());

                // 导航服务
                services.AddNavigationViewPageProvider();
                
                // 应用宿主服务
                services.AddHostedService<ApplicationHostService>();
                
                // 页面解析服务
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();

                // Main window with navigation
                services.AddView<INavigationWindow, MainWindow, MainWindowViewModel>();
                services.AddSingleton<NotifyIconViewModel>();

                // Views - Pages (带 ViewModel 的页面使用 AddView)
                services.AddView<View.Pages.HomePage, ViewModel.Pages.HomePageViewModel>();
                services.AddView<View.Pages.TriggerSettingsPage, ViewModel.Pages.TriggerSettingsPageViewModel>();
                services.AddView<View.Pages.CommonSettingsPage, ViewModel.Pages.CommonSettingsPageViewModel>();
                
                // OCR Services
                services.AddSingleton<BgiOnnxFactory>();
                services.AddSingleton<OcrFactory>();
                
                // My Services
                services.AddSingleton<GameTask.TaskTriggerDispatcher>();
                
                // Nikki Map API Service
                services.AddSingleton<NikkiMapApiService>(sp =>
                    new NikkiMapApiService(new HttpClient(new HttpClientHandler
                    {
                        AutomaticDecompression = System.Net.DecompressionMethods.All
                    }), sp.GetRequiredService<ILogger<NikkiMapApiService>>()));
                
                // Mask Map Point Service
                services.AddSingleton<IMaskMapPointService, MaskMapPointService>();

                // Update Service
                services.AddSingleton<IUpdateService, UpdateService>();

                // Memory Cache & File Cache (for icon loading)
                services.AddMemoryCache();
                services.AddSingleton<IAppCache, CachingService>();
                services.AddSingleton<MemoryFileCache>();
                services.AddSingleton(TimeProvider.System);
            }
        )
        .Build();

    public static IServiceProvider ServiceProvider => _host.Services;

    public static ILogger<T> GetLogger<T>()
    {
        return _host.Services.GetService<ILogger<T>>()!;
    }

    public static T? GetService<T>() where T : class
    {
        return _host.Services.GetService(typeof(T)) as T;
    }

    public static object? GetService(Type type)
    {
        return _host.Services.GetService(type);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            RegisterEvents();
            await _host.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"应用程序启动失败: {ex.Message}");
            HandleException(ex);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        await _host.StopAsync();
        _host.Dispose();
    }

    private void RegisterEvents()
    {
        TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
        this.DispatcherUnhandledException += AppDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
    }

    private static void TaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            e.SetObserved();
        }
    }

    private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException(exception);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    private static void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            HandleException(e.Exception);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            e.Handled = true;
        }
    }

    private static void HandleException(Exception e)
    {
        if (e.InnerException != null)
        {
            e = e.InnerException;
        }

        System.Windows.Forms.MessageBox.Show(
            $"""
             程序异常：{e.Source}
             --
             {e.StackTrace}
             --
             {e.Message}
             """
        );

        GetLogger<App>().LogDebug(e, "UnHandle Exception");
    }
}
