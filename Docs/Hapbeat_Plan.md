# Hapbeat 振動導入 計画書

『じいや！巨大ロボで紅茶を注ぎなさい！』に Hapbeat（胸装着）の振動を入れるための計画。
HapbeatTest で「本当に使えるか」を確かめ、判断材料が揃ってから GiantRobotTeaParty（以下 GRTP）へ持ち込む。

---

## 0. 前提

### 確定している前提

| 項目 | 内容 |
|---|---|
| 本番環境 | Quest 単体 / Unity 6000.3.22f1 / URP / Meta XR SDK 205 / Input System 1.20 |
| 実機 | 未開封・未セットアップ（ファーム書き込みと Wi-Fi 設定から始める） |
| HapbeatTest の検証範囲 | Unity Editor（PC）→ Hapbeat まで。Quest 実機は GRTP 側で確認する |
| 振動の役割 | 臨場感の演出。ロボ登場 / 紅茶をこぼした瞬間 / こぼれた紅茶で街が流れていく間 |
| GRTP の扱い | 現在 main のため読み取りのみ。作業開始時にブランチを切ってもらう |
| Git 操作 | 本人が行う |

### 使うもの

| ツール | 役割 | 備考 |
|---|---|---|
| Hapbeat Studio（https://devtools.hapbeat.com） | Kit（振動クリップ集）の作成・デバイスへの Deploy、ファーム書き込み、Wi-Fi 設定、再生テスト | ブラウザアプリ。USB 書き込みは Chrome / Edge 必須 |
| hapbeat-helper | Studio とデバイスの橋渡し（mDNS / UDP 7700 / TCP 7701）。Studio とは `ws://localhost:7703` で接続 | pipx で入れる CLI デーモン。0.1.3 以上 |
| Hapbeat Unity SDK（`com.hapbeat.sdk`） | Unity からの再生（PLAY / STOP / Stream） | 最新は 0.5.0。Unity 6000.0 以上 |

### 仕組みの要点（計画の判断に効くところだけ）

- 振動は **event id**（`<kit名>.<clip名>`）で呼ぶ。Unity 側は「いつ鳴らすか」だけを持ち、「何をどの強さで」は EventMap アセットと Kit 側に寄せる。
- 再生方式は2つ。
  - **Command**：Studio で Deploy 済みのクリップを id で鳴らす。通信量が少なく遅延も小さい。Kit の Deploy が前提。
  - **StreamClip**：Unity の AudioClip を PCM で逐次送る。Deploy 不要で、再生中に gain / pan を毎フレーム変えられる。PONG で相手が見つかるまでは送信しない（`Deferred`）。
  - SDK の推奨は「試作は Stream、本番は Command」。
- UDP なので到達保証はない。ループ再生中の STOP が落ちると鳴りっぱなしになり得る。
- デバイスのアドレスは `player_<N>/<部位>/group_<M>`、初期値は `player_1 / group_1`。

---

## 1. 全体像

```
Phase A: HapbeatTest（Editor 検証）
  M0 実機が Studio から震える
  M1 Unity Editor から震える
  M2 本番の3シーンを想定したパターンが成立する
  M3 決定事項をまとめて GRTP へ渡す
        │  ← ここでブランチを切ってもらう
Phase B: GRTP（本番組み込み）
  M4 SDK と振動アセットの受け皿を入れる（チームに影響を出さない）
  M5 Quest 実機から震える
  M6 自分のシーンで3演出を再現
  M7 本番の各演出に接続
```

M5（Quest 実機）を GRTP の早い段階に置いているのは意図的。HapbeatTest を Editor のみにした分、Quest → Hapbeat の疎通が最大の未確認点になるため、演出を作り込む前に潰す。

---

## 2. Phase A：HapbeatTest

### M0. 実機が Studio から震える

Unity に触る前に、デバイス・ネットワーク・ツールの問題をここで切り分けておく。

