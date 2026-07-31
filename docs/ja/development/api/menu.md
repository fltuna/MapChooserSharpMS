# ノミネーションメニュー互換 API

MCS はメニュー描画をプラグイン本体から切り離した抽象レイヤーを持っています。
サーバー運営者が好みのメニュープラグイン (FPM MenuManager、Wuling IMenu など) を選択できるようになっています。

登録は `IMapChooserSharpShared.SetNominationMenuCompat()` で行います。

---

## 全体の流れ

1. コンパニオンモジュール (例: `McsFPMCompat`) が `OnAllModulesLoaded` で `IMcsNominationMenuCompat` の実装を生成する
2. `IMapChooserSharpShared.SetNominationMenuCompat()` に渡して登録する
3. MCS がノミネーションメニューを表示する際、登録された実装の `ShowNominationMenu()` が呼ばれる

メニュー未登録のまま MCS がメニューを表示しようとした場合は `InvalidOperationException` がスローされます。

---

## IMcsNominationMenuCompat

ノミネーション関連メニューの互換アダプターインターフェースです。

**名前空間**: `MapChooserSharpMS.Shared.Ui.Menu`

| メンバー | 型 | 説明 |
|---|---|---|
| `NominationMenuService` | `INominationMenuManagementService` | MCS が登録時にセットします。コンストラクタでは `null!` で初期化してください |

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `ShowNominationMenu(IGameClient, McsNominationMenuContext)` | `void` | 対象クライアントにノミネーションメニューを表示する |
| `CloseMenu(IGameClient)` | `void` | 対象クライアントの MCS メニューを閉じる。開いていない場合は何もしない |
| `Cleanup()` | `void` | キャッシュ済みメニュー状態を全て破棄する。プラグインのアンロードやマップ遷移時に呼ばれる |

---

## McsNominationMenuContext

`ShowNominationMenu` に渡されるコンテキスト。メニューの構築に必要なデータとサービスを含みます。

**名前空間**: `MapChooserSharpMS.Shared.Ui.Menu`

| プロパティ | 型 | 説明 |
|---|---|---|
| `Title` | `string` | メニューのタイトル文字列 |
| `Items` | `IReadOnlyList<McsNominationMenuItem>` | ノミネーションメニュー項目のリスト (マップ名昇順でソート済み) |
| `AllowSortSelection` | `bool` | 一覧表示前にソート順選択 (例: `McsMapListSorter` 使用) を提供してよい場合 true。全マップ一覧メニューで true、検索結果・確認・削除メニューでは false |
| `ToolingService` | `IMapConfigToolingService` | 表示名の解決や Workshop ID の確認などのユーティリティ |
| `CooldownQueryService` | `IMapCooldownQueryService` | マップのクールダウン状態を問い合わせるサービス |
| `NominationMenuService` | `INominationMenuManagementService` | 互換インターフェースのプロパティと同じサービス |

`AllowSortSelection` (省略可、デフォルト `false`) 以外の全プロパティは `required init` です。
`AllowSortSelection` への対応は任意で、互換プラグインは渡された順で `Items` を表示するだけでも構いません。

---

## McsNominationMenuItem

ノミネーションメニュー内の 1 行分の項目です。

**名前空間**: `MapChooserSharpMS.Shared.Ui.Menu`

| プロパティ | 型 | 説明 |
|---|---|---|
| `DisplayText` | `string` | 表示テキスト (翻訳済み) |
| `MapConfig` | `IMapConfig` | この項目に関連付けられたマップ設定 |
| `OnNominate` | `Action<IGameClient>?` | クライアントがこの項目を選択したときに呼ばれるコールバック。`null` の場合は何もしない |

---

## McsMenuItem

ノミネーション確認メニューやイベント (`OnNominationMenuDetailsOpening` 等) で使われる汎用メニュー項目です。

**名前空間**: `MapChooserSharpMS.Shared.Ui.Menu`

| プロパティ | 型 | 説明 |
|---|---|---|
| `DisplayText` | `string` | 表示テキスト (翻訳済み) |
| `OnSelect` | `Action<IGameClient>?` | クライアントがこの項目を選択したときに呼ばれるコールバック。`null` の場合は何もしない |

