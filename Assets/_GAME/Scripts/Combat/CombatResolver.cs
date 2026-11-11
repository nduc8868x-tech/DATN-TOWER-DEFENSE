using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatResolver : MonoBehaviour
{
    private readonly Queue<HitRequest> hits = new Queue<HitRequest>();
    private readonly Queue<EscapeRequest> escapes = new Queue<EscapeRequest>();

    public EnemyRegistry Registry { get; } = new EnemyRegistry();

    private static int nextSessionId;
    private int sessionId;
    public int SessionId => sessionId;
    private int nextSpawnId;
    private int lastResolvedFrame = -1;
    private readonly HashSet<Projectile> projectiles = new HashSet<Projectile>();
    public int ProjectileCount => projectiles.Count;
    public void RegisterProjectile(Projectile projectile) => projectiles.Add(projectile);
    public void UnregisterProjectile(Projectile projectile) => projectiles.Remove(projectile);
    public void CleanupProjectiles(int session)
    {
        foreach (var projectile in new List<Projectile>(projectiles))
            if (projectile != null && projectile.SessionId == session) projectile.Cancel();
    }
    private void OnDestroy() => CleanupProjectiles(sessionId);

    public void BeginSession()
    {
        CleanupProjectiles(sessionId);
        sessionId = ++nextSessionId;
        nextSpawnId = 0;
        hits.Clear();
        escapes.Clear();
        lastResolvedFrame = -1;
        Registry.ClearSession();
    }

    private void LateUpdate()
    {
        ResolvePendingBatch();
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (sessionId == 0 || enemy == null || !enemy.IsAlive)
            return;

        if (enemy.Identity.SessionId == sessionId)
            return;

        var identity = new EnemyIdentity(sessionId, ++nextSpawnId);
        enemy.BindCombat(this, identity);
        Registry.Add(enemy);
    }

    public void RequestHit(HitRequest request)
    {
        if (request.RawDamage > 0)
            hits.Enqueue(request);
    }

    public void RequestEscape(EscapeRequest request)
    {
        escapes.Enqueue(request);
    }

    public void ResolvePendingBatch()
    {
        if (sessionId == 0 || lastResolvedFrame == Time.frameCount)
            return;

        lastResolvedFrame = Time.frameCount;

        int hitCount = hits.Count;
        int escapeCount = escapes.Count;

        for (int i = 0; i < hitCount; i++)
        {
            HitRequest hit = hits.Dequeue();

            if (CanResolve(hit.Target, hit.Identity))
                hit.Target.ApplyResolvedHit(hit.RawDamage);
        }

        for (int i = 0; i < escapeCount; i++)
        {
            EscapeRequest escape = escapes.Dequeue();

            if (CanResolve(escape.Target, escape.Identity))
                escape.Target.ApplyResolvedEscape();
        }
    }

    private bool CanResolve(Enemy target, EnemyIdentity identity)
    {
        return identity.IsValid
            && identity.SessionId == sessionId
            && target != null
            && target.isActiveAndEnabled
            && target.IsAlive
            && target.Identity.Equals(identity);
    }
}
