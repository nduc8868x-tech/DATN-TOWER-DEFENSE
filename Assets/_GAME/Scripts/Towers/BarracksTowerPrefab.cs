using System;
using UnityEngine;

[Serializable]
public struct BarracksLevelSettings
{
    public Sprite bodySprite;
    [Min(0)] public int cost;
    [Min(1)] public int soldierCount;
    [Min(1)] public int soldierHP;
    [Min(1)] public int soldierDamage;
    [Min(0.01f)] public float attackInterval;
    [Range(0f, 1f)] public float soldierArmor;
    [Min(0f)] public float respawnSeconds;
    [Min(0f)] public float rallyRange;
}

[DisallowMultipleComponent]
public sealed class BarracksTowerPrefab : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private TextAsset designNote;
    [SerializeField] private Transform rallyPoint;
    [SerializeField] private Transform[] soldierSlots;
    [SerializeField] private BarracksLevelSettings[] levels;
    [SerializeField, Range(1, 2)] private int level = 1;

    public int Level => level;
    public BarracksLevelSettings Settings => levels[level - 1];

    public bool TryApplyLevel(int nextLevel)
    {
        if (nextLevel != level + 1 || levels == null || nextLevel > levels.Length) return false;
        level = nextLevel;
        ApplyAppearance();
        return true;
    }

    private void Awake() => ApplyAppearance();

    private void ApplyAppearance()
    {
        if (levels == null || level < 1 || level > levels.Length || body == null) return;
        body.sprite = Settings.bodySprite;
        for (int i = 0; i < soldierSlots.Length; i++)
            soldierSlots[i].gameObject.SetActive(i < Settings.soldierCount);
    }

    private void OnDrawGizmosSelected()
    {
        if (levels == null || level < 1 || level > levels.Length || rallyPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Settings.rallyRange);
    }
}
