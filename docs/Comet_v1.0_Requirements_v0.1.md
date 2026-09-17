# Comet v1.0 要求仕様書 v0.1

> Requirements Baseline — 2026-09-18

## 1. 目的

MComixの漫画閲覧UXをWindows上で高速・正確に再構成し、第三者へ配布可能な品質まで仕上げる。公開の有無は別途判断する。

## 2. 最重要ワークフロー

1. ZIP/CBZを開く。
2. 漫画モード（右綴じ）ON + Best Fitを基本状態とする。
3. 必要な場合だけCtrl+マウスホイールで一時縮小する。
4. 前/次ZIPへ移動してもセッション中は一時倍率を維持する。
5. Comet終了時に一時倍率を破棄し、次回は基本表示モードへ戻る。

## 3. v1.0機能要件

| ID | 要件 | 仕様 | 優先度 |
|---|---|---|---|
| FR-001 | 高速起動 | Windows上でComet本体が軽快に起動し、UIが操作可能になるまでの待ち時間を最小化する。 | Must |
| FR-002 | 高速読込 | ZIP/CBZまたは画像フォルダを開いた際、先頭ページを可能な限り早く表示する。全件展開や全画像デコードを初期表示の前提にしない。 | Must |
| FR-003 | ZIP/CBZ直接閲覧 | ZIP/CBZを事前展開せず、そのまま漫画として閲覧できる。 | Must |
| FR-004 | 画像フォルダ閲覧 | 画像ファイルを含むフォルダを直接開いて閲覧できる。 | Must |
| FR-005 | ファイル関連付け | .zip/.cbzの対応アプリとしてWindowsへ登録し、ユーザーが既定アプリに設定した後はダブルクリックで対象書庫をCometで直接開ける。 | Must |
| FR-006 | 単ページ表示 | 1ページ単位の表示モードを提供する。 | Must |
| FR-007 | 見開き表示 | 2ページ見開き表示を提供する。先頭ページは単独表示し、その後を見開きとする。 | Must |
| FR-008 | 漫画モード | 右綴じ（右→左）を提供し、ページ順・見開き配置・該当するスクロール/移動方向をMComix互換とする。標準はON。 | Must |
| FR-009 | Best Fit | 画像全体を表示領域に収める。小さい画像も表示領域に合わせて拡大する。標準表示モード。 | Must |
| FR-010 | Fit Width | 縦横比を維持し、表示領域の幅に合わせる。 | Must |
| FR-011 | Fit Height | 縦横比を維持し、表示領域の高さに合わせる。 | Must |
| FR-012 | Manual Zoom | 拡大・縮小を提供する。Ctrl+マウスホイールを含め、実装対象操作はMComix互換とする。 | Must |
| FR-013 | Smart Scroll | Space/マウスホイール等によるMComix型Smart Scrollを提供し、ページ端到達時のページ送りと連携する。 | Must |
| FR-014 | 前/次アーカイブ | 同一フォルダ内の前/次ZIP/CBZへ連続して移動できる。 | Must |
| FR-015 | 先読みキャッシュ | 現在ページ周辺を非同期で先読みし、ページ送りの待ち時間を最小化する。 | Must |
| FR-016 | 自然順ソート | 1,2,3…10のような自然順でページを並べ、日本語/Unicodeファイル名を扱う。 | Must |
| FR-017 | 全画面 | MComix互換の全画面表示を提供する。 | Must |
| FR-018 | サムネイル | サムネイル一覧を表示/非表示できる。生成はUI操作を妨げない低優先度処理とする。 | Must |
| FR-019 | 読書位置記憶 | アーカイブ/フォルダごとに最後に読んだページを保存し、再度開いた際に復元する。 | Must |
| FR-020 | Bookmark | 任意ページをブックマークとして登録・参照できる。 | Must |
| FR-021 | 設定保存 | 基本表示モード、漫画モード、UI表示状態等の継続設定を保存する。 | Must |
| FR-022 | 日本語/英語UI | Windows表示言語に追従し、日本語・英語UIを提供する。 | Must |
| FR-023 | MComix操作互換 | Comet v1.0で実装する機能について、キーバインドと操作意味を原則としてMComix互換にする。 | Must |
| FR-024 | ドラッグ&ドロップ | ZIP/CBZ、対応画像、画像フォルダをウィンドウへドロップして開ける。 | Should |
| FR-025 | 破損ページ継続 | 単一画像の破損や読込失敗でアプリ全体を停止させず、エラー表示のうえ前後ページへ移動できる。 | Should |

## 4. 状態保持

| 状態 | 保存範囲 | 挙動 |
|---|---|---|
| 基本表示モード（Best Fit / Fit Width / Fit Height） | 永続 | 次回起動時に復元 |
| 漫画モード ON/OFF | 永続 | 次回起動時に復元。初期値ON |
| 一時ズーム倍率（Ctrl+Wheel等） | セッション | 前/次アーカイブへ移動しても維持。Comet終了時に破棄 |
| 読書ページ | 書籍単位で永続 | 同じアーカイブ/フォルダを再度開いた際に復元 |
| サムネイル等のUI表示 | 永続 | 前回状態を復元 |
| 全画面状態 | セッション | 安全のため次回起動時には通常ウィンドウから開始 |

