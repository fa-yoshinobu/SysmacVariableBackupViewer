# SysmacVariableBackupViewer

Sysmac Studioで作成されたXMLバックアップファイルを表示・分析するためのWPFアプリケーションです。

## 📋 概要

このアプリケーションは、Sysmac Studio（オムロンのPLCプログラミングソフトウェア）で作成されたXMLバックアップファイルを読み込み、変数の値を人間が読みやすい形式で表示します。特に、16進数データや特殊なデータ型（日時、時間、文字列配列など）を適切に変換して表示する機能を提供します。

## ✨ 主な機能

### 🔍 ファイル読み込み・表示
- **XMLファイル読み込み**: Sysmac StudioのバックアップXMLファイルを読み込み
- **プロジェクト情報表示**: プロジェクト名、デバイスモデル、バージョン情報を表示
- **変数一覧表示**: すべての変数を表形式で表示

### 🔧 データ変換機能
- **16進数変換**: 16進数データをUTF-8文字列に変換
- **日時変換**: DATE_AND_TIME型を読みやすい日時形式に変換
- **時間変換**: TIME型をミリ秒形式に変換
- **浮動小数点変換**: REAL/LREAL型を10進数表記に変換
- **文字列配列処理**: STRING[]型の配列データを適切に処理

### 🔍 フィルタリング機能
- **テキスト検索**: 変数名、データ型、値でリアルタイム検索
- **データ型フィルタ**: 特定のデータ型のみを表示
- **リアルタイム更新**: フィルタ条件変更時に即座に結果を更新

### 📊 エクスポート機能
- **CSV出力**: 表示中の変数データをCSVファイルにエクスポート
- **UTF-8対応**: 日本語文字も正しく出力

### ⚡ パフォーマンス最適化
- **非同期処理**: ファイル読み込みとフィルタリングを非同期で実行
- **キャッシュ機能**: データ変換結果をキャッシュして高速化
- **仮想化**: 大量データでもスムーズにスクロール

## 🚀 使用方法

### 1. アプリケーション起動
```
SysmacVariableBackupViewer.exe
```

### 2. ファイル読み込み
1. **「Load File」ボタン**をクリック
2. Sysmac StudioのXMLバックアップファイル（*.xml）を選択
3. ファイルが読み込まれ、変数一覧が表示されます

### 3. データの確認
- **プロジェクト情報**: 上部にプロジェクト名、デバイス、バージョンが表示
- **変数一覧**: メイン画面に変数の詳細情報が表示
- **統計情報**: 総変数数と表示中の変数数が表示

### 4. フィルタリング
- **テキスト検索**: 「Filter」テキストボックスに検索語を入力
- **データ型フィルタ**: 「Data Type」ドロップダウンから特定のデータ型を選択
- **フィルタクリア**: 「Clear Filters」ボタンでフィルタをリセット

### 5. データエクスポート
- **「Export CSV」ボタン**をクリック
- 保存先を選択してCSVファイルを出力

### 6. アプリケーション情報の確認
- **メニューバー**の「Help」→「About」をクリック
- アプリケーションのバージョン情報、ライブラリ情報、ライセンス条項を確認

## 📁 表示される情報

### 変数一覧の列
| 列名 | 説明 |
|------|------|
| Variable Name | 変数名（階層構造を含む） |
| Data Type | データ型（STRING[]型はSTRINGに統一表示） |
| Value | 元の値（XMLから読み込んだ生データ） |
| Converted Value | 変換後の値（人間が読みやすい形式） |
| Offset | メモリオフセット |
| Group | グループ名（階層の最初の部分） |
| Array Index | 配列インデックス（配列の場合） |

### 対応データ型
- **BOOL**: 真偽値
- **WORD/UINT**: 16ビット符号なし整数
- **INT**: 16ビット符号付き整数
- **REAL**: 32ビット浮動小数点
- **LREAL**: 64ビット浮動小数点
- **STRING**: 文字列（16進数データをUTF-8に変換）
- **STRING[]**: 文字列配列
- **DATE_AND_TIME**: 日時（ナノ秒→読みやすい形式）
- **TIME**: 時間（ナノ秒→ミリ秒形式）
- **DATE**: 日付（ナノ秒→YYYY-MM-DD形式）
- **TIME_OF_DAY**: 時刻（ナノ秒→HH:MM:SS.FF形式）

## 🛠️ 技術仕様

### 開発環境
- **.NET 6.0**: Windows WPFアプリケーション
- **C#**: プログラミング言語
- **WPF**: UIフレームワーク
- **MVVM**: アーキテクチャパターン

