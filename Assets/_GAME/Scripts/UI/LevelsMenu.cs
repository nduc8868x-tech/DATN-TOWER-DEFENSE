using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LevelsMenu : MonoBehaviour
{
    [System.Serializable]
    public sealed class LevelDefinition
    {
        public string sceneName;
        public bool unlocked;
        [Range(0, 3)] public int earnedStars;
    }
    [System.Serializable]
    public sealed class Slot
    {
        public Button button;
        public Image firstDigit, secondDigit, stars;
    }
    [SerializeField] private LevelDefinition[] levels;
    [SerializeField] private Slot[] slots;
    [SerializeField] private Sprite[] digits;
    [SerializeField] private Sprite[] starRatings;
    [SerializeField] private Button previous, next, close;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private string mainMenuScene = "MainMenu";
    private UnityAction[] slotActions;
    private int page;
    private bool loading;
    public int CurrentPage => page;
    public int PageCount => slots == null || slots.Length == 0 ? 0 : Mathf.CeilToInt((levels == null ? 0 : levels.Length) / (float)slots.Length);

    private void OnEnable()
    {
        if (slots != null)
        {
            slotActions = new UnityAction[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slotActions[i] = () => Select(index);
                if (slots[i].button != null) slots[i].button.onClick.AddListener(slotActions[i]);
            }
        }
        if (previous != null) previous.onClick.AddListener(Previous);
        if (next != null) next.onClick.AddListener(Next);
        if (close != null) close.onClick.AddListener(Close);
        Refresh();
    }
    private void OnDisable()
    {
        if (slots != null && slotActions != null)
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].button != null) slots[i].button.onClick.RemoveListener(slotActions[i]);
        if (previous != null) previous.onClick.RemoveListener(Previous);
        if (next != null) next.onClick.RemoveListener(Next);
        if (close != null) close.onClick.RemoveListener(Close);
    }
    private void Previous() { if (loading || page <= 0) return; page--; Refresh(); }
    private void Next() { if (loading || page + 1 >= PageCount) return; page++; Refresh(); }
    private void Select(int slotIndex)
    {
        if (loading || levels == null || slots == null) return;
        int index = page * slots.Length + slotIndex;
        if (index < 0 || index >= levels.Length || !(levels[index].unlocked || PlayerProgress.IsUnlocked(index + 1))) return;
        GuiNavigation.PendingLevel=levels[index].sceneName;GuiNavigation.ReturnScene="Levels";Open("Difficulty");
    }
    private void Close() => Open(mainMenuScene);
    private void Open(string sceneName)
    {
        if (loading || string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName)) return;
        loading = true;
        if (controls != null) controls.interactable = controls.blocksRaycasts = false;
        GuiNavigation.Go(sceneName);
    }
    private void Refresh()
    {
        if (previous != null) previous.interactable = !loading && page > 0;
        if (next != null) next.interactable = !loading && page + 1 < PageCount;
        if (slots == null || levels == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            int index = page * slots.Length + i;
            var slot = slots[i];
            bool exists = index < levels.Length;
            slot.button.gameObject.SetActive(exists);
            if (!exists) continue;
            var level = levels[index];
            bool available = (level.unlocked || PlayerProgress.IsUnlocked(index + 1)) && !string.IsNullOrWhiteSpace(level.sceneName) && Application.CanStreamedLevelBeLoaded(level.sceneName);
            slot.button.interactable = !loading && available;
            int number = index + 1;
            bool twoDigits = number >= 10;
            slot.firstDigit.sprite = digits[twoDigits ? (number / 10) % 10 : number % 10];
            slot.firstDigit.rectTransform.anchoredPosition = new Vector2(twoDigits ? -19 : 0, 32);
            slot.secondDigit.gameObject.SetActive(twoDigits);
            if (twoDigits) slot.secondDigit.sprite = digits[number % 10];
            slot.stars.sprite = starRatings[Mathf.Clamp(Mathf.Max(level.earnedStars, PlayerProgress.Stars(level.sceneName)), 0, 3)];
            var tint = available ? Color.white : new Color(.6f, .6f, .6f, .85f);
            slot.firstDigit.color = slot.secondDigit.color = slot.stars.color = tint;
        }
    }
}
