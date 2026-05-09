using System.Text.RegularExpressions;

namespace BetterInfinityNikki.Helpers;

internal static partial class RegexHelper
{
    [GeneratedRegex(@"[^0-9]+")]
    public static partial Regex ExcludeNumberRegex();

    [GeneratedRegex(@"^[0-9]+$")]
    public static partial Regex FullNumberRegex();
}
