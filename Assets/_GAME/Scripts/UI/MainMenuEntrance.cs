using System.Collections;
using UnityEngine;

public sealed class MainMenuEntrance : MonoBehaviour
{
    private const float TopDelay = 0.9f;
    private const float BottomDelay = 1.05f;
    private const float IconStagger = 0.14f;
    private const float SlideTime = 0.85f;
    private const float FirstBounceTime = 0.32f;
    private const float SecondBounceTime = 0.19f;
    private const float IconDuration = SlideTime + FirstBounceTime + SecondBounceTime;
    private const float RegistrationDelay = 1.45f;
    private const float RegistrationDuration = 1.55f;
    private const float MaxFrameStep = 1f / 15f;

    [SerializeField] private RectTransform[] topIcons;
    [SerializeField] private RectTransform[] bottomIcons;
    [SerializeField] private RectTransform registration;

    private IEnumerator Start()
    {
        Canvas.ForceUpdateCanvases();
        var canvas = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        var top = CaptureIcons(topIcons);
        var bottom = CaptureIcons(bottomIcons);
        var registrationRest = registration.anchoredPosition;
        float lastIconStart = Mathf.Max(
            TopDelay + (top.Length - 1) * IconStagger,
            BottomDelay + (bottom.Length - 1) * IconStagger);
        float duration = Mathf.Max(lastIconStart + IconDuration, RegistrationDelay + RegistrationDuration);

        float elapsed = 0f;
        while (true)
        {
            float halfCanvasHeight = canvas.rect.height * 0.5f;
            PlaceIcons(top, elapsed - TopDelay, halfCanvasHeight, 1f);
            PlaceIcons(bottom, elapsed - BottomDelay, halfCanvasHeight, -1f);

            float startY = halfCanvasHeight + registration.rect.height * 0.5f + 2f;
            float progress = (elapsed - RegistrationDelay) / RegistrationDuration;
            float y = Mathf.SmoothStep(startY, registrationRest.y, progress);
            registration.anchoredPosition = new Vector2(registrationRest.x, y);

            if (elapsed >= duration)
                yield break;

            yield return null;
            // Editor startup can produce a long frame. Keep the intro visible through it.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep);
        }
    }

    private static IconPose[] CaptureIcons(RectTransform[] icons)
    {
        var poses = new IconPose[icons.Length];
        for (int i = 0; i < icons.Length; i++)
            poses[i] = new IconPose(icons[i]);

        return poses;
    }

    private static void PlaceIcons(IconPose[] icons, float time, float halfCanvasHeight, float side)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            var icon = icons[i];
            float t = time - i * IconStagger;
            float height = icon.Transform.rect.height;
            float startY = side * (halfCanvasHeight + height * 0.5f + 2f);
            float y = icon.Position.y;
            if (t < SlideTime)
            {
                float progress = Mathf.Clamp01(t / SlideTime);
                y = Mathf.Lerp(startY, y, progress * progress);
            }
            else if (t < SlideTime + FirstBounceTime)
            {
                y += side * height * 0.20f * BounceArc((t - SlideTime) / FirstBounceTime);
            }
            else if (t < IconDuration)
            {
                float bounceTime = t - SlideTime - FirstBounceTime;
                y += side * height * 0.06f * BounceArc(bounceTime / SecondBounceTime);
            }

            float squash = Mathf.Clamp01(1f - Mathf.Abs(t - SlideTime) / 0.085f);
            icon.Transform.anchoredPosition = new Vector2(icon.Position.x, y);
            icon.Transform.localScale = new Vector3(
                icon.Scale.x * (1f + 0.05f * squash),
                icon.Scale.y * (1f - 0.10f * squash),
                icon.Scale.z);
        }
    }

    private static float BounceArc(float progress)
    {
        return 4f * progress * (1f - progress);
    }

    private readonly struct IconPose
    {
        public readonly RectTransform Transform;
        public readonly Vector2 Position;
        public readonly Vector3 Scale;

        public IconPose(RectTransform transform)
        {
            Transform = transform;
            Position = transform.anchoredPosition;
            Scale = transform.localScale;
        }
    }
}