---

## INominationMenuManagementService

ノミネーションメニューの表示・管理サービス。`IMcsNominationMenuCompat.NominationMenuService` (MCS が登録時にセット) および `IMcsNominationController.NominationMenuManagementService` から利用できます。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `ShowNominationMenu(IGameClient, List<IMapConfig>)` | `void` | 指定マップリストでノミネーションメニューを表示 |
| `ShowNominationMenu(IGameClient)` | `void` | 全マップでノミネーションメニューを表示 |
| `ShowAdminNominationMenu(IGameClient, List<IMapConfig>)` | `void` | 指定マップリストで管理者ノミネーションメニューを表示 |
| `ShowAdminNominationMenu(IGameClient)` | `void` | 全マップで管理者ノミネーションメニューを表示 |
| `ShowRemoveNominationMenu(IGameClient, List<IMcsNominationData>)` | `void` | 指定ノミネーションで削除メニューを表示 |
| `ShowRemoveNominationMenu(IGameClient)` | `void` | 全ノミネーションで削除メニューを表示 |
| `NominateOrConfirm(IGameClient, IMapConfig, bool)` | `void` | マップをノミネート。非管理者は常に確認メニューを先に表示、管理者は即時実行 |
| `CollectExtraMenuItems(IMapConfig, IGameClient)` | `List<McsMenuItem>` | `OnNominationMenuDetailsOpening` を発火し、収集された追加項目を返す |

---

## 登録フロー

ノミネーションメニュー互換の登録は `IMapChooserSharpShared` で行います:

```csharp
public void OnAllModulesLoaded()
{
    var mcs = _sharedSystem.GetSharpModuleManager()
        .GetRequiredSharpModuleInterface<IMapChooserSharpShared>(
            IMapChooserSharpShared.ModSharpModuleIdentity).Instance!;

    var menuManager = GetMenuManager(); // メニュープラグインの API

    mcs.SetNominationMenuCompat(new MyNominationMenuCompat(menuManager));
}
```

---

## OnNominationMenuDetailsOpening イベント

ノミネーション確認メニューが表示される直前に発火されます。外部プラグインは `McsMenuItem` を追加できます。

`INominationEventListener.OnNominationMenuDetailsOpening` を実装してください:

```csharp
public void OnNominationMenuDetailsOpening(INominationMenuDetailsOpeningParams @params)
{
    @params.ExtraItems.Add(new McsMenuItem
    {
        DisplayText = $"Cooldown: {@params.MapConfig.CooldownConfig.CurrentCooldown}",
        OnSelect = _ => { },
    });
}
```

| パラメータ | 型 | 説明 |
|---|---|---|
| `MapConfig` | `IMapConfig` | 確認メニューが表示されるマップ |
| `Client` | `IGameClient` | メニューを開いたクライアント |
| `ExtraItems` | `List<McsMenuItem>` | 変更可能なリスト — ここに追加項目を追加する |

---

## 実装例

```csharp
public sealed class MyNominationMenuCompat : IMcsNominationMenuCompat
{
    private readonly Dictionary<IGameClient, object> _activeMenus = new();

    public INominationMenuManagementService NominationMenuService { get; set; } = null!;

    public void ShowNominationMenu(IGameClient target, McsNominationMenuContext context)
    {
        CloseMenu(target);

        var builder = new SomeMenuBuilder();
        builder.SetTitle(context.Title);

        foreach (var item in context.Items)
        {
            var onNominate = item.OnNominate;
            builder.AddItem(item.DisplayText, () =>
            {
                CloseMenu(target);
                onNominate?.Invoke(target);
            });
        }

        var built = builder.Build();
        _activeMenus[target] = built;
        SomeMenuPlugin.Display(target, built);
    }

    public void CloseMenu(IGameClient target)
    {
        if (!_activeMenus.TryGetValue(target, out var menu))
            return;
        SomeMenuPlugin.Close(target, menu);
        _activeMenus.Remove(target);
    }

    public void Cleanup() => _activeMenus.Clear();
}
```
