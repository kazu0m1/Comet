# Comet v1.0 技術選定・アーキテクチャ設計書 v0.1

> Architecture Baseline — 2026-09-18
> Input: `Comet v1.0 要求仕様書 v0.1`

## 1. 結論

Comet v1.0 は **.NET 10 LTS / C# 14 / WPF** で実装する。画像の基本描画・デコードは WPF が利用する Windows Imaging Component (WIC) 経路を第一選択とし、ZIP/CBZ は .NET 標準の `System.IO.Compression.ZipArchive` を使用する。

最優先するのは「技術的新しさ」ではなく、要求仕様の中心である **高速起動・高速初回表示・軽快なページ送り・正確なFit/Zoom・Windowsでの安定動作**である。描画バックエンドは差し替え可能にし、実測でWPFがボトルネックだと確認された場合のみ Direct2D 等へ局所置換する。

### 採用技術

| 領域 | 採用 | 理由 |
|---|---|---|
| Runtime | .NET 10 LTS | 2028-11-14までサポート。C# 14。Windows Desktop/WPFが現行サポート対象。 |
| Language | C# 14 | Windows APIとの親和性、保守性、非同期処理、十分な性能。 |
| UI | WPF | 成熟したWindowsデスクトップ基盤。WinUI 3より配布依存が単純。 |
| Image rendering | WPF retained rendering | 通常環境ではハードウェアアクセラレーションを利用。Fit/ZoomはTransform中心。 |
| Image decode | WPF/WIC (`BitmapImage`/`BitmapDecoder`) | JPEG/PNGのdecode-to-sizeを利用可能。余計なフルサイズデコードを減らせる。 |
| ZIP/CBZ | `System.IO.Compression.ZipArchive` | 標準ライブラリで直接閲覧可能。外部展開プロセス不要。 |
| Settings | JSON | 小さな設定を高速・透明に保存。 |
| Reading state | 書籍単位JSON | DBを起動時依存にせず、必要な書籍だけ遅延読込。 |
| Installer | Inno Setup | self-contained成果物を伝統的Windows installerとして配布しやすい。 |
| Release | self-contained / win-x64 / multi-file / ReadyToRun | ランタイム未導入PCでも動作。単一EXEの展開コストを避け、起動性能を優先。 |

## 2. 技術比較と判断

### 2.1 UIフレームワーク

| 候補 | 起動/配布 | 描画 | 実装/保守 | 判断 |
|---|---|---|---|---|
| **WPF** | 依存が比較的単純。self-contained可。 | ハードウェアアクセラレーションあり。漫画表示には十分な余力。 | 成熟。情報量が多い。 | **採用** |
| WinUI 3 | Windows App SDK runtimeまたは自己完結SDK同梱が必要。unpackagedでも追加のdeployment要件あり。 | 現代的。 | UIは新しいが今回不要な複雑性が増える。 | 不採用 |
| C++ + Direct2D | 小さく高速にできる。 | 最大性能。 | 実装量・メモリ安全性・開発コストが大きい。 | 性能問題発生時の候補 |
| WPF + SkiaSharp | 描画自由度が高い。 | 高性能。 | native dependencyと画像コピー経路が増える。 | v1.0初期は不採用 |

WPFはレンダリングTier 1/2では多くのグラフィックス機能がハードウェアアクセラレーションされる。Cometの主処理は静止ビットマップの配置・拡縮であり、複雑な3Dや大量エフェクトを必要としない。このため、まずWPFの標準経路で十分かを実測するのが合理的である。

### 2.2 .NET 10を採用する理由

2026-09-18時点で .NET 10 はLTSで、2028-11-14までサポートされる。最新パッチは10.0.12、SDKは10.0.401系が提供され、C# 14を含む。開発は10.0系の最新セキュリティパッチへ追随する。

.NET 11 RCは存在するが、Comet v1.0の基盤にプレリリースを採用する理由はない。

### 2.3 なぜ「単一EXE」にしないか

Releaseは**self-contained multi-file**を基本とする。単一EXEの見栄えより、起動時のファイル展開や依存抽出を避けることを優先する。配布時にはInno Setupで複数ファイルを1つのSetup.exeに包むため、利用者側の配布体験は単純なまま維持できる。

