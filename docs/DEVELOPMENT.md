# 開発者向けドキュメント

利用者向けの説明は [README.md](../README.md) を参照。

---

## 構成

| 項目 | 内容 |
|------|------|
| 言語・フレームワーク | C# / .NET 10 / WPF + BlazorWebView（Blazor Hybrid） |
| スクレイピング | AngleSharp + AngleSharp.XPath |
| 画面 | Blazor コンポーネント（`Components/`）＋ 素のCSS（`wwwroot/css/app.css`） |
| 作者側の設定 | Googleスプレッドシート（起動時にCSVで取得） |
| 利用者ごとの設定 | `%APPDATA%\KenketsuAvailability\settings.json` |
| ログ | `%APPDATA%\KenketsuAvailability\app.log`（警告以上のみ） |

WPF ではなく Blazor Hybrid を選んでいるのは、カードグリッド・ツリーピッカー・モーダルといった
UIをHTML/CSSでそのまま書けるため。実行には WebView2 ランタイムが必要（Windows 11 には標準で入っている）。

### 主なファイル

| ファイル | 役割 |
|----------|------|
| `Services/RemoteConfigService.cs` | スプレッドシートから設定・ルーム一覧を取得 |
| `Services/RemoteConfig.cs` | 取得した設定のモデルと、既定値・許容範囲 |
| `Services/Csv.cs` | CSVパーサ（RFC 4180 相当） |
| `Services/BloodDonationAvailabilityFetcher.cs` | 予約ページのスクレイピング |
| `Services/RateLimiter.cs` | アクセス制限の判定とカウンタ |
| `Services/SettingsStore.cs` | 利用者ごとの設定の読み書き |
| `Services/CenterMaster.cs` | ルーム一覧を 地方→都道府県→ルーム の3階層に組み立てる |
| `Services/EmbeddedWwwrootFileProvider.cs` | 画面のファイルを埋め込みリソースから配る（単一exe用） |
| `Components/MainPage.razor` | 画面本体。読み込み中・停止中・エラー・通常の4状態を持つ |
| `Components/CenterPicker.razor` | 献血ルームの選択ピッカー（ルーム横断） |
| `Components/DatePicker.razor` | 対象日の選択カレンダー（日付横断） |
| `Components/AuthorMessage.razor` | 作者からのお知らせ（HTML / プレーンテキスト） |

---

## 設定スプレッドシート

作者が管理するGoogleスプレッドシートから、起動時に設定と献血ルーム一覧を読み込む。
アプリを配り直さずに内容を差し替えられ、使用可能フラグを落とせば配布済みのアプリも止められる。

### 準備

1. スプレッドシートを作り、シート（タブ）を **`config`** と **`centers`** の2つ用意する
2. 共有設定を「**リンクを知っている全員が閲覧可**」にする
3. `Services/RemoteConfigService.cs` の3つの定数を設定してビルドする

```csharp
public const string SpreadsheetId = "1AbC...";   // URL の /d/ と /edit の間
private const string ConfigGid = "539630018";    // config タブを開いたときの #gid=
private const string CentersGid = "2113707435";  // centers タブを開いたときの #gid=
```

取得には `https://docs.google.com/spreadsheets/d/{ID}/export?format=csv&gid={gid}` を使う。
APIキーや認証は不要。

> [!IMPORTANT]
> **シート名で引ける `gviz/tq?tqx=out:csv&sheet={名前}` は使ってはいけない。**
> gviz は列ごとに型を推測し、推測した型に合わないセルを**空文字にして返す**。
> `config` シートの `value` 列のように1列へ文字列と数値が混ざっていると、
> 数値列と判定されて `enabled` の `TRUE` や `messageFormat` の `text` が消える。
> `export?format=csv` はセルの表示値をそのまま返すのでこの問題が起きない。
>
> 代償として、シートを**作り直すと gid が変わる**（タブ名の変更では変わらない）。
> gid が変わった場合はアプリが起動時にエラー画面を出すので、気付かず動き続けることはない。

### `config` シート

