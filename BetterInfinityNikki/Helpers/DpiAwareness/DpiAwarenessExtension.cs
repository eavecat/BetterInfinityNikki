using System.Windows;

namespace BetterInfinityNikki.Helpers.DpiAwareness;

internal static class DpiAwarenessExtension
{
    public static void InitializeDpiAwareness(this Window window)
    {
        _ = new DpiAwarenessController(window);
    }
}