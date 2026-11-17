using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.EventSystems;

// Opt-in acceptance harness. Never saved in the gameplay scene.
public sealed class Level01Acceptance : MonoBehaviour
{
    private readonly List<string> report = new();
    private FlowBootstrapper flow;
    private GameSession session;
    private WaveManager waves;
    private BuildManager builds;
    private CombatResolver combat;
    private BuildPoint[] points;
    private void Check(bool ok, string label)
    { report.Add((ok ? "PASS " : "FAIL ") + label); Save(); }
    private void Save() => File.WriteAllLines("Docs/Level01-playmode-results.txt", report);
    private IEnumerator Start()
    {
        Application.runInBackground = true;
        yield return null;
        flow = FindFirstObjectByType<FlowBootstrapper>(); session = flow.Session;
        waves = flow.GetComponent<WaveManager>(); builds = flow.GetComponent<BuildManager>(); combat = flow.GetComponent<CombatResolver>();
        points = FindObjectsByType<BuildPoint>(FindObjectsSortMode.None).OrderBy(p => p.PointId).ToArray();
        report.Add("Unity " + Application.unityVersion + " | " + System.DateTime.Now.ToString("s"));
        report.Add("Automated Play Mode checks; synthetic combat fixtures are separate from the six-wave no-cheat playthrough.");
        Check(points.Length == 5 && points.Select(p => p.PointId).Distinct().Count() == 5, "Exactly five unique active points");
        foreach (var p in points)
        {
            flow.StartNewSession(); yield return null;
            var screen = Camera.main.WorldToScreenPoint(p.AnchorPosition);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=screen}, hits);
            Check(hits.Count == 0 && Physics2D.OverlapPoint(p.AnchorPosition, 1 << p.gameObject.layer)?.GetComponent<BuildPoint>() == p,
                p.PointId + " world mouse pick unblocked by UI");
            Check(builds.TryBuild(p,"arrow",out _) && flow.Economy.CurrentGold == 140, p.PointId + " buy 100");
            Check(builds.TryUpgrade(p,out _) && flow.Economy.CurrentGold == 40 && p.CurrentLevel == 2, p.PointId + " upgrade 100");
            Check(builds.TrySell(p,out var refund,out _) && refund == 140 && !p.IsOccupied, p.PointId + " sell floor(70% of 200), empty");
        }
        flow.StartNewSession(); yield return null;
        Check(!builds.TryBuild(points[0],"rock",out _) && flow.Economy.CurrentGold == 240, "Rock purchase service locked initially");
        // Synthetic fixture: two real prefabs from distinct routes, splash via the production resolver.
        session.TrySetState(GameState.Running);
        var left = GameObject.Find("EnemyPath_Spline 1").GetComponent<SplineContainer>();
        var right = GameObject.Find("EnemyPath_Spline 2").GetComponent<SplineContainer>();
        flow.EnemySpawnService.TrySpawn(new EnemySpawnRequest(session.SessionId,1,"normal",left),out var a);
        flow.EnemySpawnService.TrySpawn(new EnemySpawnRequest(session.SessionId,1,"runner",right),out var b);
        session.RegisterEnemySpawned(a,1); session.RegisterEnemySpawned(b,1);
        a.Target.GetComponent<SplineAnimate>().NormalizedTime=.65f;
        b.Target.GetComponent<SplineAnimate>().NormalizedTime=.66f;
        a.Target.transform.position = b.Target.transform.position = Vector3.zero;
        var targeting = new TowerTargeting();
        Check(targeting.Tick(combat.Registry,session.SessionId,Vector3.zero,3,1).Identity.Equals(a.Identity),
            "Target shorter remaining route distance despite lower normalized progress");
        var rockData = builds.GetComponent<TowerFactory>().catalog;
        rockData.TryGet("rock",out var rock); rock.TryGetLevel(1,out var stats,out _);
        var projectile = Instantiate(stats.projectilePrefab).GetComponent<Projectile_Rock>();
        projectile.transform.position = Vector3.zero;
        projectile.Initialize(new RockShotContext(Vector3.zero,session.SessionId,30,1,4,5),combat);
        yield return null; yield return null;
        Check(a.Target.GetComponent<EnemyHealth>().CurrentHP == 30 && b.Target.GetComponent<EnemyHealth>().CurrentHP == 10,
            "Splash hits both routes once per identity through CombatResolver");
        flow.EnemySpawnService.BeginMovement(a); flow.EnemySpawnService.BeginMovement(b);
        yield return null; // Let SplineAnimate restore the fixture's teleported transforms first.
        session.Pause(); var posA=a.Position; var posB=b.Position;
        yield return new WaitForSecondsRealtime(.4f);
        Check(a.Position == posA && b.Position == posB, "Pause freezes enemies on both routes");
        session.Resume();
        yield return null;
        flow.StartNewSession(); yield return null;
        Check(session.AliveEnemies==0 && combat.ProjectileCount==0 && points.All(p=>!p.IsOccupied) && flow.Economy.CurrentGold==240 && session.Lives==20 && !builds.IsUnlocked("rock"),
            "Restart cleans both routes, projectiles, five points, unlocks and 240/20");
        waves.StartNextWave(); yield return null;
        int beforePause = waves.Spawned;
        session.Pause(); yield return new WaitForSecondsRealtime(2);
        Check(waves.Spawned==beforePause,"Pause freezes spawn interval");
        session.Resume(); yield return new WaitForSecondsRealtime(2);
        Check(waves.Spawned>beforePause,"Resume continues same spawn schedule");
        flow.StartNewSession(); yield return new WaitForSecondsRealtime(2);
        Check(waves.Spawned==0 && session.AliveEnemies==0 && session.State==GameState.Preparing,"Restart cancels pending spawn interval");
        // A short in-memory fixture verifies delay semantics without changing saved WaveData.
        var field=typeof(WaveManager).GetField("waves",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var savedWaves=field.GetValue(waves);
        var fixture=ScriptableObject.CreateInstance<WaveData>();
        fixture.groups=new[]{new WaveGroup{count=1,interval=.2f,routeId="route_left"},new WaveGroup{count=1,interval=.2f,routeId="route_right",delayBeforeGroup=2}};
        field.SetValue(waves,new[]{fixture}); flow.StartNewSession(); waves.StartNextWave();
        yield return new WaitForSecondsRealtime(.3f);
        Check(waves.Spawned==1 && !waves.SpawnFinished && session.State==GameState.Running,"Pending group prevents early completion");
        session.Pause(); yield return new WaitForSecondsRealtime(2.2f);
        Check(waves.Spawned==1 && !waves.SpawnFinished,"Pause freezes delayBeforeGroup");
        session.Resume(); yield return new WaitForSecondsRealtime(2.2f);
        Check(waves.Spawned==2 && waves.SpawnFinished && session.AliveEnemies==2,"Next group spawns without waiting for previous enemies to die");
        flow.StartNewSession(); waves.StartNextWave(); yield return new WaitForSecondsRealtime(.3f);
        flow.StartNewSession(); yield return new WaitForSecondsRealtime(2.2f);
        Check(waves.Spawned==0 && session.AliveEnemies==0,"Restart cancels pending group delay");
        fixture.groups=new[]{new WaveGroup{count=1,interval=.2f,routeId="route_left"}};
        flow.StartNewSession();
        session.Initialize(1,240,1,combat.SessionId); waves.Initialize(flow); waves.StartNextWave();
        yield return new WaitForSecondsRealtime(.2f);
        var terminal=new List<EnemyHandle>(); combat.Registry.CopyAliveTo(terminal);
        if(terminal.Count==1)
        { combat.RequestEscape(new EscapeRequest(terminal[0].Target,terminal[0].Identity)); combat.RequestEscape(new EscapeRequest(terminal[0].Target,terminal[0].Identity)); }
        yield return null; yield return null;
        Check(session.State==GameState.Defeat && session.Lives==0 && flow.Economy.CurrentGold==240 && waves.CompletedWaves==0,
            "Lethal final batch chooses Defeat before Victory; duplicate escape, no reward");
        field.SetValue(waves,savedWaves); flow.StartNewSession(); Destroy(fixture); yield return null;
        if (File.Exists("Temp/level01-boundary-only.request"))
        {
            FindFirstObjectByType<BuildInputController>().SelectPoint(points[4]);
            ScreenCapture.CaptureScreenshot("Docs/Level01-layout-final.png"); yield return null;
            report.Add("FINISHED boundary recheck; six-wave run recorded separately in Level01-six-wave-results.txt"); Save();
            File.Delete("Temp/level01-boundary-only.request");
            File.WriteAllText("Temp/level01-acceptance.done","done");
            yield break;
        }
        report.Add("BEGIN SIX-WAVE PLAYTHROUGH: normal timeScale=1, production purchase/upgrade/start APIs, no injected gold/damage/spawns."); Save();
        builds.TryBuild(points[1],"arrow",out _); builds.TryBuild(points[2],"arrow",out _);
        for (int wave=1; wave<=6; wave++)
        {
            bool started=waves.StartNextWave();
            Check(started && !waves.StartNextWave(), "Wave " + wave + " manual start, duplicate rejected");
            float timeout=Time.realtimeSinceStartup+100;
            while (session.State==GameState.Running && Time.realtimeSinceStartup<timeout)
            { Save(); yield return new WaitForSecondsRealtime(.5f); }
            report.Add($"WAVE {wave}: state={session.State}; gold={flow.Economy.CurrentGold}; lives={session.Lives}; spawned={waves.Spawned}; killed={session.WaveKills}; leaked={session.WaveLeaks}; spawnFinished={waves.SpawnFinished}; alive={session.AliveEnemies}"); Save();
            Check(waves.SpawnFinished && session.AliveEnemies==0 && session.State!=GameState.Defeat, "Wave " + wave + " fully resolved");
            if (session.State==GameState.Defeat || session.State==GameState.Running) break;
            if (wave==1) builds.TryUpgrade(points[2],out _);
            if (wave==3) { Check(builds.IsUnlocked("rock"),"Rock unlocked after completed wave 3"); builds.TryBuild(points[3],"rock",out _); }
            if (wave==4) builds.TryUpgrade(points[1],out _);
            if (wave==5) builds.TryUpgrade(points[3],out _);
            if(wave==3 || wave==5) { ScreenCapture.CaptureScreenshot($"Docs/Level01-wave{wave}.png"); yield return null; }
        }
        Check(session.State==GameState.Victory && waves.CompletedWaves==6,"Six-wave no-cheat playthrough Victory");
        ScreenCapture.CaptureScreenshot("Docs/Level01-final-gameview.png");
        report.Add("FINISHED"); Save();
        File.WriteAllText("Temp/level01-acceptance.done","done");
    }
}
