using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class BuildInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask buildPointLayer;
    [SerializeField] private GameSession gameSession;

    private bool warnedMissingLayer;

    public BuildPoint SelectedPoint { get; private set; }

    public event Action<BuildPoint> SelectionChanged;
    public static Func<Vector2, bool> ExternalPointerBlocker;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (gameSession == null)
            gameSession = GetComponent<GameSession>();
    }

    private void OnDisable()
    {
        ClearSelection();
    }

    private void Update()
    {
        if (gameSession != null && !gameSession.BuildAllowed)
        {
            ClearSelection();
            return;
        }

        if (!TryGetClickScreenPosition(out Vector2 screenPos, out int pointerId))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId))
            return;

        if (ExternalPointerBlocker != null && ExternalPointerBlocker(screenPos))
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (buildPointLayer.value == 0 && !warnedMissingLayer)
        {
            warnedMissingLayer = true;
            Debug.LogWarning("[BuildInputController] Build Point Layer chưa được chọn trong Inspector!", this);
        }

        float depth = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        Collider2D hit = Physics2D.OverlapPoint(worldPos, buildPointLayer);

        if (hit != null && hit.TryGetComponent<BuildPoint>(out BuildPoint point))
            SelectPoint(point);
        else
            ClearSelection();
    }

    public void SelectPoint(BuildPoint point)
    {
        if (point == null)
        {
            ClearSelection();
            return;
        }

        if (SelectedPoint == point)
            return;

        if (SelectedPoint != null)
            SelectedPoint.SetSelected(false);

        SelectedPoint = point;
        SelectedPoint.SetSelected(true);
        SelectionChanged?.Invoke(SelectedPoint);
    }

    public void ClearSelection()
    {
        if (SelectedPoint == null)
            return;

        SelectedPoint.SetSelected(false);
        SelectedPoint = null;
        SelectionChanged?.Invoke(null);
    }

    private static bool TryGetClickScreenPosition(out Vector2 screenPos, out int pointerId)
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                pointerId = -1;
                return true;
            }

            screenPos = default;
            pointerId = -1;
            return false;
        }

        if (Touchscreen.current != null)
        {
            var primaryTouch = Touchscreen.current.primaryTouch;
            if (primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = primaryTouch.position.ReadValue();
                pointerId = primaryTouch.touchId.ReadValue();
                return true;
            }
        }

        screenPos = default;
        pointerId = -1;
        return false;
    }
}
