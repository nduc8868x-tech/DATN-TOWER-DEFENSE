using UnityEngine;
using UnityEngine.UI;

public sealed class GuiMenuControls : MonoBehaviour
{
    public Text playerLabel;
    private void OnEnable(){PlayerProgress.Changed+=Refresh;}
    private void Start(){GuiAudio.Ensure();if(PlayerProgress.Data.quality>=0)QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerProgress.Data.quality,0,QualitySettings.names.Length-1));Refresh();}
    private void Refresh(){if(playerLabel!=null)playerLabel.text=PlayerProgress.Data.firstName.Length>0?"PLAYER: "+PlayerProgress.Data.firstName+"  |  STARS: "+PlayerProgress.TotalStars:"LOCAL PLAYER  |  STARS: "+PlayerProgress.TotalStars;}
    private void OnDisable(){PlayerProgress.Changed-=Refresh;}
}
