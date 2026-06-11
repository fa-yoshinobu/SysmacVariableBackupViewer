# refactor-instructions.md — SysmacVariableBackupViewer

作成日: 2026-06-11。根拠: ソース・README.md・CHANGELOG.md・ci.bat・git履歴の実読。
本リポジトリ単体で完結する指示書。本ファイル自体は untracked のままにし、コミットに含めない。

---

## 1. Objective

CHANGELOG 1.0.4/1.0.5 で意図的に固定された型変換フォーマットと CSV エスケープ仕様を変えずに、
(1) その変換仕様を特性テストで固定し、(2) 実在する並行アクセスの危険箇所（無同期 Dictionary）を
最小差分で安全化し、(3) テンプレート残骸を掃除する。トリミング設定と未使用 `XmlWriter` は触らない。

## 2. Project Understanding

- Windows WPF アプリ (net8.0-windows, 単一EXE self-contained, ReadyToRun+Trimming)。
  Sysmac Studio の XML バックアップを読み込み、変数を表形式で表示・フィルタ・CSV出力する。
- アセンブリ名 `SysmacVariableBackupViewer`、プロジェクト/namespace は `SysmacXmlViewer`（歴史的経緯・現状維持）。
- 構成:
  - `Services/XmlParser.cs`: XML → `XmlBackupData`（`VariableItem` リスト + `ProjectInfo`）。
  - `Services/DataTypeConverter.cs`（36KB, static）: PLC型の表示変換。`ConcurrentDictionary` キャッシュ +
    `ClearCache()`。BOOL `1/0→True/False`、REAL/LREAL（16進8/16桁のみ・InvariantCulture+`F6`）、
    DATE_AND_TIME `yyyy-MM-dd-HH:mm:ss.ff`、TIME（24h上限なし・long範囲）、STRING（16進→UTF-8）等。
  - `Services/CsvExporter.cs`: 全フィールド `"`→`""` エスケープ + 二重引用符囲み。
  - `Services/XmlWriter.cs`: **現在どこからも呼ばれていない**（`MainViewModel` でインスタンス化のみ）。
  - `ViewModels/MainViewModel.cs`: 非同期ロード、フィルタ再実行の調停ロジック
    （`_filterLock`/`_pendingFilterRequest`/`_isFiltering` — CHANGELOG 1.0.4 で意図的に導入）、
    `ClearMemory()`（キャッシュクリア + `GC.Collect()` 強制）。
- 仕様根拠: `CHANGELOG.md`（特に 1.0.4 の「詳細」節が変換フォーマットの正本）と `README.md`。
- テスト: `SysmacXmlViewer.Tests`（xUnit）に `DataTypeConverterTests`（基本型のみ）・`XmlParserTests`・
  `UnitTest1.cs`（空テスト1個のテンプレート残骸）。
- 検証: `ci.bat` = restore → format(whitespace) → build(`/warnaserror` + analyzers, 一部CA警告は除外) →
  test → publish smoke test。GitHub Actions（ci / release / VirusTotal）。

## 3. Behaviors To Preserve

1. **型変換出力フォーマット**（CHANGELOG 1.0.4 で固定）: REAL/LREAL は InvariantCulture+`F6`、
   16進判定は偶数桁のみ・REAL/LREAL は 8/16桁のみ許容、DATE_AND_TIME/DATE は tick(100ns)丸め +
   `yyyy-MM-dd-HH:mm:ss.ff`/`yyyy-MM-dd`、TIME_OF_DAY は小数部 ff、TIME は24h上限なし、
   BOOL `1/0→True/False`（1.0.5 修正）、STRING[] 型は raw のまま、未知型は raw のまま、空値は空文字。
2. **CSVエスケープ**: 全フィールドを `"`→`""` エスケープし二重引用符で囲む（1.0.4 仕様）。
3. **フィルタ調停ロジック**: 連続入力時に最新条件で再実行される動作（`_pendingFilterRequest` ループ）。
   1.0.4 で意図的に導入された仕様であり、構造を変えない。
