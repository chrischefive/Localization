using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Chrische.Localization
{
    public class SettingsWindow : EditorWindow
    {
        private int _selectedLanguageIndex = 0;
        Vector2 _scrollPos = Vector2.zero;
        private bool _areTextsValid = true;
        private TextDataBase _source = default;
        private bool _isLanguageGroupShown = true;
        private readonly List<bool> _textFoldout = new List<bool>();


        [MenuItem("Window/Localization/Settings")]
        public static void ShowWindow()
        {
            var path = Path.GetFullPath("BaseTextSetter.cs");
            Debug.Log("#SettingsWindow#: " + path);
            path = Path.GetDirectoryName("#SettingsWindow#: " + path);
            Debug.Log(path);
            
            EnumGenerator.Generate(new List<string>(){"Mock"});
            GetWindow<SettingsWindow>("Settings");
        }
        private void OnGUI()
        {
            GUILayout.Label("Languages");
            EditorGUILayout.BeginHorizontal("DataBase");
            {
                _source = EditorGUILayout.ObjectField(_source, typeof(TextDataBase), false) as TextDataBase;
                if (GUILayout.Button("Generate"))
                {
                    var dataBase = CreateInstance<TextDataBase>();
                    AssetDatabase.CreateAsset(dataBase, "Assets/textDataBase.asset");
                    _source = dataBase;
                    AssetDatabase.SaveAssets();
                    Repaint();
                }

                if (_source)
                {
                    foreach (var unused in _source.Entries)
                    {
                        _textFoldout.Add(true);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawUiLine(new Color(0.57f, 0.57f, 0.57f), 4);
            
            if (_source)
            {
                ShowContent();
            }
            if (GUILayout.Button("Load from csv"))
            {
                LoadFromCsv();
            }

            if (GUILayout.Button("Clear all"))
            {
                _textFoldout.Clear();
                _source = null;
                Repaint();
            }
        }
        
        private void ShowContent()
        {
            _isLanguageGroupShown = EditorGUILayout.BeginFoldoutHeaderGroup(_isLanguageGroupShown, "Language");
            if (_isLanguageGroupShown)
            {
                ShowLanguagesField();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            DrawUiLine(new Color(0.57f, 0.57f, 0.57f), 4);
            EditorGUILayout.Space();

            ShowTextSection();
            EditorGUILayout.Space();
            
            DrawUiLine(new Color(0.57f, 0.57f, 0.57f), 4);
            EditorGUILayout.Space();

            ShowButtonSection();
            
        }

        private void ShowButtonSection()
        {
            if (GUILayout.Button("Validate IDs"))
            {
                ValidateIdFailure failure = ValidateIds();
                switch (failure)
                {
                    case ValidateIdFailure.OKAY:
                    {
                        EditorUtility.DisplayDialog("Validate IDs", "Everything allright", "Okay");
                        break;
                    }
                    case ValidateIdFailure.EMPTY_ID:
                    {
                        EditorUtility.DisplayDialog("Validate IDs", "At least one ID is empty", "Okay");
                        break;
                    }
                    case ValidateIdFailure.DUPLICATE_ID:
                    {
                        EditorUtility.DisplayDialog("Validate IDs", "at least two ids are same", "Okay");
                        break;
                    }
                    case ValidateIdFailure.ID_WITH_SPACE:
                    {
                        EditorUtility.DisplayDialog("Validate IDs", "At least one id has a space", "Okay");
                        break;
                    }
                }
            }
            if (GUILayout.Button("Validate Texts"))
            {
                ValidateTexts();
            }

            if (!_areTextsValid)
            {
                EditorGUILayout.HelpBox("There are at least one text empty", MessageType.Error, true);
            }

            if (GUILayout.Button("Save"))
            {
                EnumGenerator.Generate(_source.AllValues);
                for (var i = 0; i < _source.Entries.Count; ++i)
                {
                    _source.Entries[i].ID = (TextId) i;
                }
            }

            if (GUILayout.Button("Save to csv"))
            {
                SaveToCsv();
            }
        }

        private void ShowTextSection()
        {
            if (_source.SelectedLanguages.Count > 0)
            {
                if(GUILayout.Button("Add Text"))
                {
                    var newEntry = new TextEntry();
                    newEntry.AddStringLanguagePair(_source.SelectedLanguages);
                    newEntry.ShadowId = String.Empty;
                    _source.Entries.Add(newEntry);
                    _textFoldout.Add(true);
                }
            }
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                for(var i = 0; i < _source.Entries.Count; ++i)
                {
                    _textFoldout[i] = EditorGUILayout.BeginFoldoutHeaderGroup(_textFoldout[i], _source.Entries[i].ShadowId);
                    {
                        if (_textFoldout[i])
                        {
                            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(5));
                            rect.y += (int) (5 / 2f);
                            rect.x -= 2;
                            rect.height = _source.Entries[i].AllTexts.Count * 20 + 28;
                            EditorGUI.DrawRect(rect, Color.grey);
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("ID:");
                                _source.Entries[i].ShadowId = EditorGUILayout.TextField(_source.Entries[i].ShadowId, GUILayout.Width(250));
                                if (GUILayout.Button("Delete ID", GUILayout.Width(80)))
                                {
                                    _source.Entries.Remove(_source.Entries[i]);
                                    _textFoldout.RemoveAt(i);
                                    break;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            foreach (var text in _source.Entries[i].AllTexts)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label((SystemLanguage) text.LanguagueIndex + ":");
                                    text.Text = EditorGUILayout.TextField(text.Text, GUILayout.Width(332));
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowLanguagesField()
        {
            var all = new List<GUIContent>();
            foreach (SystemLanguage language in Enum.GetValues(typeof(SystemLanguage)))
            {
                var content = new GUIContent(language.ToString());
                all.Add(content);
            }
            _selectedLanguageIndex = EditorGUILayout.Popup(new GUIContent("Language to add:"), _selectedLanguageIndex, all.ToArray());

            if (GUILayout.Button("Add Language", GUILayout.Height(30), GUILayout.Width(300)))
            {
                if (!_source.SelectedLanguages.Contains(_selectedLanguageIndex))
                {
                    _source.SelectedLanguages.Add(_selectedLanguageIndex);
                    _source.CurrentLanguageId = _selectedLanguageIndex;
                    AddTextEntriesWhenAddingALanguage(_selectedLanguageIndex);
                    Repaint();
                }
            }

            if (_source.CurrentLanguageId > -1)
            {
                GUILayout.Label("Current Language: " + (SystemLanguage)_source.CurrentLanguageId);
            }
            else
            {
                GUILayout.Label("Current Language: nothing");
            }
            

            for (var i = 0; i < _source.SelectedLanguages.Count; ++i)
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(5));
                rect.y += (int)(5 / 2f);
                rect.x -= 2;
                rect.height = 30;
                EditorGUI.DrawRect(rect, Color.grey);
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label(Enum.GetName(typeof(SystemLanguage), _source.SelectedLanguages[i]),GUILayout.Height(30));

                    if (GUILayout.Button("remove language", GUILayout.Height(20), GUILayout.Width(120)))
                    {
                        CheckTextEntriesWhenDeleteLanguage(_source.SelectedLanguages[i]);
                        _source.SelectedLanguages.Remove(_source.SelectedLanguages[i]);
                        if (_source.SelectedLanguages.Count != 0)
                        {
                            _source.CurrentLanguageId = _source.SelectedLanguages[_source.SelectedLanguages.Count - 1];
                        }
                        Repaint();
                    }

                    if (GUILayout.Button("make current", GUILayout.Height(20), GUILayout.Width(120)))
                    {
                        _source.CurrentLanguageId = _source.SelectedLanguages[i];
                        Repaint();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }

        private ValidateIdFailure ValidateIds()
        {
            var allIds = new List<string>();
            foreach (var entry in _source.Entries)
            {
                allIds.Add(entry.ShadowId);
                if (entry.ShadowId == String.Empty)
                {
                    return ValidateIdFailure.EMPTY_ID;
                }

                if (entry.ShadowId.Contains(' '))
                {
                    return ValidateIdFailure.ID_WITH_SPACE;
                }
            }
            if (allIds.Count != allIds.Distinct().Count())
            {
                return ValidateIdFailure.DUPLICATE_ID;
            }

            return ValidateIdFailure.OKAY;
        }

        private void ValidateTexts()
        {
            _areTextsValid = true;
            foreach (var entry in _source.Entries)
            {
                foreach (var text in entry.AllTexts)
                {
                    if (text.Text == String.Empty)
                    {
                        _areTextsValid = false;
                        break;
                    }
                }
            }
            Repaint();
        }
        
        private void SaveToCsv()
        {
            var path = EditorUtility.SaveFilePanel(
                "Save dataBase as csv",
                Application.dataPath,
                "textDatabase.csv",
                "csv");
            if (path.Length != 0)
            {
                using StreamWriter sw = File.CreateText(path);
                var indexString = string.Empty;
                for (var i = 0; i < _source.SelectedLanguages.Count; ++i)
                {
                    indexString += _source.SelectedLanguages[i].ToString();
                    if (i != _source.SelectedLanguages.Count - 1)
                    {
                        indexString += ";";
                    }
                }
                sw.WriteLine(indexString);
                var defaultLanguageIndexString = _source.CurrentLanguageId.ToString();
                sw.WriteLine(defaultLanguageIndexString);
                foreach (var entry in _source.Entries)
                {
                    var textString = String.Empty;
                    textString += entry.ShadowId + ";";
                    foreach (var text in entry.AllTexts)
                    {
                        textString += text.Text + ";";
                    }
                    sw.WriteLine(textString);
                }
            }
        }

        private void LoadFromCsv()
        {
            var path = EditorUtility.OpenFilePanel("Load dataBase from csv", Application.dataPath, "csv");
            if (path != String.Empty)
            {
                
                var dataBase = CreateInstance<TextDataBase>();
                AssetDatabase.CreateAsset(dataBase, "Assets/textDataBase.asset");
                var readSource = dataBase;
                AssetDatabase.SaveAssets();
                _textFoldout.Clear();
                using StreamReader sr = new StreamReader(path);
                var line = sr.ReadLine();
                if (line != null)
                {
                    var languageIndices = line.Split(';');
                    foreach (var index in languageIndices)
                    {
                        readSource.SelectedLanguages.Add(Int32.Parse(index));
                    }
                }

                line = sr.ReadLine();
                readSource.CurrentLanguageId = Int32.Parse(line);

                line = sr.ReadLine();
                while (line != null)
                {
                    var all = line.Split(';');
                    var entry = new TextEntry {ShadowId = all[0]};
                    for(var i = 0; i < readSource.SelectedLanguages.Count; ++i)
                    {
                        entry.AllTexts.Add(new StringLanguagePair(readSource.SelectedLanguages[i], all[i+1]));
                    }
                    readSource.Entries.Add(entry);
                    _textFoldout.Add(true);
                    line = sr.ReadLine();
                }

                _source = readSource;
                var allIds = readSource.Entries.Select(entry => entry.ShadowId).ToList();

                AssetDatabase.SaveAssets();
                EnumGenerator.Generate(allIds);
                for (var i = 0; i < _source.Entries.Count; ++i)
                {
                    _source.Entries[i].ID = (TextId) i;
                }
                Repaint();
            }
            
        }
        
        private void CheckTextEntriesWhenDeleteLanguage(int languageToDeleteIndex)
        {
            foreach (var entry in _source.Entries)
            {
                StringLanguagePair textToDelete = null;
                foreach (var text in entry.AllTexts)
                {
                    if (text.LanguagueIndex == languageToDeleteIndex)
                    {
                        textToDelete = text;
                    }
                }
                entry.AllTexts.Remove(textToDelete);
            }
        }

        private void AddTextEntriesWhenAddingALanguage(int languageToAddIndex)
        {
            foreach (var entry in _source.Entries)
            {
                entry.AllTexts.Add(new StringLanguagePair(languageToAddIndex));
            }
        }
        
        private static void DrawUiLine(Color color, int thickness = 2, int padding = 10)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            rect.height = thickness;
            rect.y += (int)(padding / 2f);
            rect.x -= 2;
            EditorGUI.DrawRect(rect, color);
        }
    }
}
