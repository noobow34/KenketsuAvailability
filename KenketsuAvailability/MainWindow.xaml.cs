using KenketsuAvailability.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace KenketsuAvailability;

public partial class MainWindow : Window
{
    /// <summary>設定ファイルと同じ場所に置く。WebView 内で起きた例外はここでしか追えない。</summary>
    private static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KenketsuAvailability", "app.log");

    public MainWindow()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

        ServiceCollection services = new();
        services.AddWpfBlazorWebView();
        services.AddLogging(builder => builder.AddProvider(new FileLoggerProvider(LogPath)));
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        services.AddSingleton<RemoteConfigService>();
        services.AddSingleton<SettingsStore>();
        services.AddSingleton<RateLimiter>();
        services.AddSingleton<ExternalBrowser>();

        //InitializeComponent より前に入れておかないと DynamicResource が解決できない
        Resources.Add("services", services.BuildServiceProvider());

        InitializeComponent();
    }
}
