using System.Diagnostics;

namespace BetterInfinityNikki.GameTask.AutoPick;

/// <summary>
/// 速度计时器，用于性能监控
/// </summary>
public class SpeedTimer
{
    private readonly Stopwatch _stopwatch;
    private readonly List<(string Label, long Time)> _records;
    private long _lastTime;

    public SpeedTimer()
    {
        _stopwatch = Stopwatch.StartNew();
        _records = new List<(string, long)>();
        _lastTime = 0;
    }

    public void Record(string label)
    {
        var currentTime = _stopwatch.ElapsedMilliseconds;
        _records.Add((label, currentTime - _lastTime));
        _lastTime = currentTime;
    }

    public void DebugPrint()
    {
        if (_records.Count == 0)
            return;

        var message = string.Join(" | ", _records.Select(r => $"{r.Label}: {r.Time}ms"));
        Debug.WriteLine($"[SpeedTimer] {message}");
    }
}
