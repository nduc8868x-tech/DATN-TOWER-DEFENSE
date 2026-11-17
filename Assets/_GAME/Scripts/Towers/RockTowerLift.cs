using UnityEngine;

[DisallowMultipleComponent]
public sealed class RockTowerLift : MonoBehaviour
{
    [SerializeField] private Transform platform;
    [SerializeField] private SpriteRenderer loadedStone;
    [SerializeField] private SpriteRenderer launchRear;
    [SerializeField] private SpriteRenderer launchFront;
    [SerializeField, Min(0f)] private float dipDepth = 0.06f;
    [SerializeField, Min(0f)] private float riseHeight = 0.52f;
    [SerializeField, Min(0.01f)] private float dipDuration = 0.10f;
    [SerializeField, Min(0.01f)] private float riseDuration = 0.14f;
    [SerializeField, Min(0.01f)] private float returnDuration = 0.25f;
    [SerializeField, Min(0f)] private float reloadDuration = 0.17f;

    private enum Phase { Idle, Windup, Return }
    private Phase phase;
    private Vector3 restPosition;
    private float elapsed;
    private Sprite nextRear;
    private Sprite nextFront;
    private Sprite nextStone;

    public bool IsWindingUp => phase == Phase.Windup;
    public bool IsReturning => phase == Phase.Return;
    public float WindupDuration => dipDuration + riseDuration;
    public float WorldRise => platform != null && platform.parent != null
        ? platform.parent.TransformVector(Vector3.up * riseHeight).magnitude : 0f;

    public bool IsConfigured(Transform tower, SpriteRenderer body, Transform muzzle) =>
        enabled && gameObject.activeSelf && platform != null && platform != tower && platform.IsChildOf(tower) &&
        body != null && !body.transform.IsChildOf(platform) && muzzle != null && muzzle.IsChildOf(platform) &&
        loadedStone != null && loadedStone.transform.IsChildOf(platform) && loadedStone.sprite != null;

    public void ApplyLevelVisuals(TowerLevelStats stats)
    {
        nextRear = stats.launchRearSprite;
        nextFront = stats.launchFrontSprite;
        var renderer = stats.projectilePrefab != null
            ? stats.projectilePrefab.GetComponent<SpriteRenderer>() : null;
        nextStone = renderer != null ? renderer.sprite : null;
        // An upgrade during windup keeps the reserved shot's visuals until reload.
        if (phase == Phase.Idle) RefreshLoadedVisuals();
    }

    private void RefreshLoadedVisuals()
    {
        if (launchRear != null && nextRear != null) launchRear.sprite = nextRear;
        if (launchFront != null && nextFront != null) launchFront.sprite = nextFront;
        if (loadedStone != null && nextStone != null) loadedStone.sprite = nextStone;
    }

    public void BeginWindup()
    {
        restPosition = platform.localPosition;
        elapsed = 0f;
        phase = Phase.Windup;
    }

    // True only while the platform is at its release position.
    public bool Tick(float dt)
    {
        elapsed += dt;
        if (IsWindingUp)
        {
            float height = elapsed < dipDuration
                ? Mathf.SmoothStep(0f, -dipDepth, elapsed / dipDuration)
                : Mathf.Lerp(-dipDepth, riseHeight, Mathf.Pow(Mathf.Clamp01((elapsed - dipDuration) / riseDuration), 2f));
            platform.localPosition = restPosition + Vector3.up * height;
            return elapsed >= WindupDuration;
        }

        if (IsReturning)
        {
            float height = Mathf.SmoothStep(riseHeight, 0f, elapsed / returnDuration);
            platform.localPosition = restPosition + Vector3.up * height;
            if (elapsed >= returnDuration + reloadDuration) ResetLift();
        }
        return false;
    }

    public void BeginReturn(bool fired)
    {
        loadedStone.enabled = !fired;
        elapsed = 0f;
        phase = Phase.Return;
    }

    public void ResetLift()
    {
        if (phase == Phase.Idle) return;
        if (platform != null) platform.localPosition = restPosition;
        if (loadedStone != null) loadedStone.enabled = true;
        phase = Phase.Idle;
        elapsed = 0f;
        RefreshLoadedVisuals();
    }
}
