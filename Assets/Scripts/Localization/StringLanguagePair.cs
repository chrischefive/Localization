using System;

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
        
        public StringLanguagePair(int index, string text)
        {
            _languagueIndex = index;
            _text = text;
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