4. `AvailableDataTypes` の Dispatcher 上での差分更新（クロススレッド例外回避、1.0.4 仕様）。
5. STRING[n] 型をフィルタ一覧上 `STRING` に統一する `NormalizeDataType` の挙動。
6. `ClearMemory()` のキャッシュクリア + GC 強制実行（意図的な「メモリクリア」仕様として維持）。
7. csproj の publish/トリミング/ReadyToRun 設定一式（**2026-06-11 保守者承認済み: 触らない方針で確定**）。
8. csproj の `<Version>`、`.github/workflows/`。

## 4. Non-Negotiables

- 開始前に `git status` 確認。本ファイル以外の差分があれば停止・報告。
- 編集前に `ci.bat` を実行し baseline（成否・テスト件数）を記録。失敗なら作業中止・報告。
- 1論点=1コミット。`/warnaserror` のため、コミット前に `ci.bat` を通す。
- NuGet の追加・更新はしない（Tests に必要なものは揃っている）。
- `XmlWriter.cs` は削除も変更もしない（§5-2 参照）。

## 5. Stop And Ask Conditions

1. テストを書いたら現実装と CHANGELOG の仕様記述が矛盾した場合。
2. `XmlWriter` の扱い: 未使用だが CHANGELOG 1.0.4 に修正記録があり、過去または将来の XML 保存機能の
   可能性がある。**削除可否は保守者判断待ち**。発見事項があれば（参照の痕跡等）報告に含める。
3. 変更が CSV 出力・変換結果・フィルタ動作に影響しうる場合。
4. csproj / `.github/workflows/` に触れたくなった場合。

## 6. Baseline Commands

```bat
cd /d D:\refactor\SysmacVariableBackupViewer
git status
ci.bat
```

## 7. Debt Map

凡例: ✅=実装可 / ⚠️=条件付き(指定フェーズ厳守) / ❌=提案・報告のみ

| # | 負債 | 根拠 | 改善案 | 可否 |
|---|---|---|---|---|
| V1 | `XmlWriter` が `MainViewModel.cs:21,41` で生成されるが**一度も使われない**（保存UIなし） | Grep 全域で使用箇所なし。CHANGELOG 1.0.4 に XmlWriter 修正記録あり | 削除せず報告のみ（保守者判断待ち） | ❌ |
| V2 | `_dataTypeCache`（plain `Dictionary`）が `Task.Run` 内（`ApplyFiltersInternal`→`NormalizeDataType`）と UI スレッドの両方から無同期アクセス | `MainViewModel.cs:31,184-205,328` | `ConcurrentDictionary<string,string>` 化（挙動同一・最小差分）。テスト不能箇所のため差分レビューで担保 | ✅ |
| V3 | `async void LoadFile`、`GC.Collect()+WaitForPendingFinalizers()` 強制実行 | `MainViewModel.cs:104,159-160` | LoadFile は内部 try/catch 済みで実害小。GC 強制は意図的仕様の可能性。**変更しない**（記録のみ） | ❌ |
| V4 | `DataTypeConverter` キャッシュのキーが `$"{dataType}_{rawValue}"`（衝突理論上可）・ロードまで無制限成長 | `Services/DataTypeConverter.cs:14-31` | ロード毎に `ClearCache` 済みで実害小。現状維持（記録のみ） | ❌ |
| V5 | WPF に `EnableTrimming/TrimMode=link` + `SuppressTrimAnalysisWarnings`（WPFはトリミング非サポート） | `SysmacXmlViewer.csproj` | **絶対に変更しない**（2026-06-11 保守者承認済み: 触らない方針で確定） | ❌ |
| V6 | `UnitTest1.cs` が空テスト1個（`Test1(){}`）のテンプレート残骸 | `Tests/UnitTest1.cs` | 削除 | ✅ |
| V7 | 変換仕様のテストが薄い: REAL/LREAL の F6・16進桁数制限、日時書式、TIME>24h、STRING 16進→UTF-8、CSVエスケープが未テスト | Tests 実読 | CHANGELOG 1.0.4 の「詳細」節を根拠に特性テスト追加。`CsvExporter` のエスケープも一時ファイルでテスト | ✅ |

