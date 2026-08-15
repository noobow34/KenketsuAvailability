using System.ComponentModel;

namespace KenketsuAvailability.Constants;

/// <summary>
/// 性別。先方の空き状況ページが引数に取るため保持する。
/// 選択肢は先方サイトに合わせて2値のみ。
/// </summary>
public enum GenderEnum
{
    [Description("男性")]
    Male,

    [Description("女性")]
    Female
}