さらに `PublishReadyToRun=true` を性能比較対象にする。ReadyToRunはJIT量を減らして起動時間を改善し得る一方、ファイルサイズを増やすため、最終採用はベンチマークで確認する。初期Release profileでは有効にする。

## 3. アーキテクチャ原則

1. **UIスレッドをI/Oとdecodeから隔離する。**
2. **最初の1ページを最優先し、全件処理を待たない。**
3. **ページ送りは先読みで隠蔽する。**
4. **Fit/Zoom操作中は既存BitmapをTransformし、操作のたびに再decodeしない。**
5. **画像Viewportの実寸を唯一のFit基準とする。**
6. **MComix互換の操作意味と、Comet内部実装を分離する。**
7. **測定で必要になるまで低レベル最適化を導入しない。**
8. **外部ランタイム依存・NuGet依存を最小化する。**

## 4. 論理構成

```text
┌────────────────────────────────────────────┐
│                 Comet.App                  │
│ Window / Menu / Toolbar / Input / ViewModel│
│                PageViewport                │
└───────────────────┬────────────────────────┘
                    │ commands / state
┌───────────────────▼────────────────────────┐
│                 Comet.Core                 │
│ Fit / Zoom / spread / navigation / models │
│ interfaces only; no WPF / no ZIP concrete │
└───────┬───────────────────────────┬────────┘
        │                           │
┌───────▼────────────────┐  ┌──────▼─────────────────────┐
│ Comet.Infrastructure   │  │ Comet.Platform.Windows    │
│ ZIP/CBZ / Folder       │  │ WPF/WIC decode           │
│ Settings / Book state  │  │ DPI / file association   │
│ Natural sort           │  │ future Direct2D adapter  │
└────────────────────────┘  └────────────────────────────┘
```

### 4.1 Project責務

- `Comet.Core`: 表示モード、Fit計算、綴じ方向、ページモデル、セッション状態、抽象I/F。
- `Comet.Infrastructure`: ZIP/CBZ、フォルダ、永続化。WPF型を参照しない。
- `Comet.Platform.Windows`: WPF/WIC decode、Windows DPI・関連付けなどWindows固有処理。
- `Comet.App`: WPF UIと入力。Coreの状態を表示に反映する。

この境界により、将来Direct2D描画へ差し替える場合でもZIP・読書位置・ナビゲーションを変更しない。

## 5. 起動シーケンス

```text
Comet.exe "book.zip"
   │
   ├─ 1. 引数だけ解析
   ├─ 2. 小さいsettings.jsonを読込
   ├─ 3. MainWindowを即表示
   │      └─ 漫画モードON + 保存済みFitを反映
   └─ 4. 非同期でbook.zipをOpen
          ├─ central directory列挙
          ├─ 画像entryだけ抽出
          ├─ 自然順sort
          ├─ 読書位置を遅延読込
          ├─ 現在ページを最優先read/decode
          └─ 表示後に隣接ページをprefetch
```

禁止事項は「全ページを展開」「全サムネイルを作成」「全画像をdecode」してからMainWindowを出す設計である。

## 6. ZIP/CBZ読み込み

### 6.1 基本方式

`ZipArchive`でcentral directoryを読み、画像entryだけを論理ページとして保持する。ディスクへの一時展開は行わない。

entry streamはseek不能になり得るため、対象ページだけをメモリへ読み、decode workerへ渡す。`ZipArchive`自身の並列安全性に依存しないよう、**archive entryの読出しは1本のgateで直列化**し、画像decodeはその後で並列化する。

### 6.2 日本語ファイル名

ZIPのUTF-8フラグを尊重する。.NET 10の`ZipArchive`は`entryNameEncoding`を指定できるため、古い日本語ZIPで復号失敗（U+FFFD等）が検出された場合はCP932で再オープンするfallbackを用意する。

### 6.3 安全性

Cometはentryをファイルシステムへ展開しないためZip Slipの主要リスクを避けられる。一方、Zip Bomb対策として以下を置く。

- 単一画像entryの非圧縮サイズ上限: 初期値512 MiB。
- 異常なページ数・巨大画像はログし、必要に応じ確認UIを追加可能にする。
- 1ページ破損時は書籍全体を落とさずエラープレースホルダを表示する。

