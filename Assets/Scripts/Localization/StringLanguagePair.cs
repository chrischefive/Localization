using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Chrische.Localization
{
    [Serializable]
    public class StringLanguagePair
    {
        public StringLanguagePair(SystemLanguage language)
        {
            _language = language;
        }
        
        [ShowInInspector] 
        [ReadOnly]
        private readonly SystemLanguage _language = default;
        
        [ShowInInspector] 
        private string _text = String.Empty;
    }

}
