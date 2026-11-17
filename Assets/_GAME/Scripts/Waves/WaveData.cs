using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Wave")]
public sealed class WaveData : ScriptableObject
{
    public string enemyId = "normal";
    [Min(1)] public int count = 5;
    [Min(0.01f)] public float interval = 1.5f;
    public WaveGroup[] groups;
    // Legacy assets remain readable until explicitly migrated by the editor.
    public WaveGroup[] GetGroups() => groups != null && groups.Length > 0 ? groups :
        new[] { new WaveGroup { enemyId = enemyId, routeId = "route_left", count = count, interval = interval } };
    public bool IsValid => System.Array.TrueForAll(GetGroups(), g => g != null &&
        !string.IsNullOrWhiteSpace(g.enemyId) && !string.IsNullOrWhiteSpace(g.routeId) &&
        g.count > 0 && g.interval > 0 && !float.IsNaN(g.interval) && !float.IsInfinity(g.interval) &&
        g.delayBeforeGroup >= 0 && !float.IsNaN(g.delayBeforeGroup) && !float.IsInfinity(g.delayBeforeGroup));
}

[System.Serializable]
public sealed class WaveGroup
{
    public string enemyId = "normal", routeId = "route_left";
    [Min(1)] public int count = 1;
    [Min(.01f)] public float interval = 1;
    [Min(0)] public float delayBeforeGroup;
}
