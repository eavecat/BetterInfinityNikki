using System.Diagnostics;
using System.IO;
using System.Windows;
using BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

namespace BetterInfinityNikki.Test;

public partial class MainWindow : Window
{
    private MapInfo SelectedMap => (MapInfo)MapSelector.SelectedItem;

    private string GetTilesDir(MapInfo map) => Path.Combine(@"D:\tmp\nikki", map.Key, "tiles");
    private string GetFullMapPath(MapInfo map) => Path.Combine(@"D:\tmp\nikki", map.Key, "full_map.png");
    private string GetOutputDir(MapInfo map) => Path.Combine(@"D:\tmp\nikki", map.Key);

    public MainWindow()
    {
        InitializeComponent();
        MapSelector.ItemsSource = MapInfo.Maps.Values.ToList();
        MapSelector.SelectedIndex = 0;
    }

    private void OnDownloadTiles(object sender, RoutedEventArgs e)
    {
        var map = SelectedMap;
        var tilesDir = GetTilesDir(map);
        StatusText.Text = $"正在下载 {map.Name} 切片...";

        Task.Run(async () =>
        {
            await MapTileDownloader.DownloadTilesAsync(map, tilesDir, log =>
            {
                Dispatcher.Invoke(() => StatusText.Text = log);
            });

            Dispatcher.Invoke(() => StatusText.Text = $"下载完成！目录: {tilesDir}");
        });
    }

    private void OnMergeTiles(object sender, RoutedEventArgs e)
    {
        var map = SelectedMap;
        var tilesDir = GetTilesDir(map);
        var fullMapPath = GetFullMapPath(map);

        if (!Directory.Exists(tilesDir) || !Directory.GetFiles(tilesDir, "*.webp").Any())
        {
            StatusText.Text = $"切片目录为空，请先下载: {tilesDir}";
            return;
        }

        StatusText.Text = $"正在合并 {map.Name} 切片...";

        Task.Run(() =>
        {
            try
            {
                var tileCount = Directory.GetFiles(tilesDir, "*.webp").Length;
                Dispatcher.Invoke(() => StatusText.Text = $"正在合并 {tileCount} 个切片...");

                var sw = Stopwatch.StartNew();

                MapTileMerger.MergeTiles(
                    tilesDirectory: tilesDir,
                    outputPath: fullMapPath,
                    startX: map.StartX,
                    startY: map.StartY,
                    endX: map.EndX,
                    endY: map.EndY,
                    log: msg => Dispatcher.Invoke(() => StatusText.Text = msg)
                );

                sw.Stop();

                Dispatcher.Invoke(() =>
                {
                    var size = new FileInfo(fullMapPath).Length / 1024 / 1024;
                    StatusText.Text = $"合并完成！{size}MB，耗时 {sw.Elapsed.TotalSeconds:F1}秒\n{fullMapPath}";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => StatusText.Text = $"合并失败: {ex.Message}\n{ex.StackTrace}");
            }
        });
    }

    private void OnGenerateFeatures(object sender, RoutedEventArgs e)
    {
        var map = SelectedMap;
        var fullMapPath = GetFullMapPath(map);
        var outputDir = GetOutputDir(map);

        if (!File.Exists(fullMapPath))
        {
            StatusText.Text = $"文件不存在: {fullMapPath}\n请先合并切片";
            return;
        }

        var fileSize = new FileInfo(fullMapPath).Length / 1024 / 1024;
        StatusText.Text = $"正在生成 {map.Name} 特征数据...（{fileSize}MB）";

        Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();

            try
            {
                MapFeatureGenerator.GenerateFeatures(
                    mapImagePath: fullMapPath,
                    outputDirectory: outputDir,
                    outputName: $"{map.Key}_0");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => StatusText.Text = $"失败: {ex.Message}");
                return;
            }

            sw.Stop();

            Dispatcher.Invoke(() =>
            {
                var kpFile = Path.Combine(outputDir, $"{map.Key}_0_SIFT.kp.bin");
                var matFile = Path.Combine(outputDir, $"{map.Key}_0_SIFT.mat.png");

                StatusText.Text = File.Exists(kpFile) && File.Exists(matFile)
                    ? $"完成！耗时 {sw.Elapsed.TotalSeconds:F1}秒\n{kpFile}\n{matFile}"
                    : $"失败！文件未生成\n检查: {outputDir}";
            });
        });
    }

    private void OnCopyFeatures(object sender, RoutedEventArgs e)
    {
        var map = SelectedMap;
        var srcDir = GetOutputDir(map);
        var destDir = Path.Combine(@"D:\code\better-game-assistant\BetterInfinityNikki\Assets\Map", map.Key);

        var kpFile = Path.Combine(srcDir, $"{map.Key}_0_SIFT.kp.bin");
        var matFile = Path.Combine(srcDir, $"{map.Key}_0_SIFT.mat.png");

        if (!File.Exists(kpFile) || !File.Exists(matFile))
        {
            StatusText.Text = $"特征文件不存在，请先生成\n{kpFile}\n{matFile}";
            return;
        }

        Directory.CreateDirectory(destDir);

        var destKp = Path.Combine(destDir, $"{map.Key}_0_SIFT.kp.bin");
        var destMat = Path.Combine(destDir, $"{map.Key}_0_SIFT.mat.png");

        File.Copy(kpFile, destKp, true);
        File.Copy(matFile, destMat, true);

        StatusText.Text = $"已复制到项目目录:\n{destKp}\n{destMat}";
    }
}
