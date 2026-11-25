using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Image fill;
    [SerializeField] private Canvas display;

    private Enemy enemy;

    public float FillAmount => fill != null ? fill.fillAmount : 0f;
    public bool IsVisible => display != null && display.enabled;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<EnemyHealth>();
        if (display == null) display = GetComponent<Canvas>();
        if (health != null) enemy = health.GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.HealthChanged += Refresh;
        if (enemy != null) enemy.Resolved += Hide;
        Refresh(health.CurrentHP, health.MaxHP);
    }

    private void Start()
    {
        // Parent health may finish Awake after this child has been enabled.
        if (health != null) Refresh(health.CurrentHP, health.MaxHP);
    }

    private void OnDisable()
    {
        if (health != null) health.HealthChanged -= Refresh;
        if (enemy != null) enemy.Resolved -= Hide;
        if (display != null) display.enabled = false;
    }

    private void Refresh(int current, int maximum)
    {
        float ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;
        if (fill != null) fill.fillAmount = ratio;
        if (display != null)
            display.enabled = enemy != null && enemy.IsAlive && ratio > 0f;
    }

    private void Hide(Enemy resolved)
    {
        if (display != null) display.enabled = false;
    }
}
