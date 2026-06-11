# TnmsPluginFoundation → Wuling 移行計画

> 作成: 2026-06-11 (MapChooserSharpMS セッションでの設計議論より)
> このファイルは移行完了後に削除する。main にはコミットしない (作業ブランチに乗せるのは可)。

## ゴール

TnmsPluginFoundation の 3 つのプラットフォーム依存を廃止し、内部フレームワーク
**Wuling** (`D:\myworks\github\cs2\ModSharp\Wuling`) + ModSharp 公式モジュールに置き換える。

| 廃止する依存 | 置き換え先 |
|---|---|
| `TnmsAdministrationPlatform.Shared` 0.0.4 | `Wuling.Abstract` の `IAuthority` |
| `TnmsLocalizationPlatform.Shared` 0.0.3 | `Wuling.Abstract` の `ILocalizer` / `IStringLocalizer` |
| `TnmsExtendableTargeting.Shared` 0.1.4 | **FPM の `Sharp.Modules.TargetingManager`** (ModSharp 公式) |

## 決定事項 (ユーザー確認済み)

1. **Targeting は FPM (Sharp.Modules.TargetingManager) を使う** — Wuling に Targeting Tianshi は作らない
2. **テスト移行用ブランチで作業** — 配布形態 (NuGet feed vs ProjectReference) の最終判断は後回し。
   ブランチ上では Wuling.Abstract への ProjectReference で進める
3. **Adapter で旧表面を温存しない。Wuling ネイティブ API に書き換える** —
   `ITnmsPlfdAdminManager` / `ITnmsLocalizer` 形の抽象は削除し、`IAuthority` / `IStringLocalizer` を直接公開する
   (下流プラグイン側も書き換える前提。下流の MapChooserSharpMS は別セッションで追従させる)

## 事前調査結果

### 影響ファイル (grep: AdminManager|Localizer|ExtendableTargeting|LocalizationPlatform, 46 hits / 9 files)

| ファイル | 内容 |
|---|---|
| `TnmsPlugin.cs` (21 hits) | 中心。L59 `static IExtendableTargeting ExtendableTargeting`、L61 `static ITnmsPlfdAdminManager AdminManager`、L64-65 `Localizer`/`LocalizationPlatform`、L242-267 で 3 モジュールを `GetRequiredSharpModuleInterface` 取得、L275 `CreateAdminManager()` virtual シーム |
| `TnmsPlugin.Localization.cs` (10 hits) | ローカライズヘルパー群 |
| `Interfaces/ITnmsPlfdAdminManager.cs` | 削除対象 (admin 抽象。permission/can-target/immunity/group 操作) |
| `Interfaces/ITnmsPluginBase.cs` (1 hit) | 参照箇所の型差し替え |
| `Models/Admin/TnmsAdminManagerWrapper.cs` | 削除対象 (IAdminManager の wrapper) |
| `Models/Admin/TnmsPlfdAdminUserAdapter.cs` | 削除対象 |
| `Models/Admin/TnmsPlfdAdminGroupAdapter.cs` | 削除対象 |
| `Models/Command/Validators/PermissionValidator.cs` (1 hit) | `TnmsPlugin.AdminManager.ClientHasPermission` → `IAuthority.PlayerHasPermission(steamId, node)` |
| `Models/Command/Validators/ExtendableTargetValidator.cs` (3 hits) | TargetingManager ベースに書き換え |
| `Models/Command/ValidatedArguments.cs` (1 hit) | targeting 型の差し替え |
| `Models/Logger/AbstractDebugLoggerBase.cs` (1 hit) | 参照箇所の確認・差し替え |

### API 対応表

**Admin → IAuthority** (`Wuling.Abstract/Tianshi/Authority/IAuthority.cs`) — ほぼ 1:1:
- `ClientHasPermission(IGameClient?, node)` → `PlayerHasPermission(ulong steamId, string permission, string? serverName = null)`
- `ClientCanTarget(client, target)` → `PlayerCanTarget(executorId, targetId)`
- immunity get/set → `GetPlayerImmunity` / `SetPlayerImmunity`
- Add/RemovePermissionToClient → `AddPermissionToPlayer` / `RemovePermissionFromPlayer` (expiresAtUtc 付き)
- グループ操作 → `AddPlayerToGroup` / `AddPermissionToGroup` / `CreateGroup`
- 戻り値: `PermissionModifyResult` → `AuthoritySaveResult`
- 注意: `IAuthority.GetGroups()` 列挙は無い (旧 Wrapper も NotSupported だったので機能後退なし)
- `IPlayerEntry` 拡張 (`AuthorityExtensions`): `entry.HasPermission(node)` 等が zero-setup で使える

**Localizer → ILocalizer / IStringLocalizer** (`Wuling.Abstract/Tianshi/Localizer/`):
- 取得: `IWuling.Localizer.CreateStringLocalizer(string moduleDirectory)` → `IStringLocalizer`
  (旧: `ITnmsLocalizationPlatform.CreateStringLocalizer(this)` — 引数がプラグインインスタンスから moduleDirectory 文字列に変わる)
