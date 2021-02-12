using System;
using System.Collections.Generic;
using Chrische.Localization;
using UnityEngine;

[Serializable]
public class TextDataBase : ScriptableObject
{
    private List<int> _selectedLanguages = new List<int>();
    private List<TextEntry> _entries = new List<TextEntry>();
    private int _currentLanguageId = -1;

    public string GetText(TextId id)
    {
        foreach (var entry in _entries)
        {
            if (id == entry.ID)
            {
                foreach (var t in entry.AllTexts)
                {
                    if (t.LanguagueIndex == _currentLanguageId)
                    {
                        return t.Text;
                    }
                }
            }
        }
        return String.Empty;
    }

    #region Properties

    public List<string> AllValues
    {
        get
        {
            var values = new List<string>();
            foreach (var entry in _entries)
            {
                values.Add(entry.ShadowId);
            }

            return values;
        }
    }

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

    public int CurrentLanguageId
    {
        get => _currentLanguageId;
        set => _currentLanguageId = value;
    }

    #endregion
}
