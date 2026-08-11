namespace KenketsuAvailability.Models;

/// <summary>
/// 献血ルーム1件。作者のスプレッドシートの centers シート1行に対応する。
/// </summary>
public class BloodDonationCenter
{
    public int CenterId { get; set; }

    public string? CenterName { get; set; }

    public string? PlaceId { get; set; }

    /// <summary>所在都道府県（例：東京都）。画面のグループ表示・一括選択に使う。</summary>
    public string? Prefecture { get; set; }

    /// <summary>
    /// 血小板献血の取り扱いがないルームか。
    /// 先方ページは「取扱なし」も「その日満枠」も同じくタブ非活性になるため、
    /// HTMLからは区別できない。恒常的に取り扱いがないルームだけこのフラグで持つ。
    /// </summary>
    public bool NoPlatelet { get; set; }
}

/// <summary>献血ルームマスタのファイル形式（Data/centers.json）。</summary>
public class CenterMasterFile
{
    /// <summary>マスタを書き出した日（画面に出す更新日）。</summary>
    public string? UpdatedAt { get; set; }

    public List<BloodDonationCenter> Centers { get; set; } = [];
}

/// <summary>都道府県ごとにまとめた献血ルーム（一括選択の単位）。</summary>
public class BloodDonationCenterGroup
{
    public string Prefecture { get; set; } = string.Empty;

    public List<BloodDonationCenter> Centers { get; set; } = [];
}

/// <summary>地方ブロックごとにまとめた都道府県（画面で折りたたむ単位）。</summary>
public class BloodDonationRegionGroup
{
    public string Region { get; set; } = string.Empty;

    public List<BloodDonationCenterGroup> PrefectureGroups { get; set; } = [];
}