1行目はヘッダで、`key` と `value` の2列。行の順番は問わない。

| key | 説明 | 例 |
|-----|------|-----|
| `enabled` | **使用可能フラグ。** `FALSE` にすると全利用者のアプリが使用不可になる | `TRUE` |
| `message` | 作者からのお知らせ。空なら非表示 | `メンテナンス中です` |
| `messageFormat` | `html` または `text`（既定は `text`） | `html` |
| `roomIntervalMs` | ルーム間のウェイト（ミリ秒） | `300` |
| `maxRoomsPerSearch` | 1回の検索で取得できるルーム数（ルーム横断） | `50` |
| `maxDatesPerSearch` | 1回の検索で取得できる日数（日付横断）。既定 `7` | `7` |
| `cooldownSeconds` | 検索と検索の間隔（秒） | `30` |
| `hourlyRequestLimit` | 1時間あたりのリクエスト数 | `200` |
| `minAppVersion` | **これ未満のバージョンは使用不可**にする。空なら下限なし | `1.2.0` |
| `blockedVersions` | 個別に使用不可にするバージョン（カンマ区切り） | `1.1.0, 1.1.1` |

- `enabled` のキー自体が無い場合は「停止」として扱う（安全側に倒す）。
- バージョン制限に該当したアプリは、検索画面を出さずダウンロードページへ誘導する。
  先方HTMLの変更に追従できていない古い版を止める用途を想定している。
  `minAppVersion` / `blockedVersions` が空や不正な値のときは制限なしとして通す
  （設定ミスで全利用者を締め出さないため）。判定に使う自分のバージョンは
  `Services/AppInfo.cs` がアセンブリから読む。
- 制限値は `RemoteConfig` の `Min*` / `Max*` の範囲に丸める。入力ミスで先方サイトへ極端な負荷が
  かかる設定になるのを防ぐため。値が空・不正なら既定値を使う。
- `messageFormat` に `html` を指定した場合、**作者が書いたHTMLをそのまま描画する**。
  サニタイズはしていない。スプレッドシートを作者しか編集できない前提の設計であり、
  この前提が崩れる場合はリリースを差し替えられる状況と同等なので、信頼境界は変わらないものとしている。

### `centers` シート

1行目はヘッダ。

| 列 | 説明 |
|----|------|
| `centerId` | 並び順の基準になる整数。重複や空があると行の順番で振り直す |
| `centerName` | 画面に出すルーム名 |
| `placeId` | kenketsu.jp の `placeId`。**空の行は無視される** |
| `prefecture` | 例：`東京都`。地方へのグループ分けに使う（`Services/PrefectureRegions.cs`） |
| `offerWhole400` | 全血400の取り扱いがあるか。`FALSE` で「取扱なし」表示 |
| `offerPPP` | 血漿の取り扱いがあるか |
| `offerPCPPP` | 血小板の取り扱いがあるか |

取り扱いフラグが必要な理由：先方ページは「取扱なし」も「その日は満枠」も同じくタブ非活性になり、
HTMLからは区別できない。恒常的な取り扱いの有無はマスタ側で持ち、
画面の表示を「取扱なし」と「空きなし」で出し分けている。
全血のみのルームなど種別ごとに事情が違うため、3種別それぞれのフラグにしている。

- 列が無い場合・空の場合は「取り扱いあり」として扱う。

### 読み込みに失敗したときの挙動

**キャッシュを持たず、読み込めなければ何もさせない。** 手元のキャッシュで動いてしまうと
使用可能フラグを落としても止まらなくなるため。アプリ自体がネットワーク必須なので実害は少ない。

---

## レートリミットの実装

- カウンタ（直近1時間のリクエスト時刻・最終検索時刻）は `settings.json` に永続化する。
  再起動でリセットできては制限にならないため。
- 記録は先方サイトへリクエストを投げる直前に行うので、取得に失敗した分もカウントされる。
- 検索を中断した場合もクールダウンは開始する（リクエスト自体は投げているため）。
- アクセスには User-Agent `AngleSharp / Noobow Bot (contact: https://noobow-bot-contact.pages.dev/)`
  を付けており、先方から見て識別・遮断できるようにしている。

