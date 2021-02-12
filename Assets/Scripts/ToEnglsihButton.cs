using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToEnglsihButton : MonoBehaviour
{
    public void ToEnglish()
    {
        Chrische.Localization.Localization.SetCurrentLanguage(SystemLanguage.English);
    }
}
