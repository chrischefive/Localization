using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToGErmanButton : MonoBehaviour
{
    public void ToGerman()
    {
        Chrische.Localization.Localization.SetCurrentLanguage(SystemLanguage.German);
    }
}
