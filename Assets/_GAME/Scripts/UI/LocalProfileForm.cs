using UnityEngine;
using UnityEngine.UI;

public sealed class LocalProfileForm : MonoBehaviour
{
    public InputField firstName,lastName,email;
    public Button save;
    public Text status;
    private void OnEnable()
    {
        if(firstName!=null)firstName.text=PlayerProgress.Data.firstName;if(lastName!=null)lastName.text=PlayerProgress.Data.lastName;if(email!=null)email.text=PlayerProgress.Data.email;
        if(save!=null)save.onClick.AddListener(Save);
    }
    private void OnDisable(){if(save!=null)save.onClick.RemoveListener(Save);}
    private void Save()
    {
        string first=firstName.text.Trim(),last=lastName.text.Trim(),address=email.text.Trim();
        if(first.Length==0){status.text="Please enter your first name.";return;}
        if(address.Length>0&&(!address.Contains("@")||!address.Substring(address.IndexOf('@')+1).Contains("."))){status.text="Please enter a valid email, or leave it empty.";return;}
        PlayerProgress.Data.firstName=first;PlayerProgress.Data.lastName=last;PlayerProgress.Data.email=address;PlayerProgress.Store();GuiAudio.Click();status.text="Profile saved on this device.";
    }
}
