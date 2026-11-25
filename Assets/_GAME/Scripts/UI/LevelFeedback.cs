using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: combat, prices and wave counts stay in their existing systems.
[DisallowMultipleComponent]
public sealed class LevelFeedback : MonoBehaviour
{
    [SerializeField] private LevelHUD hud;
    [SerializeField] private WaveEntryWarning entryWarning;
    [SerializeField] private Canvas warningCanvas;
    [SerializeField] private Camera worldCamera;
    private readonly System.Collections.Generic.List<WaveEntryWarning> entryWarnings = new System.Collections.Generic.List<WaveEntryWarning>();
    [SerializeField] private Image damageFlash;
    [SerializeField] private TMP_Text livesLabel;
    [SerializeField, Min(.1f)] private float damageFlashDuration = .65f;
    [SerializeField] private Color rangeColor = new Color(.3f, 1f, .65f, .85f);
    private GameSession session;
    private LineRenderer range;
    private Material rangeMaterial;
    private int previousLives, sessionId;
    private float flashRemaining;
    private Color livesColor;
    public bool RangeVisible => range != null && range.enabled;
    public bool WarningVisible => entryWarnings.Exists(marker => marker != null && marker.IsVisible);
    public int VisibleWarningCount => entryWarnings.FindAll(marker => marker != null && marker.IsVisible).Count;
    public float DamageFlashAlpha => damageFlash != null ? damageFlash.color.a : 0;

    private void Awake()
    {
        if (entryWarning != null) entryWarnings.Add(entryWarning);
        if (livesLabel != null) livesColor = livesLabel.color;
        var go = new GameObject("SelectedTowerRange");
        go.transform.SetParent(transform, false);
        range = go.AddComponent<LineRenderer>();
        rangeMaterial = new Material(Shader.Find("Sprites/Default"));
        range.sharedMaterial = rangeMaterial;
        range.useWorldSpace = true; range.loop = true; range.positionCount = 128;
        range.widthMultiplier = .025f; range.numCornerVertices = 3;
        range.startColor = range.endColor = rangeColor;
        range.sortingOrder = 15; range.enabled = false;
        ResetPresentation();
    }
    private void LateUpdate()
    {
        var current = hud != null && hud.flow != null ? hud.flow.Session : null;
        if (session != current)
        {
            Unbind(); session = current;
            if (session != null) { session.SnapshotChanged += Snapshot; Snapshot(session.GetSnapshot()); }
        }
        if (session == null) return;
        var point = hud.input != null ? hud.input.SelectedPoint : null;
        bool show = session.BuildAllowed && point != null && point.IsOccupied;
        range.enabled = show;
        if (show)
        {
            var tower = point.CurrentTowerHandle.Instance;
            float radius = point.CurrentTowerHandle.ReadSnapshot().Range;
            var origin = tower != null ? tower.transform.position : point.AnchorPosition;
            for (int i = 0; i < range.positionCount; i++)
            {
                float a = i * Mathf.PI * 2 / range.positionCount;
                range.SetPosition(i, origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius);
            }
        }
        bool incoming = hud.waves != null && hud.waves.CountdownActive &&
            (session.State == GameState.Preparing || session.State == GameState.Paused);
        int count = incoming ? hud.waves.IncomingRoutes.Count : 0;
        if (entryWarning != null && warningCanvas != null)
        {
            while (entryWarnings.Count < count)
                entryWarnings.Add(Instantiate(entryWarning, entryWarning.transform.parent));
            if (worldCamera == null) worldCamera = Camera.main;
            for (int i = 0; i < entryWarnings.Count; i++)
            {
                if (i < count) entryWarnings[i].Refresh(hud.waves, hud.waves.IncomingRoutes[i], warningCanvas, worldCamera);
                else entryWarnings[i].Hide();
            }
        }
        flashRemaining = Mathf.Max(0, flashRemaining - Time.deltaTime);
        float strength = Mathf.Clamp01(flashRemaining / damageFlashDuration);
        if (damageFlash != null) damageFlash.color = new Color(1, .05f, .02f, .28f * strength);
        if (livesLabel != null) livesLabel.color = Color.Lerp(livesColor, Color.red, strength);
    }
    private void Snapshot(SessionSnapshot snapshot)
    {
        if (session.SessionId != sessionId)
        {
            sessionId = session.SessionId; previousLives = snapshot.Lives; ResetPresentation();
        }
        else if (snapshot.Lives < previousLives) flashRemaining = damageFlashDuration;
        previousLives = snapshot.Lives;
    }
    private void ResetPresentation()
    {
        flashRemaining = 0;
        if (range != null) range.enabled = false;
        foreach (var marker in entryWarnings) if (marker != null) marker.Hide();
        if (damageFlash != null) damageFlash.color = Color.clear;
        if (livesLabel != null) livesLabel.color = livesColor;
    }
    private void Unbind()
    {
        if (session != null) session.SnapshotChanged -= Snapshot;
        session = null;
    }
    private void OnDisable() { Unbind(); ResetPresentation(); }
    private void OnDestroy() { Unbind(); if (rangeMaterial != null) Destroy(rangeMaterial); }
}
