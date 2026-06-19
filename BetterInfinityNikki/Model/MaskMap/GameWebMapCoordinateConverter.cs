using BetterInfinityNikki.Model;

namespace BetterInfinityNikki.Model.MaskMap;

public static class GameWebMapCoordinateConverter
{
    public static (double imageX, double imageY) WebToImage(double webX, double webY, MapFeatureConfig config)
    {
        var offsetBaseX = config.OffsetBaseX;
        var offsetBaseY = config.OffsetBaseY;
        var scale = config.WebToImageScale;
        var offsetX = webX / offsetBaseX * 128;
        var offsetY = webY / offsetBaseY * 128;
        return ((webX - offsetX) * scale, (webY - offsetY) * scale);
    }

    public static (double gameX, double gameY) WebToGame(double webX, double webY)
    {
        var gameX = 4096.0 - webX / 128.0;
        var gameY = 4096.0 - webY / 128.0;
        return (gameX, gameY);
    }
}
