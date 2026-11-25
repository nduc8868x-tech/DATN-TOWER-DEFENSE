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

    private void OnEnable(){PlayerProgress.Changed+=Refresh;}
    private void Start(){GuiAudio.Ensure();Refresh();}
    private void OnDisable(){PlayerProgress.Changed-=Refresh;}
    private void Refresh(){if(icon==null)return;bool on=name=="Music"?PlayerProgress.Data.music>0:PlayerProgress.Data.sound>0;icon.sprite=on?onIcon:offIcon;}
    public void ToggleIcon()
    {
        if(name=="Music")PlayerProgress.Data.music=PlayerProgress.Data.music>0?0:.6f;
        else PlayerProgress.Data.sound=PlayerProgress.Data.sound>0?0:.8f;
        PlayerProgress.Store();GuiAudio.Click();
    }
}