## 7. 画像decodeと表示

### 7.1 2段階処理

```text
Compressed bytes
      │
      ├─ metadata probe → width / height / format
      │
      └─ decode request(target pixel width)
                │
                ▼
        Frozen BitmapSource
                │
                ▼
          PageViewport
```

WPFの`BitmapImage.DecodePixelWidth`を用いると、JPEG/PNGでは指定サイズへネイティブdecodeできる。4K画像を小さなウィンドウで読む際に、毎回原寸RGBAまで膨らませることを避ける。

### 7.2 Resize/Zoom中に再decodeしない

ウィンドウリサイズやCtrl+Wheelのたびに画像をdecodeし直すとMComixで感じた「もっさり」に近づく。したがってCometは:

1. 手元のBitmapSourceを即座にWPF Transformで拡縮。
2. 操作停止後のdebounce（目安120–180ms）で、現在解像度が不足している場合だけ高解像度decodeを裏で要求。
3. 完了後にBitmapSourceを差し替える。

これにより操作応答と静止時画質を分離する。

### 7.3 Bitmap scaling quality

通常表示はHighQuality/Fant相当を使用する。連続リサイズ中だけLowQualityへ一時的に下げ、停止後にHighQualityへ戻す方式を許容する。これは計測で効果がある場合のみ有効化する。

## 8. Fit / Zoom仕様の実装

### 8.1 Best Fit

単ページ:

```text
scale = min(ViewportWidth / ImageWidth,
            ViewportHeight / ImageHeight)
```

Comet標準では`StretchSmallImages=true`なので、`scale > 1`も許可する。

見開き:

```text
SpreadWidth  = Page1Width + gap + Page2Width
SpreadHeight = max(Page1Height, Page2Height)
scale = min(ViewportWidth / SpreadWidth,
            ViewportHeight / SpreadHeight)
```

Fit Width / Fit Heightは同じSpread geometryを基準に幅または高さだけでscaleを求める。

### 8.2 一時ズーム

`TemporaryZoomFactor`をBase Fit scaleへ乗算する。

```text
EffectiveScale = BaseFitScale × TemporaryZoomFactor
```

- Ctrl+Wheelで変更。
- 前/次ZIPへ移動しても保持。
- プロセス終了時に破棄。
- 次回起動時は永続化されたBase Fit modeから開始。

この構造がユーザー指定のワークフローをそのまま表現する。

## 9. 漫画モードと見開き

標準値は `ReadingDirection.RightToLeft`。

- 表紙（ページ0）は単独表示。
- 以降は見開き時に2ページを組む。
- 右綴じでは若いページ番号を右側、次ページを左側へ配置する。
- Smart Scroll・前後移動の方向意味もMComix互換にする。
- 横長ページを単独扱いするMComix互換ルールは、画像metadata取得後のSpread plannerに閉じ込める。

## 10. Prefetch / Cache

### 10.1 優先順位

```text
P0: 現在表示ページ
P1: 次ページ / 前ページ
P2: ±2ページ
P3: サムネイル
```

ページ送り直後は新しい進行方向側を優先する。ユーザーが逆方向へ戻った場合はpriorityを反転する。

### 10.2 Worker構成

- Archive read worker: 1（ZipArchiveの競合を避ける）
- Decode worker: 初期値2
- Thumbnail worker: 1 / lowest priority
- UI Dispatcher: BitmapSourceの差し替えだけ

古いリクエストにはgeneration IDまたはCancellationTokenを付け、ユーザーが高速にページを進めた場合、不要なdecode結果をUIへ適用しない。

### 10.3 Cache budget

Decoded bitmap cacheは**byte weight付きLRU**とする。

初期budget:

```text
min(512 MiB,
    max(128 MiB, PhysicalMemory / 16))
```

サムネイルは別budget（初期64 MiB）。現在ページと直近ページはevictionしにくくする。

## 11. 状態保存

### settings.json

`%LOCALAPPDATA%\Comet\settings.json`

保存対象: Base Fit、漫画モード、見開き、サムネイル表示など。全画面・TemporaryZoomは保存しない。

### 書籍状態

