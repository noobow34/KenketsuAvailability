using System.ComponentModel;

namespace KenketsuAvailability.Constants;

/// <summary>
/// 献血種別。横断検索で扱うものだけを持つ（全血200は対象外）。
/// </summary>
public enum BloodDonationTypeEnum
{
    [Description("全血400")]
    Whole400,

    [Description("血漿")]
    PPP,

    [Description("血小板")]
    PCPPP,

    [Description("成分全て")]
    ComponentAll,

    [Description("全種")]
    All
}

public static class BloodDonationTypeEnumExtensions
{
    /// <summary>Description 属性の文字列（Noobow.Commons の GetDescription 相当）。</summary>
    public static string GetDescription(this BloodDonationTypeEnum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>().FirstOrDefault();
        return attribute?.Description ?? value.ToString();
    }
}
