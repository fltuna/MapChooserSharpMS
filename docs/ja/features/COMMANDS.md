# コマンド一覧

全コマンドは `ms_` プレフィックス付きで登録されます (例: `ms_nominate`)。
チャットでは `!nominate` / `css_nominate` でも使用可能です。

## マップ名検索

`<map>` 引数を取るコマンド (`!nominate`, `!nominate_addmap`, `!nominate_removemap`, `!map`, `!setnextmap`, `!mapinfo`) は共通の検索動作を持ちます:

1. マップ名の**部分一致** (大文字小文字区別なし)
2. 複数ヒット時は**マップ名の完全一致**を優先
3. 何もヒットしなかった場合は **SearchTag** として検索

1 件ヒットなら即座に実行されます。複数ヒット時は選択メニューが開きます (サーバーコンソールの場合は候補一覧を表示)。`!nominate_removemap` は現在のノミネーションのみを検索するため、SearchTag フォールバックは適用されません。

## プレイヤーコマンド

| コマンド | エイリアス | 説明 |
|---|---|---|
| !nominate \<map\> | !nom | マップをノミネートする。引数なしで全マップ一覧 (Wuling メニューでは先にソート順選択 — Name / Cooldown / Tag の各昇順・降順 — を表示)、部分一致で複数ヒット時はマップ名昇順のメニュー表示 |
| !nomlist | - | 現在のノミネーション一覧を投票の選出順 (管理者ノミネート → ノミネート数降順) で表示。投票枠に入らないエントリはセパレータ行の下に表示される。`!nomlist full` で管理者はノミネーター名表示 |
| !rtv | チャット "rtv" | Rock The Vote に投票する |
| !unnominate | !unnom | 自分のノミネートを取り消す |
| !unrtv | - | RTV 投票を取り消す |
| !ext | - | 現在のマップの延長に投票する (RTV 式カウント) |
| !timeleft | チャット "timeleft" | 残り時間/ラウンドを表示 |
| !nextmap | チャット "nextmap" | 次のマップを表示 |
| !currentmap | チャット "currentmap" | 現在のマップを表示 |
| !mapinfo \<map\> | - | マップ情報を表示。引数なしで現在のマップ。部分一致検索対応 |
| !extends | - | 残りの延長回数を表示 |
| !thetime | チャット "thetime" | 現在のサーバー時刻を表示 |
| !mcs_settings \<subcommand\> [value] | !mcss | 個人設定を変更。`volume` (`vol`) \<0-100\> で投票サウンド音量、`countdown` (`cd`) \<type\> でカウントダウン UI 変更。引数なしで現在設定表示 |

## 管理者コマンド

| コマンド | エイリアス | 権限ノード | 説明 |
|---|---|---|---|
| !nominate_addmap \<map\> | - | mcs.admin.command.nomination.addmap | マップを管理者ノミネーションとして追加。部分一致 / SearchTag 検索対応 (無効マップも対象) |
| !nominate_addwsmap \<workshopId\> | - | mcs.admin.command.nomination.addwsmap | Workshop ID でマップを管理者ノミネーション追加 (config 未登録でも API フェッチ) |
| !nominate_removemap \<map\> | - | mcs.admin.command.nomination.removemap | ノミネーションからマップを削除 |
| !map \<map\> | - | mcs.admin.command.mapcycle.map | マップへ即時変更。部分一致検索対応、複数ヒット時は選択メニュー |
| !wsmap \<workshopId\> | - | mcs.admin.command.mapcycle.wsmap | Workshop ID でマップへ即時変更 (config 未登録でも API フェッチ) |
| !setnextmap \<map\> | - | mcs.admin.command.mapcycle.setnextmap | 次のマップを設定。部分一致検索対応、複数ヒット時は選択メニュー |
| !setnextwsmap \<workshopId\> | - | mcs.admin.command.mapcycle.setnextwsmap | Workshop ID で次のマップを設定 (config 未登録でも API フェッチ) |
| !removenextmap | - | mcs.admin.command.mapcycle.removenextmap | 次のマップ設定を解除 |
| !extend \<amount\> | - | mcs.admin.command.mapcycle.extend | マップの時間/ラウンドを延長 (マイナスで短縮) |
| !voteextend \<minutes\> | !ve | mcs.admin.command.mapcycle.voteextend | 延長投票 (NVM YesNo 投票) を開始 |
| !setmapcooldown \<map\> \<cd\> | !setmapcd | mcs.admin.command.mapcycle.setmapcooldown | マップのクールダウン (回数) を設定 |
| !setgroupcooldown \<group\> \<cd\> | !setgroupcd | mcs.admin.command.mapcycle.setgroupcooldown | グループのクールダウン (回数) を設定 |
| !setmaptcd \<map\> \<duration\> | - | mcs.admin.command.mapcycle.setmaptcd | マップの時限クールダウンを設定 (例: 2h, 3d, 1w) |
| !setgrouptcd \<group\> \<duration\> | - | mcs.admin.command.mapcycle.setgrouptcd | グループの時限クールダウンを設定 (例: 2h, 3d, 1w) |
| !forceresetmcs | - | mcs.admin.command.mapvote.forceresetmcs | MCS の全状態を強制リセット (投票/RTV/ノミネーション) |
| !reloadmapcfgs | - | mcs.admin.command.mapconfig.reloadmapcfgs | マップ config をリロード |
| !forcertv | - | mcs.admin.command.rtv.forcertv | 強制的に RTV を発動 |
| !enablertv | - | mcs.admin.command.rtv.enablertv | RTV を有効化 |
| !disablertv | - | mcs.admin.command.rtv.disablertv | RTV を無効化 |
| !enableext | - | mcs.admin.command.mapcycle.enableext | !ext を有効化 |
| !disableext | - | mcs.admin.command.mapcycle.disableext | !ext を無効化 |
| !setext \<count\> | - | mcs.admin.command.mapcycle.setext | !ext の残り回数を設定 |
| !mcsdebug \<subcommand\> | - | mcs.admin.command.mapcycle.mcsdebug | デバッグコマンド。`config <map>` でマップ config の詳細表示、`state` で MCS 内部状態を表示 |

## 権限ノード (Nomination)

| ノード | 説明 |
|---|---|
| mcs.nominate.map.bypass.\<map\> | 全チェックをバイパスしてノミネートを許可 (exact match) |
| mcs.nominate.group.bypass.\<group\> | 同上 (グループ単位, exact match) |
| mcs.nominate.map.allow.\<map\> | `RestrictToAllowedUsersOnly = true` のマップでノミネートを許可 (ワイルドカード対応) |
| mcs.nominate.group.allow.\<group\> | 同上 (グループ単位) |
| mcs.nominate.map.deny.\<map\> | 特定マップのノミネートを拒否 (exact match) |
| mcs.nominate.group.deny.\<group\> | 特定グループのノミネートを拒否 (exact match) |
| mcs.admin.command.nomination.nomlist.verbose | !nomlist full でノミネーター名表示 |

チェック順: **Bypass (即許可)** → Disabled → Cooldown → Deny → **Allow (config 制限時のみ)** → 通常チェック (Day/Time/Players 等)
