using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Wave")]
public sealed class WaveData : ScriptableObject
{
    public string enemyId = "normal";
    [Min(1)] public int count = 5;
    [Min(0.01f)] public float interval = 1.5f;
    public bool IsValid => enemyId == "normal" && count > 0 && interval > 0 && !float.IsNaN(interval) && !float.IsInfinity(interval);
}
