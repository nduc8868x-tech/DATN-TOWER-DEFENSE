using UnityEngine;
using UnityEngine.UI;

public sealed class LevelHUD : MonoBehaviour
{
    public FlowBootstrapper flow;
    public WaveManager waves;
    public BuildManager builds;
    public BuildInputController input;
    public Text status, selection, message, resultTitle;
    public Button startWave, pause, restart, buy, upgrade, sell, resultRestart;
    public GameObject resultPanel;
    private void OnEnable()
    {
        startWave.onClick.AddListener(StartWave);
        pause.onClick.AddListener(TogglePause);
        restart.onClick.AddListener(Restart);
        buy.onClick.AddListener(Buy);
        upgrade.onClick.AddListener(Upgrade);
        sell.onClick.AddListener(Sell);
        resultRestart.onClick.AddListener(Restart);
        flow.Session.SnapshotChanged += Refresh;
        input.SelectionChanged += SelectionChanged;
        builds.PointChanged += SelectionChanged;
        waves.Changed += RefreshWave;
        Refresh(flow.Session.GetSnapshot()); // Bind reads current state immediately.
    }
    private void OnDisable()
    {
        startWave.onClick.RemoveListener(StartWave); pause.onClick.RemoveListener(TogglePause);
        restart.onClick.RemoveListener(Restart); buy.onClick.RemoveListener(Buy);
        upgrade.onClick.RemoveListener(Upgrade); sell.onClick.RemoveListener(Sell);
        resultRestart.onClick.RemoveListener(Restart);
        if (flow && flow.Session) flow.Session.SnapshotChanged -= Refresh;
        if (input) input.SelectionChanged -= SelectionChanged;
        if (builds) builds.PointChanged -= SelectionChanged;
        if (waves) waves.Changed -= RefreshWave;
    }
    private void StartWave() { waves.StartNextWave(); }
    private void TogglePause() { if (flow.Session.State == GameState.Paused) flow.Session.Resume(); else flow.Session.Pause(); }
    public void Restart() { message.text = ""; flow.StartNewSession(); }
    private void Buy() { if (!builds.TryBuild(input.SelectedPoint, "arrow", out var error)) message.text = error; else message.text = "Arrow built"; }
    private void Upgrade() { if (!builds.TryUpgrade(input.SelectedPoint, out var error)) message.text = error; else message.text = "Arrow upgraded"; }
    private void Sell() { if (!builds.TrySell(input.SelectedPoint, out var refund, out var error)) message.text = error; else message.text = $"Sold +{refund} gold"; }
    private void SelectionChanged(BuildPoint _) => Refresh(flow.Session.GetSnapshot());
    private void RefreshWave() { if (!string.IsNullOrEmpty(waves.LastError)) message.text = waves.LastError; Refresh(flow.Session.GetSnapshot()); }
    private static void Label(Button b, string text) => b.GetComponentInChildren<Text>().text = text;
    private void Refresh(SessionSnapshot s)
    {
        status.text = $"GOLD  {s.Gold}     LIVES  {s.Lives}     WAVE  {s.CurrentWave}/{s.TotalWaves}     ALIVE  {s.AliveEnemies}";
        startWave.interactable = flow.Session.CanStartWave;
        Label(startWave, s.State == GameState.Preparing && waves.SpawnFinished ? "Next Wave" : "Start Wave");
        Label(pause, s.State == GameState.Paused ? "Resume" : "Pause");
        pause.interactable = s.State != GameState.Victory && s.State != GameState.Defeat;
        var point = input.SelectedPoint;
        bool occupied = point && point.IsOccupied;
        selection.text = !point ? "Select a marked build point" : $"{point.PointId}   " + (occupied ? $"Arrow {point.CurrentLevel}   Invested {point.TotalInvested}" : "Empty point");
        buy.gameObject.SetActive(point && !occupied);
        upgrade.gameObject.SetActive(occupied); sell.gameObject.SetActive(occupied);
        if (builds.TryGetBuildCost("arrow", out var price)) Label(buy, $"Buy Arrow  {price}");
        buy.interactable = flow.Session.BuildAllowed && s.Gold >= price;
        if (occupied)
        {
            var snap = point.CurrentTowerHandle.ReadSnapshot();
            bool hasNext = snap.CanUpgrade && builds.TryGetUpgradeCost("arrow", snap.Level + 1, out _);
            builds.TryGetUpgradeCost("arrow", snap.Level + 1, out var upgradePrice);
            Label(upgrade, hasNext ? $"Upgrade  {upgradePrice}" : "Max level");
            upgrade.interactable = hasNext && flow.Session.BuildAllowed && s.Gold >= upgradePrice;
            Label(sell, $"Sell  +{BuildManager.CalculateSellRefund(point.TotalInvested)}");
            sell.interactable = flow.Session.BuildAllowed;
        }
        bool ended = s.State == GameState.Victory || s.State == GameState.Defeat;
        resultPanel.SetActive(ended);
        resultTitle.text = s.State == GameState.Victory ? "VICTORY" : "DEFEAT";
    }
}