### 最適化機能
- **単一ファイル配布**: `PublishSingleFile=true`
- **自己完結型**: `SelfContained=true`
- **ReadyToRun**: 起動高速化
- **トリミング**: 不要なコードを除去
- **圧縮**: ファイルサイズ最適化

### メモリ管理
- **キャッシュ機能**: 変換結果をキャッシュして高速化
- **非同期処理**: UIブロッキングを防止
- **仮想化**: 大量データの効率的な表示
- **ガベージコレクション**: メモリ使用量の最適化

## 📦 ビルド方法

### 前提条件
- .NET 6.0 SDK
- Visual Studio 2022 または Visual Studio Code

### ビルド手順

#### 1. コマンドラインでのビルド
```bash
# 依存関係の復元
dotnet restore

# リリースビルド
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

#### 2. バッチファイルでのビルド
```bash
# build.batを実行
build.bat
```

### 出力ファイル
- **実行ファイル**: `SysmacVariableBackupViewer.exe`
- **出力先**: `bin\Release\net6.0-windows\win-x64\publish\`

## 🏗️ プロジェクト構造

```
SysmacVariableBackupViewer/
├── SysmacXmlViewer/
│   ├── Commands/           # コマンド実装
│   │   └── RelayCommand.cs
│   ├── Models/             # データモデル
│   │   ├── ProjectInfo.cs
│   │   ├── VariableItem.cs
│   │   └── XmlBackupData.cs
│   ├── Services/           # ビジネスロジック
│   │   ├── CsvExporter.cs
│   │   ├── DataTypeConverter.cs
│   │   ├── XmlParser.cs
│   │   └── XmlWriter.cs
│   ├── ViewModels/         # ビューモデル
│   │   └── MainViewModel.cs
│   ├── Views/              # UI
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── AboutWindow.xaml
│   │   └── AboutWindow.xaml.cs
│   ├── ViewModels/         # ビューモデル
│   │   ├── MainViewModel.cs
│   │   └── AboutViewModel.cs
│   ├── App.xaml            # アプリケーション設定
│   ├── SysmacXmlViewer.csproj
│   ├── build.bat           # ビルドスクリプト
│   ├── LICENSE.txt         # アプリケーションライセンス
│   └── Licenses/           # サードパーティライセンス
│       └── THIRD_PARTY_LICENSES.txt
├── SysmacXmlViewer.sln     # ソリューションファイル
└── README.md               # このファイル
```

## 🔧 カスタマイズ

### 新しいデータ型の追加
1. `Services/DataTypeConverter.cs`に変換メソッドを追加
2. `Models/VariableItem.cs`の`ConvertedValueDisplay`プロパティを更新
3. 必要に応じてUIの列を追加

### フィルタ条件の追加
1. `ViewModels/MainViewModel.cs`の`ApplyFiltersInternal`メソッドを修正
2. UIに新しいフィルタコントロールを追加

### エクスポート形式の追加
1. `Services/`フォルダに新しいエクスポータークラスを作成
2. `ViewModels/MainViewModel.cs`にエクスポートコマンドを追加

## 🐛 トラブルシューティング

### よくある問題

#### 1. ファイルが読み込めない
- **原因**: XMLファイルの形式が正しくない
- **解決策**: Sysmac Studioで正しくエクスポートされたXMLファイルを使用

#### 2. 文字化けが発生する
- **原因**: エンコーディングの問題
- **解決策**: UTF-8エンコーディングで保存されたファイルを使用

#### 3. アプリケーションが重い
- **原因**: 大量の変数データ
- **解決策**: フィルタ機能を使用して表示するデータを絞り込み

#### 4. メモリ不足エラー
- **原因**: 非常に大きなXMLファイル
- **解決策**: ファイルを分割するか、フィルタ機能を使用

## 📄 ライセンス

このプロジェクトは独自のライセンス条項の下で公開されています。詳細は`LICENSE.txt`ファイルを参照してください。

### サードパーティライブラリ
このアプリケーションは以下のサードパーティライブラリを使用しています：
- **System.Text.Encoding.CodePages (7.0.0)**: MIT License
- **.NET 6.0 Runtime**: MIT License
- **Windows Presentation Foundation (WPF)**: MIT License

詳細なライセンス情報は`Licenses/THIRD_PARTY_LICENSES.txt`ファイルを参照してください。

## 🤝 貢献

バグ報告や機能要望は、GitHubのIssuesページでお知らせください。

## 📞 サポート

技術的な質問や問題がある場合は、GitHubのIssuesページでお問い合わせください。

---

**注意**: このアプリケーションはSysmac StudioのXMLバックアップファイル専用です。他のXMLファイルでは正常に動作しない場合があります。