# 更新履歴 (CHANGELOG)

## [1.0.4]

### 修正
- フィルタ再実行の調停ロジックを導入し、連続入力時でも常に最新条件で変数一覧が更新されるように修正
# 更新履歴

このコミットでは、潜在的な不具合の修正と精度・安全性の向上を目的として、数値/日時の変換、CSV 出力、XML の読み書き、UI 更新のスレッド安全性に関する修正を行いました。

## 変更概要

- 浮動小数点（REAL/LREAL）の16進→数値変換時のエンディアン処理と判定条件を是正
- 日付/時刻（DATE_AND_TIME/DATE/TIME_OF_DAY）の相互変換で小数部の精度と表記の一貫性を確保
- TIME の妥当性チェックを緩和（継続時間として日数上限を撤廃）
- CSV 出力でフィールドのエスケープ（ダブルクォート）を実装
- XML 保存時に表示値→生値の再変換を行わず元の生値を書き戻すよう修正
- `EnableOffset` の解析を `TryParse` 化し例外リスクを排除
- データ型フィルタ（`AvailableDataTypes`）を `ObservableCollection` 化し、UI スレッドで安全に更新

## 影響ファイル

- `SysmacXmlViewer/Services/DataTypeConverter.cs`
- `SysmacXmlViewer/Services/CsvExporter.cs`
- `SysmacXmlViewer/Services/XmlWriter.cs`
- `SysmacXmlViewer/Services/XmlParser.cs`
- `SysmacXmlViewer/ViewModels/MainViewModel.cs`

## 変更詳細

### DataTypeConverter

- 16進判定を厳密化
  - 偶数桁のみを16進として許可（バイト境界でない文字列を除外）。
  - 該当箇所: `IsHexString()`

- REAL/LREAL の 16進→浮動小数点変換
  - 16進扱いは REAL=8 桁（4B）、LREAL=16 桁（8B）の場合に限定。
  - 既存のペア順入替（ビッグ→リトル補正）に加えた二重の `Array.Reverse` を除去し、反転の重複を解消。
  - 表示は `InvariantCulture` で `F6` 固定。

- DATE_AND_TIME/DATE（数値→表示）
  - ナノ秒値を tick（100ns）に換算して `AddTicks()` を使用。丸めによる小数秒欠落を防止。
  - フォーマットは `yyyy-MM-dd-HH:mm:ss.ff`（ff=1/100秒）、`yyyy-MM-dd`。`InvariantCulture` を使用。

- DATE_AND_TIME/DATE（表示→数値）
  - `DateTimeStyles.AssumeUniversal | AdjustToUniversal` を使用し UTC として解釈。
  - 差分 tick をナノ秒（tick×100）に変換して復元。

- TIME_OF_DAY（数値↔表示）
  - 表示の小数部を ff（1/100秒）へ統一（以前の 2 桁 ms 表記を廃止）。

- TIME の妥当性
  - 継続時間として扱うため 24h 上限チェックを撤廃（数値形式は long 範囲内を許容）。

### CsvExporter

- すべてのフィールドを CSV エスケープ（`"` → `""`）してから二重引用符で囲むよう変更。
- 影響箇所: `ExportToCsv()` に `EscapeCsv()` を追加し適用。

### XmlWriter

- 変数値の書き戻しで、表示値→生値の再変換を行わず、解析済みの生値（`v.Value`）をそのまま保存するよう修正。
  - REAL/LREAL/日時などの再変換による破損リスクを排除。

### XmlParser

- `EnableOffset` を `bool.TryParse` に変更し、空/0/1/その他値でも例外が出ないように安全化。

### MainViewModel

- データ型フィルタ `AvailableDataTypes` を `List<string>` から `ObservableCollection<string>` に変更。
- 別スレッドで種類を収集し、UI スレッド（Dispatcher）で `Clear()` と `Add()` を実施するように修正。
  - ComboBox に即時反映、クロススレッド例外の回避。

## 互換性/注意点

