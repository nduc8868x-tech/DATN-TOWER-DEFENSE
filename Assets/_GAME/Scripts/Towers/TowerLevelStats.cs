using System;
using UnityEngine;

// Configuration only. A value copy is held by each Tower after Initialize.
[Serializable]
public struct TowerLevelStats
{
    [Min(0)] public int damage;
    [Min(0.01f)] public float cooldown;
    [Min(0.01f)] public float range;
    [Min(0.01f)] public float projectileSpeed;
    public GameObject projectilePrefab;
    [Min(0)] public float radius; // Zero for direct-hit arrows.
    [Min(0)] public int cost;
    public Sprite bodySprite;

    public bool TryValidate(out string error)
    {
        error = string.Empty;
        if (damage < 0 || cost < 0 || !Positive(cooldown) ||
            !Positive(range) || !Positive(projectileSpeed) || radius < 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            error = "Damage/cost must be non-negative; cooldown/range/projectileSpeed must be finite and positive.";
        return error.Length == 0;
    }

    private static bool Positive(float value) => value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);
}
