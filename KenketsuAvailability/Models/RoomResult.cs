using KenketsuAvailability.Constants;
using KenketsuAvailability.Services;

namespace KenketsuAvailability.Models;

/// <summary>
/// 結果カード1枚分。取得できたものから順に画面へ積んでいく。
/// ルーム横断では献血ルーム1件、日付横断では1日分にあたる。
/// </summary>
public class RoomResult
{
    /// <summary>描画時の @key。ルーム横断は CenterId、日付横断は日付。</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>カードの見出し。ルーム横断はルーム名、日付横断は日付。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 都道府県の変わり目で結果グリッドを折り返すために保持する。
    /// 日付横断では折り返さないので null。
    /// </summary>
    public string? Prefecture { get; set; }

    public string ReserveUrl { get; set; } = string.Empty;

    /// <summary>
    /// 先方HTMLでは「血小板の取扱なし」と「その日は満枠」がどちらもタブ非活性で区別できないため、
    /// マスタのフラグで判断する。
    /// </summary>
    public bool NoPlatelet { get; set; }

    public List<TypeResult> Types { get; set; } = [];

    /// <summary>取得に失敗した場合のメッセージ。成功時は null。</summary>
    public string? Error { get; set; }
}

/// <summary>1ルーム・1献血種別の結果。</summary>
public class TypeResult
{
    public BloodDonationTypeEnum Key { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool Offered { get; set; }

    /// <summary>空き・満枠をまとめて時間順のまま持つ。</summary>
    public List<BloodDonationSlot> Slots { get; set; } = [];

    public int AvailableCount => Offered ? Slots.Count(s => s.Available) : 0;

    public int FullCount => Offered ? Slots.Count - AvailableCount : 0;
}
