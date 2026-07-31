# ノミネーション API

プレイヤーやサーバー管理者がマップを投票候補として推薦 (ノミネーション) する機能を提供します。
ノミネーションされたマップはマップ投票の候補として優先的に使用されます。

`IMapChooserSharpShared.McsNominationController` からアクセスします。

---

## IMcsNominationController

ノミネーションモジュール全体のファサードです。各サービスとマネージャーへのアクセス、およびイベントリスナーの登録・解除を提供します。

| メンバー | 型 | 説明 |
|---|---|---|
| `NominationService` | `IMapNominationService` | ノミネーションの追加・削除を行うサービス |
| `NominationValidateService` | `INominationValidateService` | ノミネーション可否のバリデーションサービス |
| `NominationMenuManagementService` | `INominationMenuManagementService` | ノミネーションメニューの表示を管理するサービス |
| `NominationManager` | `INominationManager` | 現在のノミネーション状態を読み取るマネージャー |

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `InstallEventListener(INominationEventListener)` | `void` | ノミネーションイベントのリスナーを登録する |
| `RemoveEventListener(INominationEventListener)` | `void` | リスナーの登録を解除する |

---

## IMapNominationService

ノミネーションの追加・削除を行うコアサービスです。バリデーション、イベント発火、状態更新を一括で処理します。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `TryNominateMap(IGameClient, IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | プレイヤーによるノミネーションを試みる。空リスト = 成功 |
| `TryAdminNominateMap(IGameClient?, IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | 管理者ノミネーションを試みる。`nominator` が `null` の場合はコンソール実行として扱われる。空リスト = 成功 |
| `TryRemoveNomination(IMapConfig, IGameClient?, bool)` | `bool` | 指定マップのノミネーションを削除する。`forceRemoval` が `true` の場合は管理者ノミネーションも強制的に削除できる |
| `TryUnNominate(IGameClient, UnNominateReason)` | `bool` | プレイヤーの現在のノミネーション参加を解除する。そのプレイヤーが唯一の参加者かつ管理者強制でなければ、ノミネーション自体も削除される |
| `TryUnNominate(int, UnNominateReason)` | `bool` | スロット番号による `TryUnNominate`。切断フック等で `IGameClient` が利用できない場合に使う |
| `ClearNominations()` | `bool` | 全ノミネーションをクリアする |

### ノミネーションのフロー

1. プレイヤーが `TryNominateMap` を呼び出す
2. `INominationValidateService.PlayerCanNominateMap` による内部バリデーション (権限、プレイヤー数、曜日・時間帯、クールダウン等)
3. バリデーション通過後、`INominationEventListener.OnNominationCheckPassed` イベントが発火 (外部プラグインによる追加バリデーション)
4. 外部プラグインがキャンセルしなければ、`INominationEventListener.OnNomination` イベントが発火 (最終キャンセルポイント)
5. 全て通過 → ノミネーション成功、`OnNominationChanged` イベント発火
6. いずれかの段階で拒否 → `NominationCheckResult` のリストが返る

管理者ノミネーションでは手順 2 が `CanAdminNominateMap` に置き換わり、プレイヤー数・曜日・時間帯・権限のチェックがバイパスされます。

---

## INominationValidateService

個々のバリデーション条件を公開するサービスです。複合チェックメソッドと個別チェックメソッドの両方を備えています。

### 複合チェックメソッド

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `PlayerCanNominateMap(IGameClient, IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | プレイヤーがそのマップをノミネーションできるか全条件をチェックする。空リスト = 許可 |
| `CanPickupMap(IMapConfig)` | `IReadOnlyList<NominationCheckResult>` | ランダム選択用のチェック。権限などプレイヤー依存のチェックは省略される。空リスト = 選択可 |
| `CanAdminNominateMap(IMapConfig, IGameClient?)` | `IReadOnlyList<NominationCheckResult>` | 管理者ノミネーション用のチェック。`nominator` が `null` ならコンソール実行扱い。空リスト = 許可 |
| `GetNominationState(IMapConfig, IGameClient?)` | `IReadOnlyList<NominationCheckResult>` | 指定マップのノミネーション状態を返す。`AlreadyNominated` / `NominatedByAdmin` / 空リスト (未ノミネーション) のいずれか |

### 個別チェックメソッド

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `IsDuringVotingPeriod()` | `bool` | 投票が進行中かどうか |
| `IsMapDisabled(IMapConfig)` | `bool` | マップが無効化されているか |
| `IsCurrentMap(IMapConfig)` | `bool` | 現在プレイ中のマップか |
| `IsWithinTimeRange(IMapConfig)` | `bool` | 現在の時刻がマップの許可時間帯内か |
| `IsWithinAllowedDays(IMapConfig)` | `bool` | 今日がマップの許可曜日に含まれるか |
| `IsGreaterThanMinPlayers(IMapConfig)` | `bool` | 現在のプレイヤー数が最小人数以上か |
| `IsLowerThanMaxPlayers(IMapConfig)` | `bool` | 現在のプレイヤー数が最大人数以下か |
| `IsMapInCooldown(IMapConfig)` | `bool` | マップがクールダウン中か |
| `SharesCooldownGroupWithCurrentMap(IMapConfig)` | `bool` | 現在プレイ中のマップと、クールダウン設定 (`Cooldown` > 0 または `CooldownDateTime`) のあるグループを共有しているか。現在マップ自身に対しては `false` (`IsCurrentMap` の担当) |
| `IsMapInNominationCooldown(IMapConfig)` | `bool` | マップがノミネーション専用クールダウン中か |
| `IsPlayerInNominationCooldown(ulong)` | `bool` | プレイヤー (SteamID) がプレイヤー単位のノミネーションクールダウン中か |
| `GetPlayerCooldownState(ulong)` | `IPlayerNominationCooldownState?` | プレイヤーのノミネーションクールダウン状態を返す。クールダウン中でなければ `null` |
| `HasReachedGroupNominationLimit(IMapConfig)` | `bool` | マップの所属グループがノミネーション上限に達しているか |
| `HasBypassPermission(IMapConfig, IGameClient)` | `bool` | 全ノミネーションチェックをスキップするバイパス権限を持っているか (exact match) |
| `IsPlayerAllowedByPermission(IMapConfig, IGameClient)` | `bool` | 制限付きマップへの allow 権限を持っているか (ワイルドカード対応) |
| `IsPlayerDeniedByPermission(IMapConfig, IGameClient)` | `bool` | 権限ノードにより拒否されているか (exact match)。解決順: Deny > Allow > Default (許可) |
| `GetCooldownInformations(IMapConfig)` | `IDetailedCooldownResult` | マップの詳細なクールダウン状態を取得する |

---

## INominationMenuManagementService

ゲーム内のノミネーションメニュー表示を管理するサービスです。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `ShowNominationMenu(IGameClient, List<IMapConfig>)` | `void` | 指定したマップリストでノミネーションメニューを表示する |
| `ShowNominationMenu(IGameClient)` | `void` | 全マップを対象にノミネーションメニューを表示する |
| `ShowAdminNominationMenu(IGameClient, List<IMapConfig>)` | `void` | 指定したマップリストで管理者用ノミネーションメニューを表示する |
| `ShowAdminNominationMenu(IGameClient)` | `void` | 全マップを対象に管理者用ノミネーションメニューを表示する |
| `ShowRemoveNominationMenu(IGameClient, List<IMcsNominationData>)` | `void` | 指定したノミネーションの削除メニューを表示する |
| `ShowRemoveNominationMenu(IGameClient)` | `void` | 全ノミネーションの削除メニューを表示する |
| `NominateOrConfirm(IGameClient, IMapConfig, bool)` | `void` | マップをノミネートする。非管理者のノミネーションは常に確認メニューを先に表示する。`isAdmin` が `true` の場合は管理者ノミネーションとして即時実行する |

---

## INominationManager

現在のノミネーション状態を読み取るマネージャーです。

| プロパティ | 型 | 説明 |
|---|---|---|
| `NominatedMaps` | `IReadOnlyDictionary<string, IMcsNominationData>` | ノミネーション済みマップの辞書。キーはマップ名 |

---

## IMcsNominationData

個々のノミネーションエントリのデータを表します。

| プロパティ | 型 | 説明 |
|---|---|---|
| `MapConfig` | `IMapConfig` | ノミネーションされたマップの設定 |
| `NominationParticipants` | `IReadOnlySet<int>` | ノミネーションに参加しているプレイヤーの UserID セット |
| `IsForceNominated` | `bool` | 管理者により強制ノミネーションされたかどうか |

同じマップを複数のプレイヤーがノミネーションした場合、`NominationParticipants` に全員が追加されます。管理者ノミネーションの場合は `IsForceNominated` が `true` になり、参加者が全員抜けてもノミネーションが残ります。

---

## IDetailedCooldownResult

マップに適用されているクールダウンの詳細な内訳を提供します。マップ本体のクールダウンとグループ経由のクールダウンの両方を確認できます。

MCS のクールダウンには 2 つの軸があります:

- **回数ベース (Count)**: マップがプレイされるたびに設定値がセットされ、他のマップがプレイされるたびに 1 ずつ減少するカウンタ
- **時限ベース (Timed)**: マップがプレイされた時刻から指定期間が経過するまでクールダウン状態を維持する

マップが複数のグループに所属している場合、グループごとに独立したクールダウンが適用されます。`HighestCooldownCount` / `LongestTimedCooldown` は、マップ本体とグループの全クールダウンの中から最も制限的な値を返します。

| プロパティ | 型 | 説明 |
|---|---|---|
| `HasCooldown` | `bool` | いずれかのクールダウンが適用中の場合 `true` |
| `HighestCooldownCount` | `int` | マップ本体・全グループを通じて最も大きい回数クールダウン |
| `LongestTimedCooldown` | `DateTime` | マップ本体・全グループを通じて最も遅い時限クールダウン終了時刻 (UTC) |
| `MapConfig` | `IMapConfig` | 対象マップの設定。デフォルトのクールダウン値の参照に使用できる |
| `CooldownCount` | `int` | マップ本体に適用中の回数クールダウン |
| `TimedCooldown` | `DateTime` | マップ本体に適用中の時限クールダウン終了時刻 (UTC) |
| `GroupCooldowns` | `IReadOnlyDictionary<string, int>` | グループ名をキーとした、各グループの回数クールダウン |
| `GroupTimedCooldowns` | `IReadOnlyDictionary<string, DateTime>` | グループ名をキーとした、各グループの時限クールダウン終了時刻 (UTC) |

---

## NominationCheckResult

ノミネーションが拒否された理由を表す列挙型です。`TryNominateMap` 等のメソッドは、成功時に空リストを返し、失敗時に該当する値を全て含むリストを返します。

| 値 | 説明 |
|---|---|
| `Disabled` | マップが無効化されている (`IsDisabled = true`) |
| `NotEnoughPermissions` | 権限ノードにより拒否されている |
| `TooMuchPlayers` | サーバーのプレイヤー数が `MaxPlayers` を超えている |
| `NotEnoughPlayers` | サーバーのプレイヤー数が `MinPlayers` 未満 |
| `VotingPeriod` | 投票が進行中のためノミネーション不可 |
| `OnlySpecificDay` | 今日はこのマップのノミネーション許可曜日ではない |
| `OnlySpecificTime` | 現在時刻はこのマップのノミネーション許可時間帯外 |
| `MapIsInCooldown` | マップまたは所属グループがクールダウン中 |
| `NominationCooldownActive` | ノミネーション専用クールダウンが有効 (投票候補として消費された直後に付与される) |
| `AlreadyNominated` | 既にノミネーションされている |
| `NominatedByAdmin` | 管理者により強制ノミネーション済み (通常プレイヤーでは上書き不可) |
| `SameMap` | 現在プレイ中のマップと同じ |
| `GroupNominationLimitReached` | マップの所属グループのノミネーション上限に到達している |
| `CancelledByExternalPlugin` | 外部プラグインのイベントリスナーによりキャンセルされた |
| `ProhibitAdminNomination` | マップ設定で管理者ノミネーションが禁止されている |
| `PlayerCooldownActive` | プレイヤーがプレイヤー単位のノミネーションクールダウン中 |
| `SameMapGroup` | 現在プレイ中のマップとクールダウン設定のあるグループを共有している |

---

## IPlayerNominationCooldownState

プレイヤー単位のノミネーションクールダウン状態です。

| プロパティ | 型 | 説明 |
|---|---|---|
| `RemainingCount` | `int` | 残りクールダウンカウント |
| `CooldownUntil` | `DateTime` | クールダウン終了時刻 |

---

## NominationSortOrder

ノミネーションメニュー等でのマップ一覧の並び順を指定する列挙型です。

| 値 | 説明 |
|---|---|
| `AlphabeticalAscending` | マップ名昇順 (A → Z) |
| `AlphabeticalDescending` | マップ名降順 (Z → A) |
| `CooldownAscending` | 回数クールダウン昇順 (残り少ない順) |
| `CooldownDescending` | 回数クールダウン降順 (残り多い順) |
| `TimedCooldownAscending` | 時限クールダウン昇順 (終了が近い順) |
| `TimedCooldownDescending` | 時限クールダウン降順 (終了が遠い順) |
| `TagAscending` | 最小サーチタグ昇順 (A → Z)。タグなしマップは末尾 |
| `TagDescending` | 最小サーチタグ降順 (Z → A)。タグなしマップは末尾 |

---

## McsMapListSorter

マップ一覧を `NominationSortOrder` でソートする静的ヘルパーです。MCS コアがノミネーションメニュー構築時の
デフォルト順 (マップ名昇順) の適用に使用するほか、メニュー互換プラグインがプレイヤー選択のソート順を
適用する際にも利用できます。

**名前空間**: `MapChooserSharpMS.Shared.Nomination`

```csharp
public static List<T> Sort<T>(
    IEnumerable<T> source,
    Func<T, IMapConfig> configSelector,
    NominationSortOrder order,
    IMapCooldownQueryService cooldownQueryService)
```

- ソート済みの新しいリストを返します。元のリストは変更しません。
- マップ名以外のソートでは、同値時にマップ名昇順でタイブレークします。
- クールダウン系ソートは `IMapCooldownQueryService.GetCurrentCooldowns()` (インメモリのみ) を使用します。
- タグ系ソートは各マップの alphabetical 最小タグをキーにします。タグなしマップは常に末尾です。
- 文字列比較は ordinal の大文字小文字無視です。

---

## UnNominateReason

プレイヤーのノミネーション参加が解除された理由を表す列挙型です。`TryUnNominate` の引数として渡され、`OnUnNominate` イベントのパラメータに含まれます。

| 値 | 説明 |
|---|---|
| `Normally` | プレイヤーの自発的な解除。別のマップをノミネーションした場合や、明示的にノミネーションを取り消した場合 |
| `PlayerDisconnect` | プレイヤーがサーバーから切断したことによる自動解除 |

---

## イベントリスナー (INominationEventListener)

`IMcsNominationController.InstallEventListener` で登録するリスナーインターフェースです。全メソッドにデフォルト実装があるため、必要なイベントだけをオーバーライドすれば十分です。

| メソッド | 戻り値 | 説明 |
|---|---|---|
| `OnNominationCheckPassed(INominationCheckPassedEventParams)` | `McsCancellableEvent` | バリデーション通過後に発火。`Stop` を返すとノミネーションを拒否する (外部プラグインによる追加バリデーション用) |
| `OnNomination(INominationParams)` | `McsCancellableEvent` | 通常ノミネーション直前に発火。`Stop` を返すとキャンセル |
| `OnAdminNomination(IAdminNominationParams)` | `McsCancellableEvent` | 管理者ノミネーション直前に発火。`Stop` を返すとキャンセル |
| `OnNominationChanged(INominationChangeParams)` | `void` | ノミネーション状態に変更があったとき (追加・参加者変更) に発火 |
| `OnNominationRemoved(INominationRemovedParams)` | `void` | ノミネーションが削除されたときに発火 |
| `OnUnNominate(IUnNominateParams)` | `void` | プレイヤーのノミネーション参加が解除されたときに発火。参加者ごとに個別に呼ばれる |
| `OnNominationMenuDetailsOpening(INominationMenuDetailsOpeningParams)` | `void` | ノミネーション詳細メニューが開かれる直前に発火。`ExtraItems` に `McsMenuItem` を追加できる |
