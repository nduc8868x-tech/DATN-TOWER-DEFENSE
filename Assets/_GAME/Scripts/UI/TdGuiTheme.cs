using UnityEngine;

[CreateAssetMenu(menuName="Tower Defense/GUI Theme")]
public sealed class TdGuiTheme : ScriptableObject
{
    [System.Serializable] public struct Art { public string key; public Sprite sprite; }
    public AudioClip music;
    public Art[] sprites;
    public Sprite Get(string key)
    {
        if(sprites!=null)foreach(var item in sprites)if(item.key==key)return item.sprite;
        return null;
    }
}
