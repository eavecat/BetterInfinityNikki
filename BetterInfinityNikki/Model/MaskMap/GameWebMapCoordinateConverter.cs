namespace BetterInfinityNikki.Model.MaskMap;

public static class GameWebMapCoordinateConverter
{
    // 如果点位偏左上，减小此值；偏右下，增大此值
    // 每调整 64 对应图像坐标偏移 1 像素
    private const double OffsetBase = 16384 + 64 * -36;
    private const double WebToImageScale = 1.0 / 64.0;

    public static (double imageX, double imageY) WebToImage(double webX, double webY)
    {
        var offsetX = (webX / OffsetBase * 128);
        var offsetY = (webY / OffsetBase * 128);
        var imageX = (webX - offsetX) * WebToImageScale;
        var imageY = (webY - offsetY) * WebToImageScale;
        return (imageX, imageY);
    }

    public static (double gameX, double gameY) WebToGame(double webX, double webY)
    {
        var gameX = 4096.0 - webX / 128.0;
        var gameY = 4096.0 - webY / 128.0;
        return (gameX, gameY);
    }
}