## 5. v1.0対象外 / 後回し

| 機能 | 扱い | 方針 |
|---|---|---|
| RAR/CBR | v1.x候補 | ZIP/CBZ安定後に追加 |
| 7z/CB7 | v1.x候補 | 同上 |
| PDF | v1.x候補 | 画像書庫とは別デコード経路のため後段 |
| Library | v1.x候補 | 読書管理機能。v1.0は読書位置+Bookmarkで代替 |
| Fixed Size / Fit to Size | v1.x候補 | ユーザーの主要ワークフロー外 |
| Slideshow | v1.x候補 | 閲覧コア外 |
| Magnifier | v1.x候補 | 閲覧コア外 |
| 画像補正 | v1.x候補 | 明るさ/コントラスト等は後段 |
| 外部コマンド | 対象外候補 | 必要性が生じた場合のみ再評価 |

## 6. 非機能要件

- **NFR-001 レスポンス優先**: 機能数より起動・初回表示・ページ送り・ズーム/リサイズ時の体感応答を優先する。
- **NFR-002 非同期I/O**: 書庫読込・画像デコード・サムネイル生成・先読みは可能な限りUIスレッドから分離する。
- **NFR-003 メモリ上限管理**: 先読みは無制限に蓄積せず、ページ数・画像サイズ・使用メモリに応じたLRU等で制御する。
- **NFR-004 DPI対応**: Windowsの表示スケール、複数モニター、ウィンドウ移動時もFit計算を正しく行う。
- **NFR-005 Viewport基準**: Fit計算はウィンドウ外形ではなく、ツールバー/サイドバー等を除く実画像表示領域を基準にする。
- **NFR-006 設定耐障害性**: 設定ファイルが欠損/破損しても初期値で起動できる。
- **NFR-007 ポータブルな成果物**: 最終的に第三者へ配布可能なWindows成果物（インストーラまたは自己完結型パッケージ）を生成できる構成にする。
- **NFR-008 診断可能性**: 起動、書庫読込、初回描画、デコード、キャッシュヒット等の時間を開発ビルドで計測できる。
- **NFR-009 Unicode**: 日本語を含むパス/ファイル名/書庫内エントリを正しく扱う。
- **NFR-010 MComix非干渉**: MComixのインストール、設定、ファイルを変更・上書きせず、独立したアプリとして動作する。

## 7. 受入条件

| ID | 試験 | 合格条件 |
|---|---|---|
| AC-001 | ZIP/CBZをダブルクリック | WindowsでCometを既定アプリとしてユーザー設定済みの場合、Cometが起動し対象書庫の読書画面が直接開く。 |
| AC-002 | Best Fit | 任意のウィンドウサイズで画像全体がViewport内に収まり、縦横比が維持される。小画像は拡大される。 |
| AC-003 | Resize/DPI | ウィンドウリサイズ、サイドバー表示切替、全画面、DPI/モニター変更後もFitが再計算される。 |
| AC-004 | Manga mode | 見開きで右ページから左ページへ読む配置になり、次/前移動方向もMComix互換になる。 |
| AC-005 | Temporary zoom | Best FitからCtrl+Wheelで一時ズームし、次アーカイブへ移動しても倍率が維持される。アプリ再起動後は基本表示モードへ戻る。 |
| AC-006 | Reading position | 途中ページで終了した書庫を再度開くと、そのページを復元する。 |
| AC-007 | Page turn | 先読み済みの次ページは、ユーザーが待ち時間をほぼ意識しない速度で切り替わる。 |
| AC-008 | Error resilience | 書庫内の1ファイルが読めなくても、Cometがクラッシュせず前後ページへ移動できる。 |
| AC-009 | Natural sort | 001.jpg, 2.jpg, 10.jpg等を自然順で並べる。 |
| AC-010 | Localization | 日本語Windowsでは日本語、英語Windowsでは英語UIを既定表示できる。 |

## 8. 参照資料

- [MComix SourceForge Overview](https://sourceforge.net/projects/mcomix/)
- [MComix Wiki - Documentation](https://sourceforge.net/p/mcomix/wiki/Documentation/)
- [MComix Wiki - Keybindings](https://sourceforge.net/p/mcomix/wiki/Keybindings/)
- [MComix Wiki - Preferences](https://sourceforge.net/p/mcomix/wiki/Preferences/)
- [Microsoft Learn - Windows app defaults platform](https://learn.microsoft.com/windows/apps/develop/windows-integration/default-apps-platform)
- [Microsoft Learn - File type and URI associations model](https://learn.microsoft.com/windows/compatibility/file-type-and-protocol-associations-model)