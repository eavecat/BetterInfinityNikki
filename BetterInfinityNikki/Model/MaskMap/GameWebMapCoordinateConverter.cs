namespace BetterInfinityNikki.Model.MaskMap;

/// <summary>
/// 游戏与Web地图坐标转换器
/// 用于在游戏坐标、Web地图坐标和图像坐标之间进行转换
/// </summary>
public static class GameWebMapCoordinateConverter
{
    /// <summary>
    /// Web地图原点偏移 X（需要根据实际游戏调整）
    /// </summary>
    public const double WebOffsetOriginX = 0.0;

    /// <summary>
    /// Web地图原点偏移 Y（需要根据实际游戏调整）
    /// </summary>
    public const double WebOffsetOriginY = 0.0;

    /// <summary>
    /// 图像地图缩放比例（例如：2048级别地图）
    /// </summary>
    public const double ImageScaleFactor = 1.0;

    /// <summary>
    /// 游戏坐标转Web地图坐标
    /// </summary>
    /// <param name="gameX">游戏坐标 X</param>
    /// <param name="gameY">游戏坐标 Y</param>
    /// <returns>Web地图坐标 (x, y)</returns>
    public static (double x, double y) GameToWeb(double gameX, double gameY)
    {
        // 根据实际游戏的坐标系进行调整
        var x = gameX - WebOffsetOriginX;
        var y = gameY - WebOffsetOriginY;
        return (x, y);
    }

    /// <summary>
    /// Web地图坐标转游戏坐标
    /// </summary>
    /// <param name="webX">Web地图坐标 X</param>
    /// <param name="webY">Web地图坐标 Y</param>
    /// <returns>游戏坐标 (x, y)</returns>
    public static (double x, double y) WebToGame(double webX, double webY)
    {
        var x = webX + WebOffsetOriginX;
        var y = webY + WebOffsetOriginY;
        return (x, y);
    }

    /// <summary>
    /// 游戏坐标转图像坐标
    /// </summary>
    /// <param name="gameX">游戏坐标 X</param>
    /// <param name="gameY">游戏坐标 Y</param>
    /// <param name="scaleFactor">缩放比例（可选，默认为 ImageScaleFactor）</param>
    /// <returns>图像坐标 (x, y)</returns>
    public static (double x, double y) GameToImage(double gameX, double gameY, double? scaleFactor = null)
    {
        var scale = scaleFactor ?? ImageScaleFactor;
        
        // 先转换为Web坐标
        var (webX, webY) = GameToWeb(gameX, gameY);
        
        // 再缩放到图像坐标
        var imageX = webX * scale;
        var imageY = webY * scale;
        
        return (imageX, imageY);
    }

    /// <summary>
    /// 图像坐标转游戏坐标
    /// </summary>
    /// <param name="imageX">图像坐标 X</param>
    /// <param name="imageY">图像坐标 Y</param>
    /// <param name="scaleFactor">缩放比例（可选，默认为 ImageScaleFactor）</param>
    /// <returns>游戏坐标 (x, y)</returns>
    public static (double x, double y) ImageToGame(double imageX, double imageY, double? scaleFactor = null)
    {
        var scale = scaleFactor ?? ImageScaleFactor;
        
        // 先转换为Web坐标
        var webX = imageX / scale;
        var webY = imageY / scale;
        
        // 再转换为游戏坐标
        return WebToGame(webX, webY);
    }
}
