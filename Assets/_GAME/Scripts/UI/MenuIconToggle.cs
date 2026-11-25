using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class MenuIconToggle : MonoBehaviour
{
    [SerializeField] private Sprite offIcon;

    private Image icon;
    private Sprite onIcon;

    private void Awake()
    {
        icon = GetComponent<Image>();
        onIcon = icon.sprite;
    }

    public void ToggleIcon()
    {
        icon.sprite = icon.sprite == onIcon ? offIcon : onIcon;
    }
}
