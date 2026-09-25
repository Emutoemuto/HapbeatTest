# Hapbeat 振動 組み込み手順書（GRTP 用）

『じいや！巨大ロボで紅茶を注ぎなさい！』に、胸に着ける振動デバイス Hapbeat の演出を入れるための手順書です。
上から順に進めれば、Unity に慣れていなくても組み込めるように書いています。分からないところがあれば Emuto に聞いてください。

---

## 0. これで何ができるか

次の 3 つの場面で、プレイヤーの胸が震えるようになります。

| 場面 | event id | 鳴り方 |
|---|---|---|
| ロボットが登場したとき | `grtp.robot_appear` | 一発だけ鳴る |
| 紅茶をこぼした瞬間 | `grtp.tea_spill` | 一発だけ鳴る |
| こぼれた紅茶で街が流されていく間 | `grtp.city_flood` | **開始で鳴らし始め、終了で止める**（止めないと最後まで鳴り続ける） |

**event id** は振動に付けた名前です。Unity 側はこの名前で「どの振動を鳴らすか」を指定します。

---

## 1. 仕組み（ここだけは読んでください）

```
Unity（ゲーム） ──Wi-Fi──▶ Hapbeat（胸のデバイス）
  「grtp.robot_appear を鳴らして」     デバイスの中に入っている振動データを再生する
```

- 振動データ（Kit）は、あらかじめ **Hapbeat Studio** というブラウザのツールでデバイスに書き込んでおきます（Deploy）。
- Unity は「この名前の振動を鳴らして」と送るだけです。
- **Unity 側は、鳴らなくてもエラーを出しません。** 鳴らないときは「7. 鳴らないとき」の順に確認してください。

---

## 2. 受け取るもの

| ファイル | 中身 |
|---|---|
| `GRTP_Haptics.unitypackage` | Unity に入れるもの一式（下の表） |
| `HapticKits.zip` | 振動データ（Kit）。解凍してできる `HapticKits` フォルダを、Studio で開いてデバイスに書き込む |
| この手順書 | |

`GRTP_Haptics.unitypackage` の中身：

```
Assets/GRTP/Haptics/
├─ Prefabs/HapbeatRouter.prefab     … 振動を送る本体。シーンに 1 つだけ置く
├─ EventMaps/GRTP_Haptics.asset     … 3 つの振動の一覧と強さの設定
└─ Resources/HapbeatConfig.asset    … 接続設定
```

---

## 3. Unity への導入（最初に 1 回だけ）

### 3-1. SDK を入れる

1. GRTP のプロジェクトを閉じた状態で、`Packages/manifest.json` をテキストエディタで開きます。
2. `"dependencies": {` のすぐ下の行に、次の 1 行を追加して保存します（行末のカンマを忘れずに）。

   ```json
   "com.hapbeat.sdk": "https://github.com/Hapbeat/hapbeat-unity-sdk.git#a74107150920330fec6077ecdf05c9f6da02d6ed",
   ```

3. Unity でプロジェクトを開きます。読み込みが終わり、上のメニューに **「Hapbeat」** が増えていれば成功です。

> 末尾の `#a741...` は SDK の版を固定するための番号です。消したり変えたりしないでください。メンバーごとに違う版が入るのを防いでいます。

### 3-2. 受け取ったアセットを入れる

1. Unity のメニューから **Assets → Import Package → Custom Package...** を選びます。
2. `GRTP_Haptics.unitypackage` を選び、全部にチェックが入った状態で **Import** を押します。
3. Project ウィンドウに `Assets/GRTP/Haptics/` ができていれば成功です。

### 3-3. シーンに Router を置く

1. **ゲームで最初に読み込まれるシーン**を開きます。
2. `Assets/GRTP/Haptics/Prefabs/HapbeatRouter.prefab` を Hierarchy に**ドラッグ**します。
3. シーンを保存します。

Router は一度置けば、シーンを切り替えても自動で残り続けます。**他のシーンには置かないでください。**

---

## 4. まず鳴らしてみる（1 分で確認）

デバイスの準備（「6. デバイスの準備」）が済んでいることが前提です。

