using Hapbeat;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// M1-5 検証用。キー入力で HapbeatManager を直接呼び、Play モードでの疎通を確認する。
/// 1: impact / 2: splash / 3: rumble / S: StopAll
/// 本番では使わない（本番は EventMap + Trigger 経由）。
/// </summary>
public sealed class HapbeatKeyTester : MonoBehaviour
{
    [SerializeField] private string _impactEventId = "hbtest.impact";
    [SerializeField] private string _splashEventId = "hbtest.splash";
    [SerializeField] private string _rumbleEventId = "hbtest.rumble";
    [SerializeField, Range(0f, 2f)] private float _gain = 1f;

    private void Update()
    {
        var keyboard = Keyboard.current;
        var manager = HapbeatManager.Instance;
        if (keyboard == null || manager == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) Play(manager, _impactEventId);
        if (keyboard.digit2Key.wasPressedThisFrame) Play(manager, _splashEventId);
        if (keyboard.digit3Key.wasPressedThisFrame) Play(manager, _rumbleEventId);
        if (keyboard.sKey.wasPressedThisFrame)
        {
            manager.StopAll();
            Debug.Log("[HapbeatKeyTester] StopAll");
        }
    }

    private void Play(HapbeatManager manager, string eventId)
    {
        manager.Play(eventId, _gain);
        Debug.Log($"[HapbeatKeyTester] Play {eventId} gain={_gain:0.00} alive={manager.AliveDeviceCount}");
    }
}
