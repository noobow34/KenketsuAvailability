using KenketsuAvailability.Services;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace KenketsuAvailability;

/// <summary>
/// 画面のファイルを埋め込みリソースから配る BlazorWebView。
/// 単一 exe で配布するために必要（詳細は <see cref="EmbeddedWwwrootFileProvider"/>）。
/// </summary>
public class EmbeddedBlazorWebView : BlazorWebView
{
    public override IFileProvider CreateFileProvider(string contentRootDir)
        => new EmbeddedWwwrootFileProvider(Assembly.GetExecutingAssembly(), base.CreateFileProvider(contentRootDir));
}