1. Hapbeat の電源を入れ、PC と同じ Wi-Fi（`13LABO`）につながっていることを確認します。
2. Unity で Hierarchy の `HapbeatRouter` を選びます。
3. Inspector の **Connect** → **Discover** を押し、3 秒ほど待ちます。
4. **Hapbeat → Open Event Map** を開き、`GRTP_Haptics` のエントリを選んで **▶ Test Play** を押します。

胸が震えれば準備完了です。

---

## 5. ゲームに組み込む

### 5-1. 基本：Trigger を付けて呼ぶ（コード不要）

1. 振動を鳴らしたい演出の GameObject、または振動専用の空の GameObject を用意します。
2. **Add Component → Hapbeat → Hapbeat UnityEvent Trigger** を付けます。
3. Inspector で次の 2 つを設定します。
   - **Event Map**：`GRTP_Haptics`
   - **Event**：鳴らしたい振動（例：`ロボ登場`）
4. 演出側の UnityEvent（Timeline の Signal、Animation Event、ボタンの OnClick など）から、この Trigger の関数を呼びます。

| 呼ぶ関数 | 使う場面 |
|---|---|
| `Fire()` | 鳴らす |
| `FireWithGain(float)` | 強さを変えて鳴らす（1 が標準、0.3 なら弱め） |
| `Stop()` | 止める（`city_flood` の終了時に必ず呼ぶ） |

### 5-2. スクリプトから呼ぶ場合

Trigger を参照して、関数を呼ぶだけです。event id や強さはコードに書きません。

```csharp
using Hapbeat;
using UnityEngine;

public sealed class CityFloodDirector : MonoBehaviour
{
    [SerializeField] private HapbeatUnityEventTrigger _floodHaptics;

    public void BeginFlood()
    {
        // 演出の開始処理 …
        _floodHaptics.Fire();
    }

    public void EndFlood()
    {
        // 演出の終了処理 …
        _floodHaptics.Stop();
    }

    private void OnDisable()
    {
        // シーン切り替えや破棄で EndFlood が呼ばれなかった場合の保険
        if (_floodHaptics != null) _floodHaptics.Stop();
    }
}
```

`HapbeatManager.Instance.Play("grtp.xxx")` のように直接呼ぶこともできますが、使わないでください。強さの調整が EventMap に集まらなくなり、どこで鳴らしているかも追えなくなります。

### 5-3. 演出ごとの組み込み方

| 場面 | Trigger の Event | 呼び方 |
|---|---|---|
| ロボ登場 | `grtp.robot_appear` | 登場の瞬間（着地など）に `Fire()`。SE と同じタイミングで呼ぶ |
| 紅茶をこぼした | `grtp.tea_spill` | こぼれ判定が出た瞬間に `Fire()`。粒ごとに呼ぶと鳴りすぎるので、こぼれ 1 回につき 1 回 |
| 街が流される | `grtp.city_flood` | 開始で `Fire()`、**終了で `Stop()`**。上のコード例のように `OnDisable` でも `Stop()` を呼ぶ |

### 5-4. 強さを変えたいとき

コードは触らず、**Hapbeat → Open Event Map** で該当エントリの **Gain** を変えて **Save** します（1 が標準、0〜2）。

---

## 6. デバイスの準備（Hapbeat Studio / Helper）

新しい PC で作業するとき、デバイスを初期化したとき、振動データを入れ替えたときに行います。

### 6-1. Helper を入れる（PC ごとに 1 回）

Helper は、ブラウザ（Studio）とデバイスの橋渡しをする常駐ソフトです。コマンドプロンプトで実行します。

```cmd
pipx install hapbeat-helper
hapbeat-helper version
```

- `pipx` が無いと言われたら：`py -m pip install --user pipx` → `py -m pipx ensurepath` のあと、コマンドプロンプトを開き直す。
- `pipx needs uv>=0.9.17` と出たら：`uv self update` のあと、もう一度 `pipx install hapbeat-helper`。それでもだめなら `pipx install hapbeat-helper --backend pip`。
- バージョンが **0.1.3 以上**なら OK です（検証時は 0.4.0）。

使うときは毎回、次のコマンドで起動します。ウィンドウは開いたままにしておきます（Ctrl+C で停止）。

```cmd
hapbeat-helper start
```

