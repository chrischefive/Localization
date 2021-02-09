using System;
using System.Collections.Generic;
using Chrische.Localization;
using UnityEngine;

[Serializable]
public class TextDataBase : ScriptableObject
{
    private List<int> _selectedLanguages = new List<int>();
    private List<TextEntry> _entries = new List<TextEntry>();
    private int _defaultLanguageId = -1;

    public string GetText(TextId id)
    {
        foreach (var entry in _entries)
        {
            if (id == entry.ID)
            {
                foreach (var t in entry.AllTexts)
                {
                    if (t.LanguagueIndex == _defaultLanguageId)
                    {
                        return t.Text;
                    }
                }
            }
        }
        return String.Empty;
    }

    #region Properties

    public List<int> SelectedLanguages
    {
        get => _selectedLanguages;
        set => _selectedLanguages = value;
    }

    public List<TextEntry> Entries
    {
        get => _entries;
        set => _entries = value;
    }

    public int DefaultLanguageId
    {
        get => _defaultLanguageId;
        set => _defaultLanguageId = value;
    }

    #endregion
}
