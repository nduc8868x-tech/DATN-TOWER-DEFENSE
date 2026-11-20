using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Opt-in Play Mode verification. Never attached to the saved gameplay scene.
public sealed class LevelIntegrationChecks : MonoBehaviour
{
    private FlowBootstrapper flow;
    private GameSession session;
    private EconomyManager gold;
    private BuildManager builds;
    private WaveManager waves;
    private CombatResolver resolver;
    private BuildPoint[] points;
    private readonly List<string> results = new List<string>();
    private int failed;
    public bool Finished { get; private set; }
    public string Report => string.Join("\n", results);
    private void Check(bool value, string name)
    {
        results.Add((value ? "PASS " : "FAIL ") + name);
        if (!value) failed++;
        Debug.Log(results[results.Count - 1], this);
    }
    private static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static int Listeners(object target, string field)
    {
        var d=target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(target) as Delegate;
        return d == null ? 0 : d.GetInvocationList().Length;
    }
    private EnemyHandle SpawnRegistered()
    {
        var path=UnityEngine.Object.FindFirstObjectByType<UnityEngine.Splines.SplineContainer>();
        flow.EnemySpawnService.TrySpawn(new EnemySpawnRequest(session.SessionId,session.CurrentWave,"normal",path),out var h);
        session.RegisterEnemySpawned(h,session.CurrentWave);
        return h;
    }
    private void CleanCheck(string name)
    {
        Check(gold.CurrentGold==240 && session.Lives==20 && session.State==GameState.Preparing && session.CurrentWave==1 &&
            session.AliveEnemies==0 && waves.Spawned==0 && !waves.SpawnFinished && resolver.ProjectileCount==0 &&
            All<Tower>().Length==0 && All<Enemy>().All(e=>!e.Identity.IsValid) && points.All(p=>!p.IsOccupied && !p.TransactionInProgress) &&
            Time.timeScale==1f,name);
    }
    private sealed class RejectUpgrade : TowerHandle
    {
        private readonly TowerHandle real;
        public RejectUpgrade(TowerHandle h) { real=h; }
        public override TowerSnapshot ReadSnapshot() => real.ReadSnapshot();
        public override bool TryApplyLevel(int level) => false;
    }
    private IEnumerator Start()
    {
        Application.runInBackground=true;
        yield return null;
        flow=UnityEngine.Object.FindFirstObjectByType<FlowBootstrapper>(); session=flow.Session; gold=flow.Economy;
        builds=flow.GetComponent<BuildManager>(); waves=flow.GetComponent<WaveManager>(); resolver=flow.GetComponent<CombatResolver>();
        points=All<BuildPoint>().OrderBy(p=>p.PointId).ToArray();
        results.Add("Unity Play Mode Level01 integration checks | " + DateTime.Now.ToString("s"));
        results.Add("Real prefab/service tests unless explicitly marked injected boundary/failure.");
        flow.StartNewSession(); yield return null;
        var p=points[0]; string error; int refund;
        CleanCheck("Initial/reset 240 gold, 20 lives, wave 1, empty scene runtime");
        Check(builds.TryBuild(p,"arrow",out error) && gold.CurrentGold==140 && p.CurrentLevel==1,"Buy real Arrow: 240 -> 140");
        Check(!builds.TryBuild(p,"arrow",out error) && gold.CurrentGold==140 && All<Tower>().Length==1,"Double purchase: one tower and one charge");
        Check(builds.TrySell(p,out refund,out error) && refund==70 && gold.CurrentGold==210,"Sell Arrow I refunds 70, clears point");
        yield return null;
        gold.Initialize(99);
        Check(!builds.TryBuild(p,"arrow",out error) && gold.CurrentGold==99 && !p.IsOccupied && All<Tower>().Length==0,"99 gold: rejected without tower or charge");
        gold.Initialize(240);
        var factory=flow.GetComponent<TowerFactory>(); var savedCatalog=factory.catalog; factory.catalog=null;
        Check(!builds.TryBuild(p,"arrow",out error) && gold.CurrentGold==240 && !p.IsOccupied,"Injected missing factory catalog: no gold/point mutation");
        factory.catalog=savedCatalog; yield return null;
        Check(All<Tower>().Length==0,"Failed real factory creation leaves no garbage");
        builds.TryBuild(p,"arrow",out error);
        var original=p.CurrentTowerHandle; var before=original.ReadSnapshot();
        p.SetOccupied(new RejectUpgrade(original),"arrow",1,100);
        Check(!builds.TryUpgrade(p,out error) && gold.CurrentGold==140 && p.CurrentLevel==1 && p.TotalInvested==100 &&
            original.ReadSnapshot().Damage==before.Damage && original.ReadSnapshot().Level==before.Level,
            "Injected atomic upgrade rejection AFTER money reservation: refund, unchanged level/stats/investment");
        p.SetOccupied(original,"arrow",1,100);
        var muzzle=original.Instance.Muzzle; Set(original.Instance,"muzzle",null);
        Check(!original.TryApplyLevel(2) && original.ReadSnapshot().Level==1 && original.ReadSnapshot().Damage==16,
            "Real TryApplyLevel invalid muzzle: atomic failure");
        Set(original.Instance,"muzzle",muzzle);
        Check(builds.TryUpgrade(p,out error) && gold.CurrentGold==40 && p.CurrentLevel==2 && p.TotalInvested==200,"Upgrade real Arrow II: cost 100 from TowerData");
        Check(builds.TrySell(p,out refund,out error) && refund==140 && gold.CurrentGold==180,"Sell Arrow II investment 200 refunds 140");
        yield return null;
        flow.StartNewSession(); session.TrySetState(GameState.Running);
        var dead=SpawnRegistered(); flow.EnemySpawnService.BeginMovement(dead);
        resolver.RequestEscape(new EscapeRequest(dead.Target,dead.Identity));
        resolver.RequestHit(new HitRequest(dead.Target,dead.Identity,1000));
        yield return null; yield return null;
        Check(dead.Target.State==EnemyState.Dead && gold.CurrentGold==248 && session.Lives==20 && session.AliveEnemies==0,
            "Real normal hit before escape in same batch: Dead +8, no leak");
        var payload=new EnemyResolved(dead.Identity,1,EnemyState.Dead,8,0);
        session.EnqueueResolved(payload); session.EnqueueResolved(payload); session.DrainResolvedBatch();
        Check(gold.CurrentGold==248 && session.AliveEnemies==0,"Duplicate result ignored for gold/lives/alive");
        var live=SpawnRegistered();
        session.EnqueueResolved(new EnemyResolved(live.Identity,2,EnemyState.Dead,999,0));
        session.EnqueueResolved(new EnemyResolved(new EnemyIdentity(session.SessionId,99999),1,EnemyState.Dead,999,0));
        session.EnqueueResolved(new EnemyResolved(new EnemyIdentity(session.SessionId+999,live.Identity.SpawnId),1,EnemyState.Escaped,0,99));
        session.DrainResolvedBatch();
        Check(gold.CurrentGold==248 && session.Lives==20 && session.AliveEnemies==1,"Wrong wave, unregistered identity and old/foreign session rejected");
        resolver.RequestEscape(new EscapeRequest(live.Target,live.Identity)); yield return null; yield return null;
        Check(gold.CurrentGold==248 && session.Lives==19 && session.AliveEnemies==0,"Real normal escape subtracts one life");
        flow.StartNewSession(); yield return null;
        session.EnqueueResolved(payload); session.DrainResolvedBatch();
        CleanCheck("Restart during Death + stale callback: clean reset, no reward/leak");
        builds.TryBuild(p,"arrow",out error);
        Check(waves.StartNextWave() && !waves.StartNextWave() && !waves.StartNextWave(),"Repeated Start creates only one schedule");
        yield return null;
        Check(waves.Spawned==1 && !waves.SpawnFinished && session.State==GameState.Running,"Wave remains Running before spawnFinished");
        float deadline=Time.realtimeSinceStartup+8;
        while(resolver.ProjectileCount==0 && Time.realtimeSinceStartup<deadline) yield return null;
        Check(resolver.ProjectileCount>0,"Real arrow projectile in flight");
        var enemies=All<Enemy>().Where(e=>e.Identity.SessionId==session.SessionId).ToArray();
        var projectiles=All<Projectile>();
        Vector3 enemyPos=enemies.Length>0 ? enemies[0].transform.position : Vector3.zero;
        Vector3 projectilePos=projectiles.Length>0 ? projectiles[0].transform.position : Vector3.zero;
        float cooldown=p.CurrentTowerHandle.ReadSnapshot().RemainingCooldown; int spawned=waves.Spawned;
        session.Pause();
        Check(!builds.TryBuild(points[1],"arrow",out error) && !builds.TryUpgrade(p,out error) && !builds.TrySell(p,out refund,out error),"Paused services reject buy/upgrade/sell");
        yield return new WaitForSecondsRealtime(.35f);
        Check(waves.Spawned==spawned && p.CurrentTowerHandle.ReadSnapshot().RemainingCooldown==cooldown &&
            enemies.Length>0 && enemies[0].transform.position==enemyPos && projectiles.Length>0 && projectiles[0] && projectiles[0].transform.position==projectilePos,
            "Pause freezes enemies, projectiles, cooldown and spawn clock");
        session.Resume(); yield return null;
        Check(waves.Spawned<=spawned+1,"Resume has no mass catch-up spawn");
        int snapshotListeners=Listeners(session,"SnapshotChanged"); int resultListeners=Listeners(flow.EnemySpawnService,"Resolved");
        flow.StartNewSession(); yield return null;
        CleanCheck("Restart while projectile flying: no enemy/tower/projectile/old schedule");
        session.Pause(); flow.StartNewSession(); yield return null;
        CleanCheck("Restart from pause restores timeScale and initial UI state");
        Check(Listeners(session,"SnapshotChanged")==snapshotListeners && Listeners(flow.EnemySpawnService,"Resolved")==resultListeners,
            "Repeated restarts do not multiply session/service listeners");
        // Natural gameplay using real wave data, real damage and real escapes (no forced kills).
        builds.TryBuild(points[0],"arrow",out error); builds.TryBuild(points[1],"arrow",out error);
        int completed=0;
        while(completed<3 && session.State!=GameState.Defeat)
        {
            Check(waves.StartNextWave(),"Natural gameplay Start wave "+(completed+1));
            deadline=Time.realtimeSinceStartup+45;
            while(session.State==GameState.Running && Time.realtimeSinceStartup<deadline) yield return null;
            Check(session.State==GameState.Preparing || session.State==GameState.Victory,"Natural wave "+(completed+1)+" completes within limit");
            if(session.State==GameState.Running) break;
            completed++;
            if(completed==1) { if(gold.CurrentGold>=100) builds.TryUpgrade(points[0],out error); }
            if(completed==2) { if(gold.CurrentGold>=100) builds.TryUpgrade(points[1],out error); }
        }
        Check(completed==3 && session.State==GameState.Victory && session.Lives>0,"Play all 3 real waves reaches Victory");
        results.Add($"Natural match result: {session.State}, gold={gold.CurrentGold}, lives={session.Lives}, wave={session.CurrentWave}, spawned={waves.Spawned}");
        Check(!builds.TryBuild(points[2],"arrow",out error) && !builds.TryUpgrade(points[0],out error) && !builds.TrySell(points[0],out refund,out error),"Victory services reject buy/upgrade/sell");
        flow.StartNewSession(); yield return null;
        CleanCheck("Restart after three-wave Victory");
        // Boundary: retain real 5/7/10 schedule; kill first two waves, let FINAL registered enemy take last life.
        Time.timeScale=8;
        for(int i=0;i<3;i++)
        {
            waves.StartNextWave();
            deadline=Time.realtimeSinceStartup+12;
            while(!waves.SpawnFinished && session.State==GameState.Running && Time.realtimeSinceStartup<deadline) yield return null;
            Check(waves.SpawnFinished && session.AliveEnemies>0 && session.State==GameState.Running,"SpawnFinished with living enemies does not finish wave "+(i+1));
            var alive=All<Enemy>().Where(e=>e.Identity.SessionId==session.SessionId && e.IsAlive).ToArray();
            if(i<2)
            {
                foreach(var e in alive) resolver.RequestHit(new HitRequest(e,e.Identity,1000));
                yield return null; yield return null;
            }
            else
            {
                Set(session,"lives",1); // Inject only the last-life boundary, never saved to data.
                int priorGold=gold.CurrentGold;
                foreach(var e in alive.Take(Math.Max(0,alive.Length-1))) resolver.RequestHit(new HitRequest(e,e.Identity,1000));
                var last=alive.Last(); resolver.RequestEscape(new EscapeRequest(last,last.Identity));
                yield return null; yield return null;
                Check(session.State==GameState.Defeat && session.Lives==0 && session.AliveEnemies==0 &&
                    gold.CurrentGold==priorGold+8*(alive.Length-1),"Final batch lethal final escape: Defeat takes priority, no +20 reward/Victory");
            }
        }
        Check(!builds.TryBuild(points[0],"arrow",out error) && !builds.TryUpgrade(points[0],out error) && !builds.TrySell(points[0],out refund,out error),"Defeat services reject buy/upgrade/sell");
        flow.StartNewSession(); yield return null;
        CleanCheck("Restart after Defeat");
        var spawnService=flow.GetComponent<EnemySpawnService>();
        spawnService.enabled=false; // Inject a recoverable provider failure without touching assets.
        Time.timeScale=8;
        waves.StartNextWave();
        deadline=Time.realtimeSinceStartup+5;
        while(session.State==GameState.Running && Time.realtimeSinceStartup<deadline) yield return null;
        Check(session.State==GameState.Defeat && waves.Spawned==0 && session.AliveEnemies==0 && gold.CurrentGold==240 &&
            waves.LastError.Contains("3/3") && waves.LastError.Contains("disabled"),
            "Spawn failure: exact reason, three attempts, zero counts/reward, bounded abort");
        yield return new WaitForSecondsRealtime(.25f);
        Check(waves.Spawned==0 && session.AliveEnemies==0,"Aborted spawn schedule stays stopped");
        spawnService.enabled=true;
        flow.StartNewSession(); yield return null;
        CleanCheck("Restart after spawn failure resets scheduling and diagnostics");
        Check(All<GameSession>().Length==1 && All<EconomyManager>().Length==1 && All<CombatResolver>().Length==1 && All<WaveManager>().Length==1 &&
            All<UnityEngine.EventSystems.EventSystem>().Length==1,"One manager set, one resolver runner, one EventSystem");
        var hud=UnityEngine.Object.FindFirstObjectByType<LevelHUD>();
        Check(hud.status.text.Contains("240") && hud.status.text.Contains("20") && hud.startWave.interactable && !hud.resultPanel.activeSelf,"HUD initial snapshot and controls after restart");
        results.Add($"TOTAL: {results.Count(s=>s.StartsWith("PASS "))} passed, {failed} failed");
        Finished=true;
#if UNITY_EDITOR
        System.IO.File.WriteAllText("Docs/IntegrationTestResults.txt",Report);
#endif
        Debug.Log(Report,this);
    }
}
