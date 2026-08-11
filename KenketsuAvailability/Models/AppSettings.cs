using KenketsuAvailability.Constants;

namespace KenketsuAvailability.Models;

/// <summary>
/// 端末に保存する利用者ごとの設定。
/// 作者が配る設定（献血ルーム一覧・アクセス制限）はスプレッドシート側にある。
/// </summary>
public class AppSettings
{
    /// <summary>前回検索時に選択していた献血ルーム。</summary>
    public List<int> CenterIds { get; set; } = [];

    /// <summary>前回の対象日。過ぎていれば起動時に当日へ寄せる。</summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>ルーム1件あたりの取得所要秒数の実測値（指数移動平均）。未計測なら null。</summary>
    public double? SecPerRoom { get; set; }

    /// <summary>名前付きの献血ルーム選択セット。</summary>
    public List<PresetItem> Presets { get; set; } = [];

    /// <summary>前回の検索方向（ルーム横断／日付横断）。</summary>
    public BloodDonationSearchModeEnum SearchMode { get; set; } = BloodDonationSearchModeEnum.Rooms;

    /// <summary>日付横断で前回選択していた献血ルーム。</summary>
    public int? SingleCenterId { get; set; }

    /// <summary>日付横断で前回選択していた対象日。過ぎた日は読み込み時に落とす。</summary>
    public List<DateOnly> TargetDates { get; set; } = [];

    /// <summary>
    /// 直近のリクエスト時刻（ルーム1件＝1レコード）。1時間あたりの上限判定に使う。
    /// 再起動で上限が解除されては制限にならないので、設定と一緒に保存する。
    /// </summary>
    public List<DateTimeOffset> RequestLog { get; set; } = [];

    /// <summary>直近の検索が終わった時刻。ここからクールダウンを数える。</summary>
    public DateTimeOffset? LastSearchCompletedAt { get; set; }
}

/// <summary>献血ルームの選択セット。</summary>
public class PresetItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<int> CenterIds { get; set; } = [];
}
