# Hapbeat 決定事項

HapbeatTest の検証で確定したこと。GRTP への組み込みと受け渡しメモ（Hapbeat_Handoff.md）はこの内容を前提にする。

| 項目 | 決定 | 根拠 |
|---|---|---|
| SDK の版 | `com.hapbeat.sdk` 0.5.0（`#a74107150920330fec6077ecdf05c9f6da02d6ed` で固定） | M1-2 |
| デバイス | DuoWL v3 / fw 0.4.0 / Wi-Fi UDP 版ファーム | M0-3 |
| デバイスのアドレス | `player_1/pos_chest/group_2`（名前 `hb-grtp-01`） | M0-5 |
| hapbeat-helper | 0.4.0。Unity と同時に起動したままで問題なし | M1-4, M2-6 |
| Config の置き場所 | `Resources/HapbeatConfig.asset`（SDK が Resources から読むため移動しない） | M1-3 |
| 鳴らし方 | EventMap + Trigger 経由。強さは EventMap の gain で調整し、コードに書かない | F1-1 |
| 持続振動の方式 | **A. Command（長いクリップを Deploy + 開始で Fire / 終了で Stop）** | F1-2 |
| 持続振動で採用しなかった方式 | B. StreamClip ループ（動作は確認済み。強弱を連続で変えたくなったら再検討） | F1-2 |

## 分かったこと・注意点

- SDK の `loop` は StreamClip モード専用。Command モードではループしないので、持続振動は演出の長さ分のクリップを Kit に入れる。
- `HapbeatManager` は SDK 側でシングルトン（`Instance`、`DontDestroyOnLoad`）。重複すると GameObject ごと破棄するため、Router は専用の GameObject（Prefab）にする。
- Studio の再生テストや Deploy は Wi-Fi 側のデバイスを選ぶ（USB 側を選ぶと送信先が解決できず鳴らない）。
- Helper のログに `ffmpeg not found` が出る場合、44.1kHz などの自作 WAV は速度・音程がずれる可能性がある。`winget install ffmpeg` で解消する。
- Play 終了時の `[Hapbeat] Receive error: Thread was being aborted.` は受信スレッドの終了時に出るもので、無視してよい。

## 未確定（検証中）

- 止め忘れ時の挙動（F1-3）
- デバイスが無い環境での挙動（F1-4）
- シーン切り替え・Router 重複時の挙動（F1-5）
- `hapticDelaySeconds`（M2-7、受け渡し後に調整）
- 本番の target（M2-8、受け渡し後に調整）