| # | 小ゴール | 完了条件 |
|---|---|---|
| 0-1 | hapbeat-helper を入れる | `pipx install hapbeat-helper` → `hapbeat-helper version` が 0.1.3 以上。自動起動にするなら `install-service`、手動なら `start` |
| 0-2 | Studio と Helper が繋がる | Chrome で Studio を開き、ヘッダが「Helper 接続中」 |
| 0-3 | 初回セットアップ | Manage → Onboarding で USB 接続 → ファーム書き込み → Wi-Fi 設定。PC と同じネットワークに参加させる |
| 0-4 | デバイスが見える | Manage の一覧に出る。`get_info` の board とファームのバージョンを控える |
| 0-5 | 名前とアドレスを決める | 名前を付け、アドレスを `player_1/pos_chest/group_2` にする（group を 2 にしておくと未設定機と区別できる） |
| 0-6 | テスト用 Kit を作る | Kit タブで WAV を読み込み、Kit 名 `hbtest` で下記3種を作る。Save Folder の保存先は `HapbeatTest/HapticKits/hbtest/` |
| 0-7 | Deploy して鳴らす | Deploy → Manage の PLAY / STOP で3種とも体感できる |

テスト用クリップ（本番3シーンの代役）：

| event id | 想定する本番演出 | 素材の性格 |
|---|---|---|
| `hbtest.impact` | ロボ登場（着地の衝撃） | 短く重い一発 |
| `hbtest.splash` | 紅茶をこぼした瞬間 | 短く軽いバシャッ |
| `hbtest.rumble` | 街が流されていく間 | ループ前提の低い持続音 |

つまずきやすい点：

- `pipx install` が `WinError 448` で失敗する → ホームが OneDrive 同期下。`PIPX_HOME` / `PIPX_BIN_DIR` を同期外に向ける。
- 一覧に出ない → 同一ネットワークか確認。ゲスト Wi-Fi やテザリングはブロードキャスト / mDNS を通さないことがある。VPN・Hyper-V 等で NIC が複数ある PC は送信 NIC がずれることがある。
- Deploy / OTA で board 不一致の警告 → ファームの種類が機体と合っていない。

### M1. Unity Editor から震える

| # | 小ゴール | 完了条件 |
|---|---|---|
| 1-1 | Unity プロジェクトを作る | HapbeatTest 直下を Unity 6000.3.22f1 のプロジェクトにする（下記の手順）。テンプレートは Universal 3D（Input System 入り。SDK の Runtime asmdef が Input System を参照するため） |
| 1-2 | SDK を入れる | Package Manager → Add package from git URL → `https://github.com/Hapbeat/hapbeat-unity-sdk.git`。入った版と `packages-lock.json` のコミットハッシュを控える |
| 1-3 | シーン初期化 | `Hapbeat > Initial Scene Setup`。Config を作り `appName` を `HBTEST`（16文字以内、OLED に出る） |
| 1-4 | Edit モードで鳴らす | HapbeatManager の Inspector で Discover → `AliveDeviceCount` が 1 → Play ボタンで `hbtest.impact` が鳴る |
| 1-5 | Play モードでコードから鳴らす | キー入力で `HapbeatManager.Instance.Play("hbtest.impact")` が鳴る（または BasicExample サンプルで代用） |

1-1 の手順：Unity Hub は空でないフォルダに新規作成できないので、別の場所（例：`C:\emuto_Project\tmp\HapbeatTest`）に作ってから `Assets` / `Packages` / `ProjectSettings` を HapbeatTest 直下へ移し、Hub で「ディスクから追加」する。移したあと `.gitignore` が `Library/` `Temp/` `Logs/` `UserSettings/` を除外しているか確認する。

SDK のバージョン固定：git URL のままだと、あとから開いたメンバーの環境で別の版が入る可能性がある。M3 で決めた版を `manifest.json` の URL 末尾に `#<タグまたはコミットハッシュ>` として固定し、GRTP でも同じものを使う。

### M2. 本番の3シーンを想定したパターンが成立する

ここが HapbeatTest の本題。「GRTP で迷わないための判断材料」を揃える。

| # | 検証すること | 完了条件 | 本番での対応 |
|---|---|---|---|
| 2-1 | EventMap 経由の一発再生 | EventMap（`Assets > Create > Hapbeat > Event Map`）に3種を登録。UnityEvent Trigger の `Fire()` をボタン等から呼んで鳴る。gain は EventMap 側だけで変えられる | ロボ登場 |
| 2-2 | 強さを変えた一発再生 | `FireWithGain()` で強弱が体感で区別できる | こぼした量で強さを変える場合 |
| 2-3 | 持続振動の開始・強さ変化・停止 | 下の「持続振動の方式比較」を両方試し、どちらで行くか決める | 街が流されていく間 |
| 2-4 | 取り残し防止 | ループ中に Play モードを止める / シーンを切り替える / オブジェクトを破棄する。それぞれで振動が止まるか | 全般 |
| 2-5 | デバイスが無い状態 | 電源 OFF で Play。例外・エラーログ・目立つフレーム落ちが出ない | 実機を持っていないチームメンバーの環境 |
| 2-6 | Helper との同居 | Helper を起動したまま Unity から鳴るか。鳴らなければ Helper 停止で鳴るか | 開発中の運用ルール |
| 2-7 | 音との同期 | SE と同時に鳴らし、ズレが気になるなら Config の `hapticDelaySeconds` で合わせる | ロボ登場（SE と揃える） |
| 2-8 | 宛先の絞り込み | EventMap の target を `""`（全体）と `*/pos_chest` の両方で鳴ることを確認 | 展示会場で他の Hapbeat と混線しないように |