- TIME_OF_DAY の表示小数部が ff（1/100 秒）に統一されました。従来の 2 桁ミリ秒相当表示からの差異に注意してください。
- REAL/LREAL の 16進解釈は桁数で厳密化されました（8/16 桁以外は十進数として解釈）。
- XML 保存時に表示値を保存していた前提の外部ツール連携がある場合、`v.Value`（生値）保存への変更影響をご確認ください。

## 動作確認の目安

1. サンプル XML を読み込み、以下を確認
   - `DATE_AND_TIME` の `.ff` が 00 固定にならず小数部が表示される
   - `TIME_OF_DAY` が `HH:mm:ss.ff` で表示される
   - REAL/LREAL の 16進（8/16 桁）値が正しい 10 進表示になる
2. CSV エクスポート
   - カンマ/改行/ダブルクォートを含む値が適切にエスケープされている
3. XML 保存
   - 保存した XML の `<Data>` が表示値ではなく生値のままである（REAL 等が小数テキスト化されない）
4. UI
   - データ型コンボボックスがファイル読み込み直後に正しく更新される（例外なし）


## [1.0.3]

### 追加
- Aboutウィンドウに作者情報を追加
  - 作者名: fa-yoshinobu
  - GitHubリンク: https://github.com/fa-yoshinobu/SysmacVariableBackupViewer
- クリック可能なGitHubリンク機能を追加
  - デフォルトブラウザでGitHubページを開く
  - エラーハンドリング機能を実装

### 変更
- バージョンを1.0.3に更新
- AboutウィンドウのUIを改善（作者情報とGitHubリンクの追加）

### 修正
- Aboutウィンドウの表示内容を英語で統一
- ハイパーリンクのクリック処理を最適化

## [1.02]

### 追加
- Aboutウィンドウ機能を追加
  - アプリケーション情報表示（バージョン、ビルド日時、システム情報）
  - ライブラリ情報表示（依存関係、バージョン、ライセンス）
  - ライセンス条項表示（アプリケーションライセンス、サードパーティライセンス）
- メニューバー機能を追加
  - Fileメニュー（Load File、Export CSV、Exit）
  - Helpメニュー（About）
- ライセンスファイルを追加
  - LICENSE.txt（アプリケーションライセンス）
  - THIRD_PARTY_LICENSES.txt（サードパーティライセンス）

### 変更
- プロジェクトファイルにアセンブリ情報を追加
  - Company、Product、Description、Copyright情報を設定
- メインウィンドウにメニューバーを追加
- AboutViewModelで詳細なシステム情報を取得・表示

### 修正
- UI重複問題を修正（メニューバーとコンテンツの重複を解消）
- AboutViewModelでFileVersionInfo取得時の例外処理を追加
- Grid.Row定義を正しく調整
- メインウィンドウから重複するバージョン表示を削除（Aboutウィンドウで表示されるため）

### 技術仕様
- AboutWindow.xaml/.cs（AboutウィンドウUI）
- AboutViewModel.cs（Aboutウィンドウ用ビューモデル）
- LibraryInfo.cs（ライブラリ情報モデル）

## [1.01]

### 追加
- デバッグ用キャッシュクリア機能を追加
- App.xaml.csにXMLドキュメントコメントを追加

### 変更
- DATE_AND_TIME型とDATE型の変換処理を改善
- プロジェクトファイルのバージョンを1.0.1に更新
- 日時変換の基準日時処理を最適化

### 修正
- 日時データの変換精度を向上
- キャッシュ機能の安定性を改善

## [1.00]

### 追加
- Sysmac Studio XMLバックアップファイルの読み込み機能
- 変数データの表示・分析機能
- データ型変換機能（16進数、日時、時間、浮動小数点など）
- フィルタリング機能（テキスト検索、データ型フィルタ）
- CSVエクスポート機能
- 非同期処理によるUI応答性の向上
- キャッシュ機能によるパフォーマンス最適化

### 技術仕様
- .NET 6.0 WPFアプリケーション
- MVVMアーキテクチャ
- 単一ファイル配布対応
- 自己完結型アプリケーション
- ReadyToRun最適化
- トリミング機能によるファイルサイズ最適化
