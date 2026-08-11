using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using KenketsuAvailability.Constants;

namespace KenketsuAvailability.Services;

/// <summary>
/// 献血ルーム1件・1日分の予約空き状況（種別ごと）。
/// </summary>
public class BloodDonationAvailability
{
    public Dictionary<BloodDonationTypeEnum, BloodDonationTypeAvailability> Types { get; } = [];
}

/// <summary>時間枠1件。Timeは HH:mm。</summary>
public record BloodDonationSlot(string Time, bool Available);

/// <summary>
/// 献血種別1件分の予約空き状況。
/// </summary>
public class BloodDonationTypeAvailability
{
    /// <summary>その日その献血ルームでこの種別を取り扱っているか（種別タブが押せる状態か）。</summary>
    public bool Offered { get; set; }

    /// <summary>全時間枠。先方ページの並び順（＝時間順）をそのまま保持する。</summary>
    public List<BloodDonationSlot> Slots { get; set; } = [];

    /// <summary>予約可能な時間（HH:mm）。</summary>
    public List<string> AvailableTimes => Slots.Where(s => s.Available).Select(s => s.Time).ToList();

    /// <summary>枠は存在するが予約不可の時間（HH:mm）。</summary>
    public List<string> FullTimes => Slots.Where(s => !s.Available).Select(s => s.Time).ToList();
}

/// <summary>
/// kenketsu.jp の予約ページをスクレイピングして空き状況を取得する。
/// 1回のページ取得で 400／血漿／血小板 の3種すべてが同一HTMLに含まれるため、
/// 必要な種別をまとめて渡すことでリクエスト数を抑えられる。
/// </summary>
public sealed class BloodDonationAvailabilityFetcher : IDisposable
{
    private readonly IBrowsingContext _angleContext = AnglesharpContextFactory.Create();

    /// <summary>画面で扱う献血種別（全血200は対象外）。</summary>
    public static readonly BloodDonationTypeEnum[] AllTypes =
    [
        BloodDonationTypeEnum.Whole400,
        BloodDonationTypeEnum.PPP,
        BloodDonationTypeEnum.PCPPP
    ];

    /// <summary>
    /// 指定日・指定献血ルームの予約ページURL。ブラウザで開くリンクにもそのまま使える。
    /// </summary>
    public static string BuildReservePageUrl(DateTime targetDate, string placeId)
        => $"https://www.kenketsu.jp/reservationeditbydate?birthday=1&birthmonth=1&birthyear=2006&day={targetDate.Day}&from=date&month={targetDate.Month}&placeId={placeId}&sex=%E7%94%B7%E6%80%A7&year={targetDate.Year}";

    public Task<BloodDonationAvailability> FetchAsync(DateTime targetDate, string placeId, CancellationToken ct = default)
        => FetchAsync(targetDate, placeId, AllTypes, ct);

    public async Task<BloodDonationAvailability> FetchAsync(DateTime targetDate, string placeId, IEnumerable<BloodDonationTypeEnum> bdTypes, CancellationToken ct = default)
    {
        var doc = await _angleContext.OpenAsync(BuildReservePageUrl(targetDate, placeId), ct);

        BloodDonationAvailability availability = new();
        foreach (var bdType in bdTypes)
        {
            availability.Types[bdType] = Parse(doc, bdType);
        }
        return availability;
    }

    private static BloodDonationTypeAvailability Parse(IDocument doc, BloodDonationTypeEnum bdType)
    {
        int tabIndex = bdType switch
        {
            BloodDonationTypeEnum.Whole400 => 1,
            BloodDonationTypeEnum.PPP => 3,
            BloodDonationTypeEnum.PCPPP => 4,
            _ => 1,
        };

        var bdTab = doc.Body.SelectNodes($"//*[@id=\"section-type\"]/div/div[1]/ol/li[{tabIndex}]/a").SingleOrDefault();
        if (bdTab == null)
        {
            throw new Exception($"献血予約チェック：タブ要素が見つかりません。先方HTMLの構造が変わった可能性があります（tabIndex={tabIndex}）");
        }

        BloodDonationTypeAvailability result = new();
        string bdTabClass = ((IElement)bdTab).GetAttribute("class") ?? "";
        if (bdTabClass.Contains("is-disabled"))
        {
            //タブが押せない＝この日はこの種別の取り扱いなし
            return result;
        }
        result.Offered = true;

        string bdTypeStr = bdType switch
        {
            BloodDonationTypeEnum.Whole400 => "syurui400",
            BloodDonationTypeEnum.PPP => "syuruiPPP",
            BloodDonationTypeEnum.PCPPP => "syuruiPCPPP",
            _ => "",
        };

        var timeLis = doc.Body.SelectNodes($@"//*[@id=""{bdTypeStr}""]/ul/li");
        var timeTiles = doc.Body.SelectNodes($@"//*[@id=""{bdTypeStr}""]/ul/li//a");
        if (timeLis.Any() && !timeTiles.Any())
        {
            //li（時間枠）は存在するのにa（予約ボタン）が1件も取れない＝抽出ロジックが先方HTMLの構造変化に追従できていない
            throw new Exception($"献血予約チェック：予約時間ボタンの抽出に失敗しました。先方HTMLの構造が変わった可能性があります（{bdTypeStr}）");
        }

        foreach (var timeTile in timeTiles!)
        {
            var element = (IElement)timeTile;
            string timeText = element.Text().Trim();
            bool available = !(element.GetAttribute("class") ?? "").Contains("is-disabled");
            result.Slots.Add(new BloodDonationSlot(timeText, available));
        }
        return result;
    }

    public void Dispose()
    {
        (_angleContext as IDisposable)?.Dispose();
    }
}