持続振動の方式比較（2-3）：

| | A. Command ループ | B. StreamClip |
|---|---|---|
| やり方 | Kit にループ用クリップを Deploy し、開始で Play・終了で Stop | AudioClip を `StreamAudioClip(loop: true)` で流し、`Gain` を毎フレーム更新 |
| 強さの変化 | 基本は固定（途中で変えられるかは要検証） | 連続的に変えられる（流される勢いに合わせられる） |
| 通信負荷 | 小さい | 常時 PCM を送る。Quest 単体 + 会場 Wi-Fi では負荷とドロップのリスクがある |
| 確認すること | ループ指定で本当にループするか。Stop が確実に効くか | Quest 想定で途切れないか（M5 で再確認）。停止時に残らないか |

判断基準：「強さが一定の揺れで演出として足りるなら A」。足りない場合だけ B に進む。

2-4 で振動が残る場合に限り、GRTP 側に「破棄時・終了時に止める」小さなコンポーネントを1つ用意する（SDK の `HapbeatActionHelper` の `StopAll` を `OnDisable` 等から呼ぶ形で足りるならそれで済ませ、自作はしない）。

### M3. 決定事項をまとめて GRTP へ渡す

HapbeatTest に `Docs/Hapbeat_Decisions.md` を作り、以下を埋める。これが Phase B の入力になる。

| 決める項目 | 例 |
|---|---|
| SDK の固定版 | `#v0.5.0` またはコミットハッシュ |
| ファームのバージョン / board | 0-4 で控えた値 |
| 持続振動の方式 | A or B と理由 |
| 取り残し対策の要否 | 2-4 の結果 |
| Helper 同居の運用 | 2-6 の結果（「Unity 検証中は Helper を止める」など） |
| `hapticDelaySeconds` | 2-7 で合わせた値 |
| 本番の event id | `grtp.robot_appear` / `grtp.tea_spill` / `grtp.city_flood` |
| 本番の target | `*/pos_chest` など |

Phase A の完了条件：M3 の表がすべて埋まっていること。

---

## 3. Phase B：GRTP

ブランチを切ってもらってから着手。1 マイルストーンを 1 PR 程度の粒度にする。

### M4. SDK と振動アセットの受け皿を入れる

チーム全員の環境に入る変更なので、「実機を持っていない人に何も起きない」ことを最優先にする。

| # | 小ゴール | 完了条件 |
|---|---|---|
| 4-1 | SDK 追加 | `Packages/manifest.json` に M3 で決めた固定版を追加。既存パッケージとコンパイルエラーが出ない |
| 4-2 | 置き場所 | `Assets/GRTP/Haptics/` に Config と EventMap（`GRTP_Haptics.asset`）を置く |
| 4-3 | Kit の置き場所 | Studio の Kit フォルダはリポジトリ直下 `Haptics/Kits/grtp/`（Assets の外）。Command 方式なら WAV を Unity に取り込む必要がないため |
| 4-4 | Router の Prefab 化 | Event Router を `Assets/GRTP/Prefabs/Manager/` に Prefab として置く。シーンへの配置は M6 で自分のシーンから |

B（StreamClip）を採用した場合だけ、持続振動用の AudioClip を `Assets/GRTP/Haptics/Clips/` に置く。

### M5. Quest 実機から震える

HapbeatTest で扱わなかった部分。演出を作る前にここを通す。