---

## ビルド・実行

```bash
dotnet build KenketsuAvailability.slnx
```

```bash
dotnet run --project KenketsuAvailability/KenketsuAvailability.csproj
```

配布用の単一 exe（自己完結・約66MB）：

```bash
dotnet publish KenketsuAvailability/KenketsuAvailability.csproj -c Release -o publish
```

`publish/KenketsuAvailability.exe` だけで動く。同じフォルダに出る `wwwroot/` は
埋め込み済みのものと同じ内容なので配布には不要。

## リリース手順

1. [`CHANGELOG.md`](../CHANGELOG.md) の先頭に `## vX.Y.Z` の節を足して変更点を書く
2. `KenketsuAvailability.csproj` の `<Version>` を上げる
3. [`README.md`](../README.md) の「最新版をダウンロード」のリンクを新しいバージョンに書き換える
4. コミットして push したあと、タグを打つ

```bash
git tag v1.2.0 && git push origin v1.2.0
```

タグを push すると GitHub Actions（[`.github/workflows/release.yml`](../.github/workflows/release.yml)）が
単一 exe をビルドし、Release を作って添付する。Release の本文は
[`.github/release-notes.md`](../.github/release-notes.md) をテンプレートに、
`CHANGELOG.md` から該当バージョンの節を差し込んで組み立てる。

テンプレート内で使える差し込み記号は次のとおり。
**これらの文字列は本文中どこでも置換されるので、説明のつもりで書くと巻き込まれる**（この表がここにある理由）。

| 記号 | 差し込まれるもの |
|------|------------------|
| `{{VERSION}}` | `1.1.0` のようなバージョン |
| `{{ASSET}}` | 配布する exe のファイル名 |
| `{{URL}}` | その exe への直リンク |
| `{{CHANGELOG}}` | `CHANGELOG.md` から抜き出した該当バージョンの節 |
| `{{COMPARE}}` | 前のタグとの差分ページへのリンク（前のタグが無ければ空） |

- CHANGELOG に該当バージョンの節が無いとワークフローが警告を出し、本文は「変更点の記載がありません」になる。
  ビルド自体は止めない。
- README のリンクは exe の直リンクなので、**リリースのたびに手で更新する**。
  資産名にバージョンを含めているため `releases/latest/download/...` の固定URLが使えないため。
  更新を自動化したい場合は、資産名からバージョンを外すのが手っ取り早い。

献血ルーム一覧はスプレッドシート側で管理しているため、ルームの追加・変更でリリースし直す必要はない。

---

## 実装メモ（ハマりどころ）

- **TFM は `net10.0-windows10.0.19041.0`**。素の `net10.0-windows` だと WebView2 の WPF 版アセンブリが
  参照する `Microsoft.Windows.SDK.NET` が出力されず、起動時に `FileNotFoundException` になる。
- **`index.html` の `<script src="_framework/blazor.webview.js">` に `autostart="false"` を付けない**。
  付けると誰も `Blazor.start()` を呼ばず、画面が空のままになる。
- **静的Webアセットは単一ファイルバンドルに入らない**。`PublishSingleFile` だけでは wwwroot が
  exe の隣に散らばって動かないため、csproj の `_EmbedWwwroot` ターゲットで `@(StaticWebAsset)` を
  埋め込みリソースにし、`EmbeddedWwwrootFileProvider` から配っている。
  この際 `EmbeddedResource` に `WithCulture` / `Type` メタデータを自分で付けないと、
  `AssignTargetPaths` を通らずリソースとして取り込まれない。
- チェックボックスの「一部選択」はDOMプロパティのため Blazor のレンダリングでは表現できない。
  `data-indeterminate` 属性を描画し、`wwwroot/js/app.js` の `syncIndeterminate` で毎回反映している。
- スプレッドシートの取得に **gviz は使わない**（列の型推測で値が欠落する）。
  詳細は[設定スプレッドシート](#設定スプレッドシート)の注意書きを参照。
