using System.ComponentModel;

namespace KenketsuAvailability.Constants;

/// <summary>
/// 検索方向。
/// </summary>
public enum BloodDonationSearchModeEnum
{
    /// <summary>1日 × 複数の献血ルーム。</summary>
    [Description("ルーム横断")]
    Rooms,

    /// <summary>1つの献血ルーム × 複数の日。</summary>
    [Description("日付横断")]
    Dates
}
