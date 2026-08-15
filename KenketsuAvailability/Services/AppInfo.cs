using System.Reflection;

namespace KenketsuAvailability.Services;

/// <summary>
/// 実行中のアプリ自身のバージョン。作者のスプレッドシートによる起動可否の判定に使う。
/// </summary>
public static class AppInfo
{
    /// <summary>「1.1.0」形式のバージョン。取得できなければ "0.0.0"。</summary>
    public static string Version { get; } = ReadVersion();

    /// <summary>比較用。取得できなければ 0.0.0。</summary>
    public static Version ParsedVersion { get; } =
        System.Version.TryParse(Version, out var v) ? v : new Version(0, 0, 0);

    private static string ReadVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        //InformationalVersion は「1.1.0+コミットハッシュ」の形になることがある
        if (!string.IsNullOrWhiteSpace(informational))
        {
            string trimmed = informational.Split('+')[0].Trim();
            if (System.Version.TryParse(trimmed, out _)) return trimmed;
        }

        var assemblyVersion = assembly.GetName().Version;
        //AssemblyVersion は 1.1.0.0 のように4桁になるので3桁へ落とす
        return assemblyVersion == null
            ? "0.0.0"
            : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
    }
}