- `[name]` / `[name, args]` インデクサ → 同形あり
- `[name, culture]` / `[name, culture, args]` → **`ForCulture(name, culture, args)` に変わる** (インデクサ無し)
- `ForClient(IGameClient, ...)` → `ForPlayer(ulong steamId, ...)`
- `GetClientCulture(client)` → `ILocalizer.GetPlayerCulture(steamId)`
- 戻り値: `TnmsLocalizedString` → Wuling の `LocalizedString`

**Targeting → Sharp.Modules.TargetingManager**:
- NuGet: `ModSharp.Sharp.Modules.TargetingManager.Shared` (2.1.123 系列)
- API 詳細は未調査。**着手時に必ず確認**:
  - ローカルソース: `D:\myworks\github\cs2\ModSharp\modsharp-public\Sharp.Modules\TargetingManager\`
  - カタログ: MapChooserSharpMS リポジトリの `refs/modsharp-knowledge/catalog/projects/Sharp.Modules.TargetingManager.Shared/`
- 旧 `IExtendableTargeting` の custom target 登録 (`RegisterCustomSingleTarget` 等) に相当する機能の有無を確認し、
  無ければ Validator の対応表現を縮退させるか検討

### Wuling facade の取得方法

```csharp
// OnAllModulesLoaded 内
var wuling = SharedSystem.GetSharpModuleManager()
    .GetOptionalSharpModuleInterface<IWuling>(IWuling.Identity)?.Instance;
// wuling.Authority / wuling.Localizer / wuling.Registry / wuling.EventBus / ...
```
旧 3 モジュール取得部 (TnmsPlugin.cs L242-267) をこれ 1 つに置き換える。
Required にするか Optional+graceful degrade にするかは実装時に判断
(旧実装は全部 Required で throw していた)。

## ビルド互換性の注意

- 両方 **net10.0** だが、Wuling.Abstract は **LangVersion 14** で C# 14 の extension member 構文
  (`extension(IPlayerEntry entry)`) を使用。TnmsPluginFoundation は config.props で
  **LangVersion 13 → 14 に上げる必要あり** (extension member を構文として消費する場合)
- ビルドには **.NET SDK 10.0.301 以上** (2026-06-11 にインストール済。10.0.100 だと Roslyn 不足の可能性)
- `ModSharp.Sharp.Shared` は現在 2.1.121 → ついでに **2.1.123 に bump** 推奨
  (MapChooserSharpMS / Wuling.Abstract は 2.1.123 相当に揃っている)

## 作業手順

1. `feature/wuling-migration` ブランチ作成 (main はクリーン、5ceaa2f 起点)
2. csproj 差し替え:
   - 3 つの Tnms* PackageReference 削除
   - `<ProjectReference Include="..\..\Wuling\Wuling.Abstract\Wuling.Abstract.csproj" />`
     (相対パスは環境依存なので注意。リポジトリ位置: 両方 `D:\myworks\github\cs2\ModSharp\` 直下)
   - `ModSharp.Sharp.Modules.TargetingManager.Shared` PackageReference 追加 (ExcludeAssets="runtime")
   - Sharp.Shared 2.1.123 bump、config.props LangVersion 14
3. Admin 層: ITnmsPlfdAdminManager + Wrapper + Adapter×2 削除、
   `TnmsPlugin.AdminManager` を `IAuthority` 型に変更 (static 維持)、`CreateAdminManager()` シーム削除、
   PermissionValidator を `PlayerHasPermission(client.SteamId, node)` に
4. Localizer 層: `Localizer` プロパティを `IStringLocalizer` 型に、`TnmsPlugin.Localization.cs` の
   ヘルパー ([name, culture] 形を ForCulture に) 書き換え
5. Targeting: TargetingManager API 調査 → ExtendableTargetValidator / ValidatedArguments 書き換え
6. `dotnet build` (0 warning / TreatWarningsAsErrors) 確認
7. レビュー (Fable モデル併用可) → コミット

## 下流への影響 (このリポジトリ完了後、MapChooserSharpMS セッションで対応)

MapChooserSharpMS は TnmsPluginFoundation 0.0.6 を NuGet 参照中。移行版を使うには:
- PackageReference → ProjectReference 切替 (テストブランチ運用)
- `TnmsPlugin.AdminManager.ClientHasPermission` 使用箇所の書き換え
  (例: `Modules/Nomination/Services/NominationValidateService.cs` の `IsPlayerDeniedByPermission`)
- `plugin.Localizer[key, culture]` パターンの書き換え (EventManager/Events/ 配下の params 各位 + モジュール)
- `PermissionValidator("mcs.admin.nominate")` は表面が変わらなければ無変更
- 将来: Client prefs を Wuling `ICookie` へ、Cooldown 永続化は SurrealDB
  (MapChooserSharpMS 側のメモリ/CLAUDE.md に設計記録あり)
