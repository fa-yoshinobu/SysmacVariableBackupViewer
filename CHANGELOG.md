# 更新履歴 (CHANGELOG)

## [1.0.4]

このリリースでは、潜在的な不具合の修正と数値・日付処理、エクスポート処理、UI 更新の信頼性向上を中心にメンテナンスを行いました。

### ハイライト
- フィルタ再実行を調停するロジックを追加し、連続入力でも常に最新条件で変数一覧が更新されるように改善。
- REAL/LREAL や DATE/TIME 系の変換処理を見直し、小数精度と相互変換の安定性を向上。
- CSV エクスポートおよび XML 保存処理を再調整し、データの欠損や再変換による破損を防止。
- UI スレッドでのデータ型リスト更新を安全化し、例外発生と描画のちらつきを回避。

### 変更概要
#### 数値・日付変換
- REAL/LREAL の16進文字列→数値変換時の入力条件とエンディアン処理を是正。
- DATE_AND_TIME/DATE/TIME_OF_DAY の相互変換で小数部の精度と書式揃えを統一。
- TIME 値の検証で 24h 制限を撤廃し、連続時間として long 範囲まで許容。

#### エクスポート／永続化
- CSV 出力時に全フィールドへダブルクォートエスケープを施し、改行やコンマを安全に出力。
- XML 保存時は変換済みの表示値ではなく解析済みの生値（`v.Value`）を記録し、再変換による誤差を防止。
- `EnableOffset` の読み込みを `bool.TryParse` 化して、意図しない値での例外発生を抑制。

#### UI・スレッド処理
- データ型フィルタ `AvailableDataTypes` を `ObservableCollection<string>` 化し、Dispatcher 上での差分更新に対応。
- CSV/変換ロジックと同様に UI 更新のスレッド安全性を確保。

### 影響ファイル
- `SysmacXmlViewer/Services/DataTypeConverter.cs`
- `SysmacXmlViewer/Services/CsvExporter.cs`
- `SysmacXmlViewer/Services/XmlWriter.cs`
- `SysmacXmlViewer/Services/XmlParser.cs`
- `SysmacXmlViewer/ViewModels/MainViewModel.cs`

### 詳細
#### DataTypeConverter
- 16進文字列判定を偶数桁のみ許可する形に厳格化し、`IsHexString()` を更新。
- REAL/LREAL の16進→浮動小数点変換で 8/16 桁のみを許容、重複していた `Array.Reverse` を整理し、`InvariantCulture`+`F6` で出力を固定。
- DATE_AND_TIME/DATE の数値→表示変換で tick（100ns）単位に丸めて `AddTicks()` を適用し、フォーマットを `yyyy-MM-dd-HH:mm:ss.ff`／`yyyy-MM-dd` に統一。
- DATE_AND_TIME/DATE の表示→数値変換で `DateTimeStyles.AssumeUniversal | AdjustToUniversal` を使用し、差分 tick を 100ns 単位で復元。
- TIME_OF_DAY 変換の小数部を ff（1/100 秒）に統一。
- TIME の検証で 24 時間上限チェックを取り除き、長時間の連続値を許容。

#### CsvExporter
- すべてのフィールドを `"`→`""` にエスケープした上で二重引用符で囲む処理を追加し、`ExportToCsv()` から `EscapeCsv()` を呼び出すように変更。

#### XmlWriter
- 変数値保存時に表示値ではなく解析済みの生値を保持し、REAL/LREAL/日時などの再変換ズレを解消。

#### XmlParser
- `EnableOffset` の解析を `bool.TryParse` に切り替え、空文字や 0/1 などの入力でも例外が発生しないよう防御。

#### MainViewModel
- `AvailableDataTypes` を `ObservableCollection<string>` に変更し、UI スレッド（Dispatcher）上で `Clear()`／`Add()` を実行して ComboBox の即時更新とクロススレッド例外を回避。


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
