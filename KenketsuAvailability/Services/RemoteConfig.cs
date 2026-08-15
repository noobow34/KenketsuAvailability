using KenketsuAvailability.Models;

namespace KenketsuAvailability.Services;

/// <summary>作者からのメッセージの書式。</summary>
public enum MessageFormat
{
    /// <summary>プレーンテキスト。改行はそのまま表示し、HTMLとしては解釈しない。</summary>
    Text,

    /// <summary>HTML。作者が書いたものをそのまま描画する。</summary>
    Html
}

/// <summary>
/// 作者のGoogleスプレッドシートから読み込む設定。
/// 献血ルーム一覧（placeId）とアクセス制限値もここに含まれるため、
/// アプリを配り直さずに内容を差し替えられる。
/// </summary>
public class RemoteConfig
{
    /// <summary>
    /// アプリを使用可能にするか。スプレッドシート側でこれを落とすと、
    /// アプリは検索を含むすべての機能を停止する。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>作者から利用者へのお知らせ。空なら何も表示しない。</summary>
    public string Message { get; set; } = "";

    public MessageFormat MessageFormat { get; set; } = MessageFormat.Text;

    /// <summary>ルーム1件ごとのウェイト（ミリ秒）。</summary>
    public int RoomIntervalMs { get; set; } = DefaultRoomIntervalMs;

    /// <summary>1回の検索で取得できるルーム数の上限（ルーム横断）。</summary>
    public int MaxRoomsPerSearch { get; set; } = DefaultMaxRoomsPerSearch;

    /// <summary>1回の検索で取得できる日数の上限（日付横断）。</summary>
    public int MaxDatesPerSearch { get; set; } = DefaultMaxDatesPerSearch;

    /// <summary>検索が終わってから次の検索を始められるまでの待ち時間（秒）。</summary>
    public int CooldownSeconds { get; set; } = DefaultCooldownSeconds;

    /// <summary>直近1時間に投げられるリクエスト数の上限。</summary>
    public int HourlyRequestLimit { get; set; } = DefaultHourlyRequestLimit;

    /// <summary>
    /// これ未満のバージョンは使用不可にする（例：1.2.0）。空なら下限なし。
    /// 先方サイトのHTML変更に追従できていない古い版を止めるために使う。
    /// </summary>
    public string MinAppVersion { get; set; } = "";

    /// <summary>個別に使用不可にするバージョン。特定の版にだけ問題が見つかった場合に使う。</summary>
    public List<string> BlockedVersions { get; set; } = [];

    /// <summary>献血ルーム一覧。</summary>
    public List<BloodDonationCenter> Centers { get; set; } = [];

    /// <summary>
    /// 指定のバージョンが使用可能か。使用不可なら理由を返す。
    /// 設定が空・不正なら制限なしとして通す（設定ミスで全員が締め出されるのを避ける）。
    /// </summary>
    public (bool Allowed, string Reason) CheckVersion(Version current)
    {
        if (BlockedVersions.Any(v => string.Equals(v, current.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "このバージョンは利用できなくなりました。");
        }

        if (System.Version.TryParse(MinAppVersion, out var min) && current < min)
        {
            return (false, $"バージョン {min} 以降が必要です。");
        }

        return (true, "");
    }

    // ── 既定値と、スプレッドシート側の値を丸める範囲 ────────────────
    // スプレッドシートの入力ミスで、先方サイトへ極端な負荷をかける設定になるのを防ぐ。

    public const int DefaultRoomIntervalMs = 300;
    public const int DefaultMaxRoomsPerSearch = 50;
    public const int DefaultMaxDatesPerSearch = 7;
    public const int DefaultCooldownSeconds = 30;
    public const int DefaultHourlyRequestLimit = 200;

    public const int MinRoomIntervalMs = 100;
    public const int MaxRoomIntervalMs = 60_000;
    public const int MinMaxRoomsPerSearch = 1;
    public const int MaxMaxRoomsPerSearch = 100;
    public const int MinMaxDatesPerSearch = 1;
    public const int MaxMaxDatesPerSearch = 60;
    public const int MinCooldownSeconds = 0;
    public const int MaxCooldownSeconds = 3600;
    public const int MinHourlyRequestLimit = 1;
    public const int MaxHourlyRequestLimit = 1000;
}
