using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Chrische.Localization
{
    [Serializable]
    public class TextEntry
    {
        [ShowInInspector] 
        [ReadOnly] private int _enumId = 0;
        
        [ShowInInspector]
        [SerializeField]
        private string _id = String.Empty;

        [ShowInInspector]
        [SerializeField]
        private List<StringLanguagePair> _allTexts = new List<StringLanguagePair>();

        public void AddStringLanguagePair(List<SystemLanguage> allLanguages)
        {
            foreach (var language in allLanguages)
            {
                _allTexts.Add(new StringLanguagePair(language));
            }
        }
    }
}

