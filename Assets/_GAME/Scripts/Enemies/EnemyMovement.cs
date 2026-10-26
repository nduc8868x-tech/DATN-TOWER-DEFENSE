using System;
using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
[RequireComponent(typeof(SplineAnimate))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField, Min(0f)] private float flipThreshold = 0.0001f;

    [Header("Runtime - inspect during Play Mode")]
    [SerializeField] private bool initialized;
    [SerializeField] private bool moving;
    [SerializeField] private bool reachedEnd;

    private SplineAnimate splineAnimate;
    private SpriteRenderer spriteRenderer;
    private Vector3 previousPosition;

    public bool IsMoving => moving;
    public bool HasReachedEnd => reachedEnd;

    public event Action<EnemyMovement> ReachedEnd;

    private void Awake()
    {
        splineAnimate = GetComponent<SplineAnimate>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        splineAnimate.PlayOnAwake = false;
        splineAnimate.Pause();
    }

    private void OnEnable()
    {
        splineAnimate.Completed += HandleCompleted;
    }

    private void OnDisable()
    {
        splineAnimate.Completed -= HandleCompleted;
        StopMovement();
    }

    public bool Initialize(SplineContainer path, float speed)
    {
        if (path == null ||
            path.Spline == null ||
            path.Spline.Count < 2)
        {
            Debug.LogError(
                $"{name}: A path with at least two knots is required.",
                this
            );
            return false;
        }

        if (speed <= 0f ||
            float.IsNaN(speed) ||
            float.IsInfinity(speed))
        {
            Debug.LogError(
                $"{name}: Speed must be a finite number greater than zero.",
                this
            );
            return false;
        }

        initialized = false;
        moving = false;
        reachedEnd = false;

        splineAnimate.Pause();

        splineAnimate.PlayOnAwake = false;
        splineAnimate.Container = path;
        splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        splineAnimate.MaxSpeed = speed;
        splineAnimate.Loop = SplineAnimate.LoopMode.Once;
        splineAnimate.Easing = SplineAnimate.EasingMode.None;
        splineAnimate.Alignment = SplineAnimate.AlignmentMode.None;
        splineAnimate.StartOffset = 0f;

        splineAnimate.enabled = true;
        splineAnimate.Restart(false);

        previousPosition = transform.position;
        initialized = true;

        return true;
    }

    [ContextMenu("Debug/Start Or Resume Movement")]
public void StartMovement()
{
    if (!Application.isPlaying || !isActiveAndEnabled)
    {
        return;
    }

    // Quái chết hoặc đã thoát thì không được chạy tiếp.
    Enemy enemy = GetComponent<Enemy>();

    if (enemy != null && !enemy.IsAlive)
    {
        return;
    }

    if (!initialized)
    {
        Debug.LogWarning(
            $"{name}: Initialize the enemy before starting movement.",
            this
        );
        return;
    }

    if (reachedEnd)
    {
        return;
    }

    previousPosition = transform.position;
    moving = true;

    splineAnimate.Play();
}

    [ContextMenu("Debug/Stop Movement")]
    public void StopMovement()
    {
        moving = false;

        if (splineAnimate != null)
        {
            splineAnimate.Pause();
        }
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        float deltaX = currentPosition.x - previousPosition.x;

        if (moving && Mathf.Abs(deltaX) > flipThreshold)
        {
            bool movingRight = deltaX > 0f;

            spriteRenderer.flipX = spriteFacesRight
                ? !movingRight
                : movingRight;
        }

        previousPosition = currentPosition;
    }

    private void HandleCompleted()
    {
        if (!initialized || reachedEnd)
        {
            return;
        }

        reachedEnd = true;
        StopMovement();

        Debug.Log($"{name} reached the endpoint.", this);

        ReachedEnd?.Invoke(this);
    }
}