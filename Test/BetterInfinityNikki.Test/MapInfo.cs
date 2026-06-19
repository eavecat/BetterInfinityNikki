namespace BetterInfinityNikki.Test;

public class MapInfo
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string MapResourceUrl { get; set; } = "";
    public int Level { get; set; }
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int EndX { get; set; }
    public int EndY { get; set; }

    public static readonly Dictionary<string, MapInfo> Maps = new()
    {
        ["NikkiWorld"] = new()
        {
            Key = "NikkiWorld", Name = "大地图", MapResourceUrl = "65D9DALkhQDDFoe",
            Level = 6,
            StartX = 0, StartY = 0,
            EndX = 63, EndY = 63
        },
        ["WoNiuCheng"] = new()
        {
            Key = "WoNiuCheng", Name = "蜗牛城", MapResourceUrl = "tiles-wnc",
            Level = 6,
            StartX = 0, StartY = 0,
            EndX = 63, EndY = 40
        },
        ["WanXiangJing"] = new()
        {
            Key = "WanXiangJing", Name = "万相境", MapResourceUrl = "tiles-wxj",
            Level = 5,
            StartX = 0, StartY = 0,
            EndX = 31, EndY = 23
        },
        ["HuaYanQunDao"] = new()
        {
            Key = "HuaYanQunDao", Name = "花焰群岛", MapResourceUrl = "tiles-hyqd-20250311",
            Level = 4,
            StartX = 0, StartY = 0,
            EndX = 15, EndY = 7
        },
        ["WuYouDao"] = new()
        {
            Key = "WuYouDao", Name = "无忧岛", MapResourceUrl = "tiles-wyd",
            Level = 4,
            StartX = 0, StartY = 0,
            EndX = 15, EndY = 13
        },
        ["DanQingYu"] = new()
        {
            Key = "DanQingYu", Name = "丹青屿", MapResourceUrl = "tiles-bsj-16384",
            Level = 4,
            StartX = 0, StartY = 0,
            EndX = 15, EndY = 10
        },
        ["DanQingZhiJing"] = new()
        {
            Key = "DanQingZhiJing", Name = "丹青之境", MapResourceUrl = "tiles-lsj-16384",
            Level = 4,
            StartX = 0, StartY = 0,
            EndX = 15, EndY = 10
        },
    };
}