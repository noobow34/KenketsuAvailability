using System.Diagnostics;

namespace KenketsuAvailability.Services;

/// <summary>
/// 予約ページを既定のブラウザで開く。実際の予約操作はアプリ内ではなく通常のブラウザで行わせる。
/// </summary>
public class ExternalBrowser
{
    public void Open(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            //既定のブラウザが開けなくても、アプリ側の操作は続けられるようにする
        }
    }
}
