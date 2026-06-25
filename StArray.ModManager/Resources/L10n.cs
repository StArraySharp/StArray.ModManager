using System.Resources;

namespace StArray.ModManager.Resources;

/// <summary>基于 resx 的轻量本地化</summary>
public static class L10n
{
    private static readonly ResourceManager _rm = new(
        "StArray.ModManager.Resources.Localization",
        typeof(L10n).Assembly);

    /// <summary>获取本地化字符串，支持 {0} 占位</summary>
    public static string Get(string key, params object[] args)
    {
        var s = _rm.GetString(key) ?? key;
        return args.Length > 0 ? string.Format(s, args) : s;
    }
}
