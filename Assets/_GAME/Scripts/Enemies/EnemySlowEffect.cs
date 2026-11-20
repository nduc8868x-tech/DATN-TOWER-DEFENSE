using UnityEngine;
using UnityEngine.Splines;

public sealed class EnemySlowEffect : MonoBehaviour
{
    private struct Slow
    {
        public float Fraction;
        public float Remaining;
    }

    private readonly System.Collections.Generic.List<Slow> active = new System.Collections.Generic.List<Slow>();
    private SplineAnimate movement;
    private Enemy enemy;
    private float baseSpeed;
    private float appliedFraction;

    private void Awake()
    {
        movement = GetComponent<SplineAnimate>();
        enemy = GetComponent<Enemy>();
        if (movement != null) baseSpeed = movement.MaxSpeed;
    }

    public void Apply(float fraction, float duration)
    {
        if (movement == null || enemy == null || !enemy.IsAlive) return;
        active.Add(new Slow { Fraction = Mathf.Clamp(fraction, 0f, 0.95f), Remaining = duration });
        RefreshSpeed();
    }

    private void Update()
    {
        if (enemy == null || !enemy.IsAlive)
        {
            active.Clear();
            RefreshSpeed();
            return;
        }
        if (Time.deltaTime <= 0f) return;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var slow = active[i];
            slow.Remaining -= Time.deltaTime;
            if (slow.Remaining <= 0f) active.RemoveAt(i);
            else active[i] = slow;
        }
        RefreshSpeed();
    }

    private void RefreshSpeed()
    {
        if (movement == null) return;
        float strongest = 0f;
        foreach (var slow in active) strongest = Mathf.Max(strongest, slow.Fraction);
        if (Mathf.Approximately(strongest, appliedFraction)) return;
        float progress = movement.NormalizedTime;
        movement.MaxSpeed = baseSpeed * (1f - strongest);
        movement.NormalizedTime = progress;
        appliedFraction = strongest;
    }

    private void OnDisable()
    {
        active.Clear();
        RefreshSpeed();
    }
}
