using KenketsuAvailability.Models;

namespace KenketsuAvailability.Services;

/// <summary>
/// 献血ルームマスタ。作者のスプレッドシートから読み込んだ一覧をもとに、
/// 選択ピッカー用の 地方 → 都道府県 → ルーム の3階層に組み立てる。
/// </summary>
public class CenterMaster
{
    /// <summary>全ルーム（CenterId 順）。</summary>
    public IReadOnlyList<BloodDonationCenter> All { get; }

    /// <summary>地方 → 都道府県 → ルームの3階層。ピッカーの並び順はここで確定させる。</summary>
    public IReadOnlyList<BloodDonationRegionGroup> RegionGroups { get; }

    private readonly Dictionary<int, BloodDonationCenter> _byId;

    public CenterMaster(IEnumerable<BloodDonationCenter> centers)
    {
        All = centers.OrderBy(c => c.CenterId).ToList();
        _byId = All.ToDictionary(c => c.CenterId);

        //地方 → 都道府県 → ルームの3階層。全国に増えても初期表示が地方の数（＝8行）で収まるようにする
        var prefectureGroups = All
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Prefecture) ? PrefectureRegions.Other : c.Prefecture!)
            .Select(g => new BloodDonationCenterGroup
            {
                Prefecture = g.Key,
                Centers = g.OrderBy(c => c.CenterId).ToList()
            })
            .ToList();

        RegionGroups = prefectureGroups
            .GroupBy(g => PrefectureRegions.GetRegion(g.Prefecture))
            .OrderBy(rg => PrefectureRegions.GetRegionOrder(rg.Key))
            .Select(rg => new BloodDonationRegionGroup
            {
                Region = rg.Key,
                PrefectureGroups = rg.OrderBy(g => PrefectureRegions.GetPrefectureOrder(g.Prefecture)).ToList()
            })
            .ToList();
    }

    public BloodDonationCenter? Find(int centerId)
        => _byId.TryGetValue(centerId, out var center) ? center : null;

    public bool Contains(int centerId) => _byId.ContainsKey(centerId);
}
