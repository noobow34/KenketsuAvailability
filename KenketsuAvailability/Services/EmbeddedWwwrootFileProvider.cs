using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.IO;
using System.Reflection;

namespace KenketsuAvailability.Services;

/// <summary>
/// 画面のファイル（index.html / CSS / JS / blazor.webview.js）をアセンブリの埋め込みリソースから配る。
///
/// PublishSingleFile では静的Webアセットが単一ファイルに入らず exe の隣に散らばってしまうため、
/// ビルド時に埋め込んで（csproj の _EmbedWwwroot ターゲット）ここから読ませる。
/// 見つからないものは fallback（既定のディスク上のプロバイダ）に委ねる。
/// </summary>
public sealed class EmbeddedWwwrootFileProvider : IFileProvider
{
    /// <summary>埋め込み時に付けた論理名の接頭辞。</summary>
    private const string Prefix = "wwwroot/";

    private readonly Assembly _assembly;
    private readonly IFileProvider _fallback;
    private readonly Dictionary<string, string> _resourceBySubpath;

    public EmbeddedWwwrootFileProvider(Assembly assembly, IFileProvider fallback)
    {
        _assembly = assembly;
        _fallback = fallback;
        _resourceBySubpath = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(name => name[Prefix.Length..], StringComparer.OrdinalIgnoreCase);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        string key = (subpath ?? "").Replace('\\', '/').TrimStart('/');
        if (_resourceBySubpath.TryGetValue(key, out string? resourceName))
        {
            return new EmbeddedFile(_assembly, resourceName, key);
        }
        return _fallback.GetFileInfo(subpath!);
    }

    public IDirectoryContents GetDirectoryContents(string subpath) => _fallback.GetDirectoryContents(subpath);

    //埋め込みリソースは実行中に変わらない
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private sealed class EmbeddedFile : IFileInfo
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;

        public EmbeddedFile(Assembly assembly, string resourceName, string subpath)
        {
            _assembly = assembly;
            _resourceName = resourceName;
            Name = subpath[(subpath.LastIndexOf('/') + 1)..];
            using var stream = assembly.GetManifestResourceStream(resourceName);
            Length = stream?.Length ?? 0;
        }

        public bool Exists => true;

        public long Length { get; }

        public string? PhysicalPath => null;

        public string Name { get; }

        /// <summary>実体はアセンブリ内なので、更新日時はアセンブリのビルド時刻に寄せる。</summary>
        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
            => _assembly.GetManifestResourceStream(_resourceName) ?? Stream.Null;
    }
}
