using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent, RequireComponent(typeof(Button))]
public sealed class MenuSceneLink : MonoBehaviour
{
    [SerializeField] private string targetScene = "Levels";
    private Button button;
    private bool loading;
    private void Awake() => button = GetComponent<Button>();
    private void OnEnable() { if (button == null) button = GetComponent<Button>(); button.onClick.AddListener(Open); }
    private void OnDisable() { if (button != null) button.onClick.RemoveListener(Open); }
    private void Open()
    {
        if (loading || !Application.CanStreamedLevelBeLoaded(targetScene)) return;
        if(targetScene=="Settings"||targetScene=="Help")GuiNavigation.ReturnScene=gameObject.scene.name;
        loading = GuiNavigation.Go(targetScene); if(loading)button.interactable=false;
    }
}
