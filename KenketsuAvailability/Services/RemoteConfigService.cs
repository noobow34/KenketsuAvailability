using KenketsuAvailability.Models;
using System.Net.Http;

namespace KenketsuAvailability.Services;

public enum RemoteConfigStatus
{
    /// <summary>まだ読み込んでいない／読み込み中。</summary>
    Loading,

    /// <summary>読み込めた。使用可否は <see cref="RemoteConfig.Enabled"/> を見る。</summary>
    Ready,

    /// <summary>読み込めなかった。この状態では何もできない。</summary>
    Error
}

/// <summary>
/// 作者のGoogleスプレッドシートから設定を読み込む。
///
/// 「公開停止の要請があれば即座に止められる」ことを優先し、**読み込めなければ何もさせない**。
/// 手元にキャッシュを持って動かしてしまうと、使用可能フラグを落としても止まらなくなるため。
/// アプリ自体がネットワーク必須なので、この割り切りで実害は少ない。
/// </summary>
public class RemoteConfigService
{
    /// <summary>
    /// 設定を置いたスプレッドシートのID。
    /// スプレッドシートは「リンクを知っている全員が閲覧可」にしておくこと。
    /// </summary>
    public const string SpreadsheetId = "1OVbTdQ6l6r59PPYe5gJkagVttdWxJ-1DvybNFumMF2k";

    /// <summary>
    /// シートは名前ではなく gid で指定する。
    /// 名前で引ける gviz エンドポイントは列の型を推測してしまい、
    /// config シートのように1列に文字列と数値が混ざると値が欠落するため。
    /// gid はシートを開いたときのURL（#gid=…）で確認できる。
    /// </summary>
    private const string ConfigGid = "539630018";

    private const string CentersGid = "2113707435";

    private static readonly HttpClient Http = CreateHttpClient();

    public RemoteConfigStatus Status { get; private set; } = RemoteConfigStatus.Loading;

    /// <summary>読み込みに失敗した理由。画面に出す。</summary>
    public string ErrorMessage { get; private set; } = "";

    public RemoteConfig Config { get; private set; } = new();

    /// <summary>読み込んだ献血ルームから組み立てた選択ツリー。</summary>
    public CenterMaster Centers { get; private set; } = new([]);

    /// <summary>スプレッドシートIDが未設定のままビルドされていないか。</summary>
    public static bool IsSpreadsheetConfigured => !SpreadsheetId.StartsWith("PUT-YOUR-", StringComparison.Ordinal);

    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new() { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.Add("User-Agent", $"KenketsuAvailability / {BotContactInfo.Suffix}");
        return client;
    }

    /// <summary>シート1枚を、セルに入っている値そのままのCSVで取得するURL。</summary>
    private static string CsvUrl(string gid)
        => $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={gid}";

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Status = RemoteConfigStatus.Loading;
        ErrorMessage = "";

        if (!IsSpreadsheetConfigured)
        {
            Fail("スプレッドシートIDが設定されていません。アプリのビルド設定の問題です。");
            return;
        }