`%LOCALAPPDATA%\Comet\state\<SHA256(normalized-path)>.json`

- LastPageIndex
- Bookmarks
- source size / last modified fingerprint

SQLiteを採用しない理由は、v1.0でDB検索が不要であり、起動依存とnative dependencyを増やす利点がないため。Library機能を追加するとき再評価する。

書込みはtemp file → replace/moveの原子的更新を基本とし、破損時は初期値へfallbackする。

## 12. Windowsファイル関連付け

Cometは`.zip/.cbz`のDefault Apps候補として登録するが、既定アプリを強制変更しない。

Installer選択時に:

- ProgID `Comet.Zip`, `Comet.Cbz`
- `shell\open\command = "Comet.exe" "%1"`
- `RegisteredApplications`
- `Capabilities\FileAssociations`
- `OpenWithProgids`

を登録する。

Windows側でユーザーがCometを既定に設定した後、ZIPダブルクリックが `Comet.exe "path\book.zip"` になることを受入試験する。

## 13. 配布方式

### Development

Framework-dependent Debugを基本にする。

### Release candidate

```powershell
dotnet publish src/Comet.App/Comet.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishReadyToRun=true `
  -p:PublishSingleFile=false
```

成果物をInno Setupでインストーラ化する。

x64をv1.0基準とし、ARM64はCore/Infrastructureを共通のまま別RID publishで追加できる構成にする。

## 14. 外部依存方針

v1.0の初期実装は**アプリ本体のNuGet runtime dependencyを原則ゼロ**にする。

- DI container: 不採用。constructor injectionを手書き。
- MVVM Toolkit: 不採用。必要になるまで導入しない。
- Logging framework: 不採用。`Stopwatch` + `EventSource`/独自軽量計測。
- ZIP library: 標準ZipArchive。
- Image library: WPF/WIC。

WebP等、WICだけで保証できないcodecがMustに昇格した場合は、codec adapterとしてSkiaSharp等を**遅延ロード**する。これにより起動経路を汚さない。

## 15. 計測設計

開発ビルドでは以下をStopwatch/EventSourceで記録する。

- ProcessStart → MainWindow.Shown
- OpenRequest → central directory ready
- OpenRequest → first page bytes ready
- OpenRequest → first BitmapSource ready
- OpenRequest → first render presented
- PageCommand → cache hit/miss
- PageCommand → rendered
- decode duration / page
- archive read duration / page
- cache bytes / hit ratio

### 暫定性能目標

以下は受入契約値ではなく**最適化方向を判断するための初期engineering target**。ユーザーPCで基準測定後に改定する。

| 指標 | 初期目標 |
|---|---:|
| warm start → interactive shell | 300 ms級 |
| cold start → interactive shell | 700 ms以内を狙う |
| local ZIP → 1ページ目表示 | 1秒以内を狙う |
| cache hit page turn | 50 ms以内を狙う |
| Ctrl+Wheel表示反応 | 同一UI frame内。decode待ち禁止 |

絶対値より、MComix Windows版との同一PC・同一書庫比較を重視する。

## 16. テスト戦略

### Unit

- Best Fit / Fit Width / Fit Height
- 小画像upscale
- 見開きgeometry
- Natural sort (`1, 2, 10`, Unicode)
- Zoom session lifecycle
- right-to-left spread order

### Integration

- ZIP/CBZ 10/200/1000 page
- CP932 filename ZIP
- nested folder in ZIP
- broken image entry
- huge image / safety limit
- book state persistence

### UI / manual acceptance

- ZIP double-click
- 100% / 125% / 150% / 200% Windows scale
- monitor移動
- fullscreen
- sidebar toggle
- right-to-left + Best Fit default
- Ctrl+Wheel shrink → Next ZIP → same factor
- app restart → Base Fitへ復帰

### Performance

固定fixtureを用意し、MComixとCometを同じSSD・同じPCで比較する。主観評価「ページ送りで待ちを感じない」と計測値を両方残す。

## 17. Failure / fallback設計

| Failure | 動作 |
|---|---|
| settings.json破損 | default設定で起動。破損ファイルは上書き可能。 |
| 1画像decode失敗 | error placeholder。前後ページは継続。 |
| ZIP破損 | 書籍Open失敗として通知。アプリ自体は継続。 |
| cache不足 | LRU eviction。機能継続。 |
| decoder遅延 | 既存低解像度Bitmapを表示し続ける。 |
| GPU/WPF Tier低下 | ソフトウェア描画でも機能維持。性能ログにTierを記録。 |

## 18. 実装順序

### M0 — Skeleton

- solution/project境界
- FitCalculator / NaturalSort
- WPF window
- command line path open

### M1 — Fast reader vertical slice

- ZIP/CBZ direct open
- first page display
- next/previous
- Best Fit
- manga/right-to-left
- Ctrl+Wheel temporary zoom

### M2 — Page performance

- prefetch scheduler
- decoded bitmap LRU
- cancellation/generation
- resize debounce / decode-to-size

### M3 — MComix interaction parity

- Fit Width / Height
- Smart Scroll
- double-page rules
- fullscreen
- exact keybindings
- next/previous archive

### M4 — Product completeness

- thumbnails
- reading position
- bookmarks
- localization JA/EN
- error UI
- drag/drop

### M5 — Distribution

- self-contained publish
- Inno Setup
- file association registration
- clean uninstall
- acceptance/performance test

## 19. Architecture Decision Records

### ADR-001 — .NET 10 LTS
**Decision:** Adopt.
**Consequence:** Windows Desktop開発をC#で高速に進めつつ、2028年まで現行サポートを得る。

### ADR-002 — WPF
**Decision:** Adopt over WinUI 3/C++ for v1.0.
**Consequence:** 起動・配布の複雑性を抑え、MComix再現に集中する。描画抽象化を残す。

### ADR-003 — WPF/WIC first, Direct2D only by evidence
**Decision:** Do not pre-optimize.
**Consequence:** 現状必要な静止画像表示は単純実装。測定で不足した箇所だけ置換。

### ADR-004 — `ZipArchive`
**Decision:** Standard library first.
**Consequence:** 依存を減らす。RAR/7z対応時は別adapterを追加。

### ADR-005 — per-book JSON state
**Decision:** No SQLite in v1.0.
**Consequence:** 起動経路を軽くする。Library実装時にDBを再評価。

### ADR-006 — multi-file self-contained release
**Decision:** Prefer startup over single-EXE aesthetics.
**Consequence:** installerで配布体験を1ファイル化する。

### ADR-007 — no runtime framework dependencies initially
**Decision:** Hand-written composition.
**Consequence:** 起動・サイズ・障害切り分けが単純。必要性が出た依存だけ追加。

## 20. v0.1 Scaffoldとの関係

同時作成した `Comet_v0.1_Scaffold.zip` は上記境界をコード化した開始点である。現段階で次を含む。

- 4-project solution
- ZIP/CBZ / folder source abstraction
- natural sort
- Best Fit計算
- CP932 fallbackの骨格
- WPF/WIC decoder
- right-to-left PageViewportの骨格
- command-line open
- B/W/H/M/D/F/F11とCtrl+Wheelの初期input skeleton
- settings/book-state JSON
- self-contained ReadyToRun publish script
- Inno Setup file association registration skeleton

これはv1.0完成版ではない。次フェーズはM1として、Windows上でbuild/runし、最初の縦スライスを成立させたうえで性能計測を開始する。

## 21. 参照資料

- Microsoft .NET Support Policy: https://dotnet.microsoft.com/en-us/platform/support/policy
- .NET 10 downloads / C# 14: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- WPF documentation: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- WPF graphics rendering tiers: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/graphics-rendering-tiers
- WPF `BitmapImage.DecodePixelWidth`: https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.bitmapimage.decodepixelwidth?view=windowsdesktop-10.0
- .NET `ZipArchive` constructor / entry encoding: https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchive.-ctor?view=net-10.0
- ReadyToRun publishing: https://learn.microsoft.com/en-us/dotnet/core/deploying/
- WinUI 3 unpackaged deployment: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app
- Windows Default Programs registration: https://learn.microsoft.com/en-us/windows/win32/shell/default-programs
- MComix Keybindings: https://sourceforge.net/p/mcomix/wiki/Keybindings/
- MComix Documentation: https://sourceforge.net/p/mcomix/wiki/Documentation/
