using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BetterInfinityNikki.Helpers;

public class SpeedTimer
{
    private readonly Stopwatch _stopwatch;

    private readonly Dictionary<string, TimeSpan> _timeRecordDic = new();

    private readonly string _name = string.Empty;

    public SpeedTimer()
    {
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public SpeedTimer(string name)
    {
        _name = name;
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void Record(string name)
    {
        _timeRecordDic[name] = _stopwatch.Elapsed;
        _stopwatch.Restart();
    }

    public double GetRecordTime(string name)
    {
        return _timeRecordDic.TryGetValue(name, out var timeSpan) ? timeSpan.TotalMilliseconds : 0;
    }

    public void DebugPrint()
    {
        var msg = _name;
        if (!string.IsNullOrEmpty(msg))
        {
            msg += " : ";
        }

        foreach (var pair in _timeRecordDic)
        {
            msg += $"{pair.Key}:{pair.Value.TotalMilliseconds}ms,";
        }

        if (msg.Length > 0)
        {
            Debug.WriteLine(msg[..^1]);
        }

        _stopwatch.Stop();
    }
}
