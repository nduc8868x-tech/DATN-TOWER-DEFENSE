using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WaveEntryWarning : MonoBehaviour
{
    [SerializeField] private CanvasGroup display;
    [SerializeField] private Button callEarly;
    [SerializeField] private RectTransform skull;
    [SerializeField] private RectTransform arrow;
    [SerializeField] private Text countdown;
    [SerializeField] private WaveCountdownRing progressRing;
    [SerializeField] private Vector2 entryOffset = new Vector2(0, 70);
    [SerializeField, Min(60)] private float edgePadding = 90;
    private WaveManager manager;
    public bool IsVisible => display != null && display.alpha > 0;

    private void OnEnable() { if (callEarly != null) callEarly.onClick.AddListener(CallEarly); }
    private void OnDisable()
    {
        if (callEarly != null) callEarly.onClick.RemoveListener(CallEarly);
        Hide();
    }
    private void CallEarly()
    {
        if (manager != null && manager.TryCallNextWaveEarly()) Hide();
    }
    public void Refresh(WaveManager waves, SplineContainer route, Canvas canvas, Camera camera)
    {
        if (waves == null || route == null || canvas == null || camera == null) { Hide(); return; }
        var screen = camera.WorldToScreenPoint((Vector3)route.EvaluatePosition(0));
        var parent = (RectTransform)transform.parent;
        var uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
        if (screen.z <= 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out var entry))
        { Hide(); return; }
        var position = entry + entryOffset;
        var bounds = parent.rect;
        float padX = Mathf.Min(edgePadding, bounds.width * .5f), padY = Mathf.Min(edgePadding, bounds.height * .5f);
        position.x = Mathf.Clamp(position.x, bounds.xMin + padX, bounds.xMax - padX);
        position.y = Mathf.Clamp(position.y, bounds.yMin + padY, bounds.yMax - padY);
        transform.localPosition = new Vector3(position.x, position.y, 0);
        manager = waves;
        bool interactable = waves.CanCallNextWaveEarly;
        display.alpha = 1; display.interactable = interactable; display.blocksRaycasts = interactable;
        callEarly.interactable = interactable;
        skull.localScale = Vector3.one;
        if (progressRing != null) progressRing.Progress = waves.CountdownProgress;
        if (countdown != null) countdown.text = Mathf.CeilToInt(waves.CountdownRemaining) + "s";
        var direction = entry - position;
        if (direction.sqrMagnitude < .01f) direction = Vector2.down;
        direction.Normalize();
        arrow.anchoredPosition = direction * 70;
        arrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }
    public void Hide()
    {
        manager = null;
        if (progressRing != null) progressRing.Progress = 0;
        if (display != null) { display.alpha = 0; display.interactable = false; display.blocksRaycasts = false; }
        if (callEarly != null) callEarly.interactable = false;
    }
}