おすすめ：`winget install ffmpeg` も入れておくと、自作の WAV を使ったときに振動の速さや高さがずれるのを防げます。

### 6-2. Studio を開く

Chrome または Edge で https://studio.hapbeat.com を開き、右上が **「Helper 接続中」** になっていることを確認します。

### 6-3. 初期設定（新品のデバイスのときだけ）

Manage タブ → 初期セットアップの画面に沿って進めます。

1. デバイスを **データ通信対応の USB ケーブル**で PC につなぐ。
2. ファームウェアは **DuoWL → v3 → DuoWL v3 (Wi-Fi UDP) → 最新版** を選んで「Serial 書き込み」。
3. Wi-Fi に **`13LABO`** を登録して再起動する。
4. 設定タブで、名前を `hb-grtp-01`、アドレスを **player `1` / `chest` / group `2`** にする。

左の **Wi-Fi 一覧**にデバイスが出れば完了です。

### 6-4. 振動データを書き込む（Deploy）

1. Studio の Kit タブで **Choose Folder** を押し、受け取った `HapticKits.zip` を解凍し、中の `HapticKits` フォルダを選ぶ。
2. Kits に `grtp` が出るので、3 つのイベント（`robot_appear` / `tea_spill` / `city_flood`）がすべて **FIRE** になっていることを確認する。
3. Manage タブの左の一覧で、**Wi-Fi 側のデバイスにチェック**が入っていることを確認する（USB 側ではない）。
4. Kit タブに戻って **Deploy** を押し、`192.168.x.x … 100% ok` と出れば完了。

振動の中身を変えたいときは、Studio でクリップを差し替えて **Save Folder → Deploy** します。**event id（名前）を変えなければ、Unity 側は何も変えなくて大丈夫です。**

---

## 7. 鳴らないとき

上から順に確認してください。

| # | 確認すること | 対処 |
|---|---|---|
| 1 | デバイスの電源が入っているか、音量が小さすぎないか | 本体のボタンで音量を上げる |
| 2 | デバイスと PC（本番では Quest）が同じ Wi-Fi にいるか | Studio の Wi-Fi 一覧にデバイスが出るか見る |
| 3 | Studio の Manage → 再生テストの「100 Hz · 1 s」で鳴るか | 鳴らなければデバイスかネットワークの問題。Unity ではない |
| 4 | Kit が Deploy されているか | Manage → Kit タブに `grtp` があるか。無ければ 6-4 |
| 5 | Unity の Router で Discover したとき、デバイスが見つかるか | 見つからなければ 2 に戻る |
| 6 | EventMap の event id が Kit と一致しているか | `grtp.robot_appear` などのつづりを確認 |
| 7 | Console に `[Hapbeat]` のエラーが出ていないか | 内容を Emuto に送る |

Studio で鳴るのに Unity から鳴らない場合は、5〜7 のどれかです。

---

## 8. やってはいけないこと

- **Router を他のコンポーネントと同じ GameObject に付けない。** Router が 2 つあると、SDK は後から来たほうを **GameObject ごと削除**します。同じ GameObject に付けた他のコンポーネントも一緒に消えます。
- **Router を複数のシーンに置かない。** 最初のシーンに 1 つだけ置きます。
- **`HapbeatConfig.asset` を `Resources` フォルダの外に移動しない。** SDK が見つけられなくなり、初期設定で動いてしまいます。
- **`manifest.json` の SDK の行の `#a741...` を消さない。**
- **`city_flood` の `Stop()` を忘れない。** 止めないと、デバイスがクリップの最後まで鳴り続けます。

実機を持っていない人の環境でも、Router は置いたままにしておきます。送信先が見つからないだけでエラーにはならない想定ですが、もし `[Hapbeat]` のエラーが出たら Emuto に知らせてください。

---

## 9. まだ決まっていないこと（あとで調整）

- 振動の中身（今は仮のクリップ）。Studio で差し替えるだけで、Unity 側の変更は不要です。
- SE とのタイミング合わせ（Config の `Haptic Delay`）
- 展示会場で他の Hapbeat と混線しないように、宛先を `pos_chest` / group 2 に絞るかどうか
- Quest 実機からの動作確認（PC の Unity Editor では確認済み）
