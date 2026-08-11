namespace KenketsuAvailability.Services;

/// <summary>
/// 日本赤十字社のサイトへのアクセス量を抑えるためのレートリミット。
/// 一般配布したときに、多数の利用者が同時に叩いても先方の負担にならない水準に抑える。
///
/// 制限値は作者のスプレッドシートから読み込むので、アプリを配り直さずに締められる。
/// カウンタは設定ファイルに永続化する。アプリを再起動すれば解除できる、では制限にならないため。
/// </summary>
public class RateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly SettingsStore _store;
    private readonly RemoteConfigService _remote;

    public RateLimiter(SettingsStore store, RemoteConfigService remote)
    {
        _store = store;
        _remote = remote;
    }

    /// <summary>ルーム1件ごとのウェイト。連続アクセスの間隔をあける。</summary>
    public int RoomIntervalMs => _remote.Config.RoomIntervalMs;

    /// <summary>1回の検索で取得できるルーム数の上限。</summary>
    public int MaxRoomsPerSearch => _remote.Config.MaxRoomsPerSearch;

    /// <summary>検索が終わってから次の検索を始められるまでの待ち時間（秒）。</summary>
    public int CooldownSeconds => _remote.Config.CooldownSeconds;

    /// <summary>直近1時間に投げられるリクエスト（＝ルーム取得）数の上限。</summary>
    public int HourlyRequestLimit => _remote.Config.HourlyRequestLimit;

    private List<DateTimeOffset> Log => _store.Settings.RequestLog;

    /// <summary>集計対象は直近1時間分だけ。時計が巻き戻された場合の未来の記録も捨てる。</summary>
    private void Prune()
    {
        var now = DateTimeOffset.Now;
        Log.RemoveAll(t => now - t >= Window || t > now);
    }

    /// <summary>この1時間にあと何件取得できるか。</summary>
    public int RemainingHourly
    {
        get
        {
            Prune();
            return Math.Max(0, HourlyRequestLimit - Log.Count);
        }
    }

    /// <summary>次の検索を始められるまでの残り秒数。0なら今すぐ始められる。</summary>
    public int CooldownRemainingSeconds
    {
        get
        {
            if (_store.Settings.LastSearchCompletedAt is not DateTimeOffset last) return 0;
            double remain = CooldownSeconds - (DateTimeOffset.Now - last).TotalSeconds;
            if (remain <= 0) return 0;
            //時計が巻き戻されても待ち時間がクールダウンを超えないようにする
            return (int)Math.Ceiling(Math.Min(remain, CooldownSeconds));
        }
    }

    /// <summary>1時間の枠が1件でも空くまでの時間。空きがあれば null。</summary>
    public TimeSpan? RecoverAfter
    {
        get
        {
            Prune();
            if (Log.Count < HourlyRequestLimit) return null;
            var remain = Log.Min() + Window - DateTimeOffset.Now;
            return remain < TimeSpan.Zero ? TimeSpan.Zero : remain;
        }
    }

    /// <summary>
    /// この件数で検索を始めてよいか。ダメな場合は画面に出す理由を返す。
    /// </summary>
    public (bool Ok, string Message) CanStart(int roomCount)
    {
        if (roomCount > MaxRoomsPerSearch)
        {
            return (false, $"1回の検索で取得できるのは{MaxRoomsPerSearch}ルームまでです。\n"
                         + $"選択を{MaxRoomsPerSearch}件以下に減らしてください（現在{roomCount}件）。");
        }

        int cooldown = CooldownRemainingSeconds;
        if (cooldown > 0)
        {
            return (false, $"連続した検索を避けるため、あと{cooldown}秒お待ちください。");
        }

        int remaining = RemainingHourly;
        if (roomCount > remaining)
        {
            string recover = RecoverAfter is TimeSpan span
                ? $"約{Math.Max(1, (int)Math.Ceiling(span.TotalMinutes))}分後に枠が回復します。"
                : "しばらく待つと枠が回復します。";
            return (false, $"1時間あたりの取得上限（{HourlyRequestLimit}件）に達します。\n"
                         + $"あと{remaining}件しか取得できません。{recover}");
        }

        return (true, "");
    }

    /// <summary>ルーム1件を取得したことを記録する。</summary>
    public void RecordRequest()
    {
        Prune();
        Log.Add(DateTimeOffset.Now);
        _store.Save();
    }

    /// <summary>検索の終了を記録する。ここからクールダウンが始まる。</summary>
    public void FinishSearch()
    {
        _store.Settings.LastSearchCompletedAt = DateTimeOffset.Now;
        _store.Save();
    }
}
