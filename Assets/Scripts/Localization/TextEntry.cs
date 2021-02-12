using System;
using System.Collections.Generic;

namespace Chrische.Localization
{
    [Serializable]
    public class TextEntry
    {
        private int _enumId = 0;
        private string _shadowId;
        private TextId _id;
        private List<StringLanguagePair> _allTexts = new List<StringLanguagePair>();

        public void AddStringLanguagePair(List<int> allLanguageIndices)
        {
            foreach (var index in allLanguageIndices)
            {
                _allTexts.Add(new StringLanguagePair(index, String.Empty));
            }
        }

        #region Properties

        public TextId ID
        {
            get => _id;
            set => _id = value;
        }

        public string ShadowId
        {
            get => _shadowId;
            set => _shadowId = value;
        }

        public List<StringLanguagePair> AllTexts => _allTexts;

        #endregion
    }
}

