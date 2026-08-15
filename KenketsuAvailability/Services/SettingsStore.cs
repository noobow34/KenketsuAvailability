using KenketsuAvailability.Constants;
using KenketsuAvailability.Models;
using System.IO;
using System.Text.Json;

namespace KenketsuAvailability.Services;

/// <summary>
/// 検索条件・選択セット・取得速度の実測値・レートリミットのカウンタを %APPDATA% に保存する。
/// ここに入るのは利用者ごとの情報だけで、作者が配る設定はスプレッドシート側にある。
/// </summary>
public class SettingsStore
{
    /// <summary>取得所要時間の実測値に対する新しい測定値の重み（指数移動平均）。</summary>
    private const double FetchSpeedWeight = 0.3;

    /// <summary>実測値として受け付ける1ルームあたりの秒数の範囲。外れ値は捨てる。</summary>
    private const double MinSecPerRoom = 0.2;
    private const double MaxSecPerRoom = 30;

    /// <summary>選択セット名の上限。プルダウンに収まる長さに抑える。</summary>
    public const int PresetNameMaxLength = 30;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;

    public AppSettings Settings { get; private set; } = new();

    public SettingsStore()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KenketsuAvailability");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                Settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath), JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            //壊れていたら初期状態から始める（検索自体は続けられるようにする）
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(Settings, JsonOptions));
        }
        catch
        {
            //保存に失敗しても検索操作は続けられるようにする
        }
    }

    /// <summary>生年月日・性別が登録済みか。未登録なら検索させない。</summary>
    public bool HasProfile => Settings.BirthDate.HasValue && Settings.Gender.HasValue;

    /// <summary>生年月日・性別を保存する。このPC内にのみ保存し、外部へ送ることはない。</summary>
    public void SaveProfile(DateOnly birthDate, GenderEnum gender)
    {
        Settings.BirthDate = birthDate;
        Settings.Gender = gender;
        Save();
    }

    /// <summary>検索条件を保存する。画面での変更のたびに呼ばれる。</summary>
    public void SaveCondition(BloodDonationSearchModeEnum searchMode,
        IEnumerable<int> centerIds, DateOnly? targetDate,
        int? singleCenterId, IEnumerable<DateOnly> targetDates)
    {
        Settings.SearchMode = searchMode;
        Settings.CenterIds = centerIds.Distinct().ToList();
        Settings.TargetDate = targetDate;
        Settings.SingleCenterId = singleCenterId;
        Settings.TargetDates = targetDates.Distinct().Order().ToList();
        Save();
    }

    /// <summary>
    /// 取得にかかった時間（1ルームあたりの秒数）を記録する。
    /// 1回の遅い取得で目安が跳ねないよう、指数移動平均でならす。
    /// </summary>
    public void SaveFetchSpeed(double secPerRoom)
    {
        //ネットワークの一時的な詰まりや計測ミスで極端な値が入るのを防ぐ
        if (double.IsNaN(secPerRoom) || secPerRoom < MinSecPerRoom || secPerRoom > MaxSecPerRoom)
        {
            return;
        }

        Settings.SecPerRoom = Settings.SecPerRoom is double previous
            ? previous * (1 - FetchSpeedWeight) + secPerRoom * FetchSpeedWeight
            : secPerRoom;
        Save();
    }

    /// <summary>
    /// 献血ルームの選択セットを名前を付けて保存する。同名は上書き。
    /// 失敗時はエラーメッセージを返す（成功なら null と保存先ID）。
    /// </summary>
    public (string? Error, int SavedId) SavePreset(string? name, IEnumerable<int> centerIds)
    {
        string presetName = (name ?? "").Trim();
        if (presetName.Length == 0)
        {
            return ("セット名を入力してください。", 0);
        }
        if (presetName.Length > PresetNameMaxLength)
        {
            return ($"セット名は{PresetNameMaxLength}文字以内で入力してください。", 0);
        }

        var ids = centerIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return ("献血ルームが選択されていません。", 0);
        }

        var preset = Settings.Presets.FirstOrDefault(p => p.Name == presetName);
        if (preset == null)
        {
            //DBの連番の代わり。削除しても既存IDと衝突しないよう最大値＋1にする
            preset = new PresetItem
            {
                Id = Settings.Presets.Count == 0 ? 1 : Settings.Presets.Max(p => p.Id) + 1,
                Name = presetName
            };
            Settings.Presets.Add(preset);
        }
        preset.CenterIds = ids;
        Save();

        return (null, preset.Id);
    }

    /// <summary>献血ルームの選択セットを削除する。</summary>
    public void DeletePreset(int id)
    {
        Settings.Presets.RemoveAll(p => p.Id == id);
        Save();
    }
}