| # | 小ゴール | 完了条件 |
|---|---|---|
| 5-1 | 最小シーンでビルド | `Scenes/emuto/` に Router と `grtp.robot_appear` を鳴らすだけのシーンを作り、Quest にビルドして鳴る |
| 5-2 | 持続振動の再確認 | M3 で決めた方式が Quest から途切れず鳴り、止まる |
| 5-3 | 展示ネットワークの想定 | Quest と Hapbeat を同じルーターに繋ぐ構成で動くこと。会場 Wi-Fi に頼らず持ち込みルーターを使う前提をチームに共有 |

鳴らない場合の確認順：同一ネットワークか → `AliveDeviceCount`（`HapbeatStatusOverlay` を一時的に出す）→ target を `""` にして鳴るか → Kit に event id が入っているか。

### M6. 自分のシーンで3演出を再現

`Assets/GRTP/Scenes/emuto/Haptics_emuto.unity` を作り、本番の演出を仮のきっかけ（キー入力や単純なアニメーション）で再現する。強さ・長さの調整はここで EventMap と Kit 側だけで行う。

| 演出 | event id | 鳴らし方 |
|---|---|---|
| ロボ登場 | `grtp.robot_appear` | Timeline / Animator の登場タイミングから UnityEvent Trigger の `Fire()`（Animator なら `HapbeatStateBehaviour`） |
| 紅茶をこぼした瞬間 | `grtp.tea_spill` | こぼれ判定のタイミングで `Fire()`（量で強さを変えるなら `FireWithGain()`） |
| 街が流されていく間 | `grtp.city_flood` | 開始で再生、終了で停止（方式は M3 の決定に従う） |

完了条件：3演出がこのシーンで体感でき、ゲーム側のスクリプトに event id や gain の直書きがないこと。

### M7. 本番の各演出に接続

演出ごとに 1 PR。既存の処理には手を入れず、既にあるイベント（UnityEvent / Timeline / Animator）に Trigger をぶら下げる形を優先する。フックが無い場合は、その機能の担当者と相談してフックを足してもらう。

| 演出 | 接続先 | 着手前に確認すること |
|---|---|---|
| ロボ登場 | ロボ登場演出（Timeline / Animator） | 担当者と実装方式 |
| 紅茶をこぼした瞬間 | #9 で用意したこぼれのフック | フックの呼ばれ方（1回か、粒ごとか） |
| 街が流されていく間 | 街が流れる演出 | 実装の有無・担当者・開始と終了の取り方 |

SE の再生（AudioManager_KanKikuchi）と同じ箇所から振動を鳴らすと同期を取りやすいが、AudioManager 本体は改造しない。

最終ゴール：本番シーンで3演出の振動が Quest から鳴り、Hapbeat を付けていない環境でも挙動が変わらないこと。

---

## 4. リスクと対策

| リスク | 影響 | 対策 |
|---|---|---|
| Quest → Hapbeat の疎通が未確認 | 演出を作り込んでから鳴らないと判明する | M5 を M6 より前に置く |
| 会場 Wi-Fi がブロードキャストを通さない / 混雑 | 本番で鳴らない・遅れる | 持ち込みルーター前提。持続振動は Command を優先 |
| ループの STOP 取りこぼし | 振動が鳴りっぱなし | 2-4 で確認し、必要な場合だけ停止処理を足す |
| 他の出展者の Hapbeat と混線 | 他ブースで鳴る / こちらで鳴る | group を 2 以上に設定し、target を絞る（2-8） |
| SDK の更新で挙動が変わる | メンバー間で差が出る | manifest で版を固定。更新時は `Assets/Samples/Hapbeat SDK/<旧版>/` を削除してから再インポート |
| Helper と Unity のポート競合 | 開発中に鳴らない | 2-6 で確認し、運用ルールを M3 に記録 |

---

## 5. 未確定事項（着手時に確認）

- ロボ登場・街が流れる演出の担当者と実装方式（M7 の前まで）
- 展示会場のネットワーク条件（M5 の前まで）
- 展示で Hapbeat を複数台使うか（使うなら Address Override を検討。1台なら不要）

## 参照

- Hapbeat Studio AGENTS.md：https://raw.githubusercontent.com/hapbeat/hapbeat-studio/master/AGENTS.md
- hapbeat-helper AGENTS.md：https://raw.githubusercontent.com/hapbeat/hapbeat-helper/master/AGENTS.md
- Hapbeat Unity SDK AGENTS.md：https://raw.githubusercontent.com/hapbeat/hapbeat-unity-sdk/master/AGENTS.md
- 公式ドキュメント：https://devtools.hapbeat.com/