## 8. Implementation Phases

1. **Phase 0 — baseline**: `git status` / `ci.bat` 実行・記録。失敗なら停止。
2. **Phase 1 — 掃除 (V6)**: `UnitTest1.cs` 削除。
3. **Phase 2 — 安全網 (V7)**: 特性テスト追加。テストは**変更前のコードで通る**こと。
   CHANGELOG と実装が矛盾したら §5-1 で停止。`CsvExporter` テストは一時ディレクトリを使用し後始末する。
4. **Phase 3 — 並行性修正 (V2)**: `_dataTypeCache` を `ConcurrentDictionary` 化
   （`TryGetValue` → `GetOrAdd` 等、ロジック・出力は不変）。
5. **Phase 4 — 提案のみ (V1, V3, V4, V5)**: XmlWriter の扱い・async void 整理案を最終レポートに記載。実装禁止。

## 9. Verification Requirements

- 各フェーズで `ci.bat` 完走（format / build(`/warnaserror`) / test / publish smoke test）。
- Phase 2 のテストは変更前コードで全件パスすることを確認してから Phase 3 へ。
- Phase 3 の前後で `dotnet test` の件数・成否が同一であること。
- 手動確認（Phase 3 後）: アプリ起動 → メイン画面表示。サンプル XML があれば読み込み →
  フィルタ入力連打 → CSV 出力まで確認。無ければ起動確認のみとし報告に明記。

## 10. Reporting Format

1. 実施フェーズ / 追加・変更ファイル / コミット一覧
2. baseline と最終の `ci.bat` 結果対比（テスト件数 before → after）
3. 最後に実行した検証コマンドと生出力
4. Stop And Ask 該当事項一覧（XmlWriter に関する発見を含む）
5. Phase 4 の設計提案（実装していないことを明記）
6. スキップした確認とその理由（サンプル XML 不在など）

## 11. Out-of-scope

- `XmlWriter.cs` の削除・変更、XML 保存機能の追加
- csproj のトリミング/publish/ReadyToRun 設定変更（承認済みで対象外確定）
- `.github/workflows/`、バージョン番号の変更
- フィルタ調停ロジックの構造変更、`ClearMemory` の GC 強制の除去
- UI/XAML・文言の変更、NuGet 追加・更新、新機能追加、網羅的整形

---

## 12. Implementation Status

実装済み（2026-06-12）。

実施済みフェーズ:

- Phase 0: baseline 確認済み。
- Phase 1: テンプレート残骸の `UnitTest1.cs` を削除済み。
- Phase 2: DataTypeConverter と CsvExporter の特性テストを追加済み。
- Phase 3: `_dataTypeCache` を `ConcurrentDictionary<string, string>` 化済み。
- Phase 4: `XmlWriter`、`async void LoadFile`、DataTypeConverter cache、trim/publish 設定は提案・記録のみで、実装対象外のまま維持。

実装コミット:

- `f344aae` Remove template test stub
- `f56eb2c` Add converter and CSV characterization tests
- `eedfc5b` Make data type cache concurrent

検証:

- `ci.bat` 完走。
- 最終テスト結果: 26 passed。
- publish smoke test passed。

補足:

- `XmlWriter` は従来どおり `MainViewModel` で生成されるのみで、保存 UI からの実使用は見つからない。削除・変更は保守者判断待ちとして未実施。
- サンプル XML が無いため、読み込み・フィルタ連打・CSV 出力の手動確認は未実施。アプリ起動 smoke は実施済み。
