using UnityEditor;
using UnityEngine;

namespace Chrische.Localization
{
    public static class Localization
    {
        public static void SetCurrentLanguage(SystemLanguage language)
        {
            var dataBase = (TextDataBase)AssetDatabase.LoadAssetAtPath("Assets/textDataBase.asset", typeof(TextDataBase));
            if (dataBase.SelectedLanguages.Contains((int) language))
            {
                dataBase.CurrentLanguageId = (int) language;
                var allSetters = Object.FindObjectsOfType<BaseTextSetter>();
                foreach (var setter in allSetters)
                {
                    setter.UpdateText();
                }
            }
            else
            {
                Debug.Log("#Localization#: language is not vaild cause it is not in the database");
            }
            
        }
    }
}

