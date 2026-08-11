using KenketsuAvailability.Constants;

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
    /// 献血種別ごとの取り扱いの有無。
    /// 先方ページは「取扱なし」も「その日満枠」も同じくタブ非活性になるため、
    /// HTMLからは区別できない。恒常的な取り扱いの有無はマスタ側で持つ。
    /// 全血のみのルーム、血小板を扱わないルームなどがあるため種別ごとに分けている。
    /// </summary>
    public bool OfferWhole400 { get; set; } = true;

    public bool OfferPpp { get; set; } = true;

    public bool OfferPcppp { get; set; } = true;

    /// <summary>この献血ルームがその種別を取り扱っているか。</summary>
    public bool Offers(BloodDonationTypeEnum bdType) => bdType switch
    {
        BloodDonationTypeEnum.Whole400 => OfferWhole400,
        BloodDonationTypeEnum.PPP => OfferPpp,
        BloodDonationTypeEnum.PCPPP => OfferPcppp,
        _ => true,
    };
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
