using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData_Normal",
    menuName = "Tower Defense/Enemy Data"
)]
public class EnemyData : ScriptableObject
{
    [Header("Thông tin")]
    public string enemyId = "normal";
    public GameObject prefab;

    [Header("Chỉ số chiến đấu")]
    [Min(1)]
    public int maxHP = 60;

    [Min(0.01f)]
    public float speed = 1.2f;

    [Range(0f, 1f)]
    public float armor = 0f;

    [Header("Thưởng và sát thương căn cứ")]
    [Min(0)]
    public int bounty = 8;

    [Min(1)]
    public int leakDamage = 1;
}