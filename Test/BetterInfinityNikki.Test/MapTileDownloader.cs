using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace BetterInfinityNikki.Test;

public static class MapTileDownloader
{
    private const int Concurrency = 30;
    private const int DelayMinMs = 1000;
    private const int DelayMaxMs = 1500;

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly Random Random = new();

    public static async Task DownloadTilesAsync(MapInfo map, string outputDir, Action<string>? onLog = null)
    {
        Directory.CreateDirectory(outputDir);

        var baseUrl = $"https://assets.papegames.com/maps/{map.MapResourceUrl}";
        var tasks = new List<(int x, int y, string filename, string url)>();

        for (int x = map.StartX; x <= map.EndX; x++)
        {
            for (int y = map.StartY; y <= map.EndY; y++)
            {
                var filename = $"6-{x}-{y}.webp";
                var url = $"{baseUrl}/{filename}?x-oss-process=image/format,webp";
                tasks.Add((x, y, filename, url));
            }
        }

        onLog?.Invoke($"总任务数: {tasks.Count}");

        var queue = new Queue<(int x, int y, string filename, string url)>(tasks);
        var failed = new List<string>();
        var semaphore = new SemaphoreSlim(Concurrency);
        var workers = new List<Task>();

        while (queue.Count > 0)
        {
            var task = queue.Dequeue();
            await semaphore.WaitAsync();

            workers.Add(Task.Run(async () =>
            {
                try
                {
                    await RandomDelay();
                    var filePath = Path.Combine(outputDir, task.filename);

                    if (File.Exists(filePath))
                    {
                        onLog?.Invoke($"[SKIP] {task.filename}");
                        return;
                    }

                    var bytes = await HttpClient.GetByteArrayAsync(task.url);
                    await File.WriteAllBytesAsync(filePath, bytes);
                    onLog?.Invoke($"[OK] {task.filename}");
                }
                catch (Exception ex)
                {
                    failed.Add($"{task.url} | {task.filename} | {ex.Message}");
                    onLog?.Invoke($"[FAIL] {task.filename} - {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(workers);

        if (failed.Count > 0)
        {
            var failedLog = Path.Combine(outputDir, "failed.txt");
            await File.WriteAllTextAsync(failedLog, string.Join("\n", failed));
            onLog?.Invoke($"失败数: {failed.Count}，已记录到 {failedLog}");
        }
        else
        {
            onLog?.Invoke("全部下载完成，无失败记录");
        }
    }

    private static Task RandomDelay()
    {
        var ms = Random.Next(DelayMinMs, DelayMaxMs + 1);
        return Task.Delay(ms);
    }
}
