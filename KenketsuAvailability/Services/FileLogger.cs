using Microsoft.Extensions.Logging;
using System.IO;

namespace KenketsuAvailability.Services;

/// <summary>
/// 警告以上のログを %APPDATA%\KenketsuAvailability\app.log に追記する。
/// デスクトップアプリでは WebView 内の例外が画面に出ないため、あとから原因を追えるようにしておく。
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly Lock _lock = new();

    public FileLoggerProvider(string path) => _path = path;

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    internal void Write(string line)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch
            {
                //ログが書けなくてもアプリは動かす
            }
        }
    }

    public void Dispose() { }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _category;

        public FileLogger(FileLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {_category} - {formatter(state, exception)}";
            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }
            _provider.Write(line);
        }
    }
}