        try
        {
            string configCsv = await GetCsvAsync(ConfigGid, "config", ct);
            string centersCsv = await GetCsvAsync(CentersGid, "centers", ct);

            RemoteConfig config = ParseConfig(configCsv);
            config.Centers = ParseCenters(centersCsv);

            Config = config;
            Centers = new CenterMaster(config.Centers);
            Status = RemoteConfigStatus.Ready;
        }
        catch (OperationCanceledException)
        {
            //画面を閉じた等。状態はそのまま
        }
        catch (HttpRequestException ex)
        {
            Fail($"設定の取得に失敗しました。ネットワーク接続を確認してください。（{ex.Message}）");
        }
        catch (Exception ex)
        {
            Fail($"設定の読み込みに失敗しました。（{ex.Message}）");
        }
    }

    private void Fail(string message)
    {
        ErrorMessage = message;
        Status = RemoteConfigStatus.Error;
    }

    private static async Task<string> GetCsvAsync(string gid, string sheetName, CancellationToken ct)
    {
        var response = await Http.GetAsync(CsvUrl(gid), ct);
        if (!response.IsSuccessStatusCode)
        {
            //シートを作り直すと gid が変わる
            throw new Exception($"シート「{sheetName}」を読み取れませんでした（HTTP {(int)response.StatusCode}）。");
        }
        string body = await response.Content.ReadAsStringAsync(ct);

        //共有設定が公開になっていないとログインページのHTMLが返る
        if (body.TrimStart().StartsWith('<'))
        {
            throw new Exception($"シート「{sheetName}」を読み取れませんでした。スプレッドシートの共有設定を確認してください。");
        }
        return body;
    }

    private static RemoteConfig ParseConfig(string csv)
    {
        //key / value の2列。行の順番には依存しない
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Csv.ParseWithHeader(csv))
        {
            if (row.TryGetValue("key", out string? key) && key.Length > 0)
            {
                values[key] = row.TryGetValue("value", out string? value) ? value : "";
            }
        }

        if (values.Count == 0)
        {
            throw new Exception("設定シートに読み取れる行がありませんでした。");
        }

        return new RemoteConfig
        {
            //キー自体が無い場合は「止まっている」と解釈する（安全側に倒す）
            Enabled = ParseBool(Get(values, "enabled"), false),
            Message = Get(values, "message"),
            MessageFormat = Get(values, "messageFormat").Equals("html", StringComparison.OrdinalIgnoreCase)
                ? MessageFormat.Html
                : MessageFormat.Text,
            RoomIntervalMs = ParseInt(Get(values, "roomIntervalMs"),
                RemoteConfig.DefaultRoomIntervalMs, RemoteConfig.MinRoomIntervalMs, RemoteConfig.MaxRoomIntervalMs),
            MaxRoomsPerSearch = ParseInt(Get(values, "maxRoomsPerSearch"),
                RemoteConfig.DefaultMaxRoomsPerSearch, RemoteConfig.MinMaxRoomsPerSearch, RemoteConfig.MaxMaxRoomsPerSearch),
            MaxDatesPerSearch = ParseInt(Get(values, "maxDatesPerSearch"),
                RemoteConfig.DefaultMaxDatesPerSearch, RemoteConfig.MinMaxDatesPerSearch, RemoteConfig.MaxMaxDatesPerSearch),
            MinAppVersion = Get(values, "minAppVersion"),
            BlockedVersions = Get(values, "blockedVersions")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            CooldownSeconds = ParseInt(Get(values, "cooldownSeconds"),
                RemoteConfig.DefaultCooldownSeconds, RemoteConfig.MinCooldownSeconds, RemoteConfig.MaxCooldownSeconds),
            HourlyRequestLimit = ParseInt(Get(values, "hourlyRequestLimit"),
                RemoteConfig.DefaultHourlyRequestLimit, RemoteConfig.MinHourlyRequestLimit, RemoteConfig.MaxHourlyRequestLimit),
        };
    }

    private static List<BloodDonationCenter> ParseCenters(string csv)
    {
        List<BloodDonationCenter> centers = [];
        foreach (var row in Csv.ParseWithHeader(csv))
        {
            string placeId = Get(row, "placeId");
            //placeId が無い行は検索できないので落とす
            if (placeId.Length == 0) continue;

            centers.Add(new BloodDonationCenter
            {
                CenterId = ParseInt(Get(row, "centerId"), 0, int.MinValue, int.MaxValue),
                CenterName = Get(row, "centerName"),
                PlaceId = placeId,
                Prefecture = Get(row, "prefecture"),
                //列が無い場合・空の場合は「取り扱いあり」とみなす
                OfferWhole400 = ParseBool(Get(row, "offerWhole400"), true),
                OfferPpp = ParseBool(Get(row, "offerPPP"), true),
                OfferPcppp = ParseBool(Get(row, "offerPCPPP"), true)
            });
        }

        if (centers.Count == 0)
        {
            throw new Exception("献血ルームのシートに有効な行がありませんでした。");
        }

        //CenterId が未設定・重複している場合は並び順が壊れるので、行の順番で振り直す
        if (centers.Select(c => c.CenterId).Distinct().Count() != centers.Count
            || centers.Any(c => c.CenterId <= 0))
        {
            for (int i = 0; i < centers.Count; i++)
            {
                centers[i].CenterId = i + 1;
            }
        }
        return centers;
    }

    private static string Get(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out string? value) ? value.Trim() : "";

    private static bool ParseBool(string text, bool fallback)
        => text.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "y" or "on" or "有効" => true,
            "false" or "0" or "no" or "n" or "off" or "無効" => false,
            _ => fallback,
        };

    /// <summary>
    /// 数値として読む。スプレッドシートの表示形式によって「1,000」「300.0」のような
    /// 書式付きの文字列で届くことがあるので、そこまでは受け付ける。
    /// </summary>
    private static int ParseInt(string text, int fallback, int min, int max)
    {
        string cleaned = text.Replace(",", "").Trim();
        if (!double.TryParse(cleaned, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
        {
            return fallback;
        }
        return (int)Math.Clamp(Math.Round(value), min, max);
    }
}
