namespace KenketsuAvailability.Services;

/// <summary>
/// 都道府県と地方ブロックの対応。全国のルームを一覧するとき、47都道府県をそのまま並べると
/// 長すぎるため、地方でひとまとめにして折りたためるようにする。
/// 対応は不変なのでマスタに列を持たせず、ここで定数として持つ。
/// </summary>
public static class PrefectureRegions
{
    /// <summary>都道府県が未設定・未知のルームをまとめる先。</summary>
    public const string Other = "その他";

    /// <summary>地方 → 都道府県（都道府県コード順）。この宣言順がそのまま画面の並び順になる。</summary>
    private static readonly (string Region, string[] Prefectures)[] Table =
    [
        ("北海道・東北", ["北海道", "青森県", "岩手県", "宮城県", "秋田県", "山形県", "福島県"]),
        ("関東",        ["茨城県", "栃木県", "群馬県", "埼玉県", "千葉県", "東京都", "神奈川県"]),
        ("中部",        ["新潟県", "富山県", "石川県", "福井県", "山梨県", "長野県", "岐阜県", "静岡県", "愛知県"]),
        ("近畿",        ["三重県", "滋賀県", "京都府", "大阪府", "兵庫県", "奈良県", "和歌山県"]),
        ("中国",        ["鳥取県", "島根県", "岡山県", "広島県", "山口県"]),
        ("四国",        ["徳島県", "香川県", "愛媛県", "高知県"]),
        ("九州・沖縄",  ["福岡県", "佐賀県", "長崎県", "熊本県", "大分県", "宮崎県", "鹿児島県", "沖縄県"]),
    ];

    /// <summary>
    /// マスタの都道府県が「東京」「神奈川」のように接尾辞なしで入っていても引けるよう、
    /// 都/府/県を落としたキーも登録する（北海道は落とさない）。
    /// </summary>
    private static IEnumerable<string> Keys(string prefecture)
    {
        yield return prefecture;
        if (prefecture != "北海道")
        {
            yield return prefecture[..^1];
        }
    }

    private static readonly Dictionary<string, string> RegionByPrefecture =
        Table.SelectMany(t => t.Prefectures.SelectMany(p => Keys(p).Select(key => (Key: key, t.Region))))
             .ToDictionary(x => x.Key, x => x.Region);

    private static readonly Dictionary<string, int> OrderByPrefecture =
        Table.SelectMany(t => t.Prefectures)
             .SelectMany((p, i) => Keys(p).Select(key => (Key: key, Index: i)))
             .ToDictionary(x => x.Key, x => x.Index);

    private static readonly Dictionary<string, int> OrderByRegion =
        Table.Select((t, i) => (t.Region, Index: i))
             .ToDictionary(x => x.Region, x => x.Index);

    /// <summary>所属する地方。未知の都道府県は「その他」。</summary>
    public static string GetRegion(string? prefecture)
        => prefecture != null && RegionByPrefecture.TryGetValue(prefecture, out string? region) ? region : Other;

    /// <summary>都道府県の並び順（都道府県コード順）。未知は末尾。</summary>
    public static int GetPrefectureOrder(string? prefecture)
        => prefecture != null && OrderByPrefecture.TryGetValue(prefecture, out int order) ? order : int.MaxValue;

    /// <summary>地方の並び順（北から南）。「その他」は末尾。</summary>
    public static int GetRegionOrder(string? region)
        => region != null && OrderByRegion.TryGetValue(region, out int order) ? order : int.MaxValue;
}
