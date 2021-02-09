using System;
using Sirenix.OdinInspector;

namespace Chrische.Localization
{
    [Serializable]
    public class StringLanguagePair
    {
        private int _languagueIndex;
        
        private string _text = String.Empty;
        
        public StringLanguagePair(int index)
        {
            _languagueIndex = index;
        }

        #region Properties
        public int LanguagueIndex => _languagueIndex;

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        #endregion
    }

}
