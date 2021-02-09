using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Chrische
{
    namespace Localization
    {
        public class Settings : MonoBehaviour
        {
            [ShowInInspector] 
            [SerializeField] 
            [BoxGroup("Languages")]
            private SystemLanguage _allLanguages = default;

            [ShowInInspector] 
            private List<SystemLanguage> _choosenLanguages = new List<SystemLanguage>();

            [ShowInInspector] 
            [SerializeField] 
            [BoxGroup("Entries")]
            private List<TextEntry> _allEntries = new List<TextEntry>();


            [ShowInInspector]
            [BoxGroup("Languages")]
            private void AddLanguage()
            {
                if (!_choosenLanguages.Contains(_allLanguages))
                {
                    _choosenLanguages.Add(_allLanguages);
                }
            }

            [ShowInInspector]
            [BoxGroup("Entries")]
            private void AddEntry()
            {
                var entry = new TextEntry();
                //entry.AddStringLanguagePair(_choosenLanguages);
                _allEntries.Add(entry);
            }
        }
    }
}
