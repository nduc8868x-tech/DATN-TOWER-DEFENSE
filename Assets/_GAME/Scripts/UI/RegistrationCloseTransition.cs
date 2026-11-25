using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class RegistrationCloseTransition : MonoBehaviour
{
    private const string MainMenuScene = "MainMenu";
    private const float SlideDuration = 0.45f;

    private bool closing;

    public void Close()
    {
        if (closing)
            return;

        if (!Application.CanStreamedLevelBeLoaded(MainMenuScene))
        {
            Debug.LogError("Add MainMenu to the active build profile's scene list.", this);
            return;
        }

        closing = true;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);

        StartCoroutine(SlideOut());
    }

    private IEnumerator SlideOut()
    {
        var container = (RectTransform)transform;
        var canvasRect = (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;
        var start = container.anchoredPosition;
        float distance = (canvasRect.rect.height + container.rect.height) * 0.5f;
        var end = start + Vector2.up * distance;

        float elapsed = 0f;
        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / SlideDuration);
            container.anchoredPosition = Vector2.Lerp(start, end, progress);
            yield return null;
        }

        GuiNavigation.Go(MainMenuScene);
    }
}
