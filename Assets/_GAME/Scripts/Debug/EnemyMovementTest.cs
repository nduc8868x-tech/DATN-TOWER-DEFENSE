using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class EnemyMovementTest : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SplineContainer path;
    [SerializeField] private EnemyMovement[] enemies;

    [Header("Test Settings")]
    [SerializeField, Min(0.01f)] private float speed = 1.2f;
    [SerializeField, Min(0f)] private float startInterval = 0.8f;

    private IEnumerator Start()
    {
        if (path == null)
        {
            Debug.LogError(
                "EnemyMovementTest: Assign the path in Inspector.",
                this
            );
            yield break;
        }

        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogError(
                "EnemyMovementTest: Assign at least one enemy.",
                this
            );
            yield break;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyMovement enemy = enemies[i];

            if (enemy == null)
            {
                Debug.LogError(
                    $"EnemyMovementTest: Enemy at index {i} is missing.",
                    this
                );
                continue;
            }

            if (enemy.Initialize(path, speed))
            {
                enemy.StartMovement();
            }

            if (i < enemies.Length - 1)
            {
                yield return new WaitForSeconds(startInterval);
            }
        }
    }
}