using AngleSharp;
using AngleSharp.Io;

namespace KenketsuAvailability.Services;

/// <summary>
/// AngleSharp の IBrowsingContext を生成する共通ファクトリ。
/// すべてのコンテキストに共通の User-Agent（連絡先付き）を設定する。
/// </summary>
public static class AnglesharpContextFactory
{
    private const string UserAgent = $"AngleSharp / {BotContactInfo.Suffix}";

    /// <summary>
    /// 標準的な IBrowsingContext を生成する（Cookie あり）。
    /// </summary>
    public static IBrowsingContext Create()
    {
        var config = Configuration.Default
            .With(new DefaultHttpRequester(UserAgent))
            .WithDefaultLoader()
            .WithDefaultCookies();
        return BrowsingContext.New(config);
    }
}
