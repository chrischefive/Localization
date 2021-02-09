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
        
        
        [MenuItem("Window/Localization/Settings")]
        public static void ShowWindow()
        {
            EnumGenerator.Generate(null);
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
            }
            EditorGUILayout.EndHorizontal();

            DrawUiLine(new Color(0.57f, 0.57f, 0.57f), 4);
            
            if (_source)
            {
                ShowContent();
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
                ValidateIds();
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
                EnumGenerator.Generate(_source);
                
            }
            

            if (GUILayout.Button("Save to csv"))
            {
                SaveToCsv();
            }

            if (GUILayout.Button("Clear all"))
            {
                _source = null;
                Repaint();
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
                    _source.Entries.Add(newEntry);
                }
            }
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                foreach (var entry in _source.Entries)
                {
                    bool test = true;
                    test = EditorGUILayout.BeginFoldoutHeaderGroup(test, entry.ShadowId);
                    {
                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(5));
                        rect.y += (int) (5 / 2f);
                        rect.x -= 2;
                        rect.height = entry.AllTexts.Count * 20 + 28;
                        EditorGUI.DrawRect(rect, Color.grey);
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("ID:");
                            entry.ShadowId = EditorGUILayout.TextField(entry.ShadowId, GUILayout.Width(250));
                            if (GUILayout.Button("Delete ID", GUILayout.Width(80)))
                            {
                                _source.Entries.Remove(entry);
                                break;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        foreach (var text in entry.AllTexts)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.Label((SystemLanguage) text.LanguagueIndex + ":");
                                text.Text = EditorGUILayout.TextField(text.Text, GUILayout.Width(332));
                            }
                            EditorGUILayout.EndHorizontal();
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
                    _source.DefaultLanguageId = _selectedLanguageIndex;
                    AddTextEntriesWhenAddingALanguage(_selectedLanguageIndex);
                    Repaint();
                }
            }
           
            GUILayout.Label("Default Language: " + (SystemLanguage)_source.DefaultLanguageId);

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

                    if (GUILayout.Button("Remove Language", GUILayout.Height(20), GUILayout.Width(120)))
                    {
                        CheckTextEntriesWhenDeleteLanguage(_source.SelectedLanguages[i]);
                        _source.SelectedLanguages.Remove(_source.SelectedLanguages[i]);
                        _source.DefaultLanguageId = _source.SelectedLanguages[_source.SelectedLanguages.Count - 1];
                        Repaint();
                    }

                    if (GUILayout.Button("make default", GUILayout.Height(20), GUILayout.Width(120)))
                    {
                        _source.DefaultLanguageId = _source.SelectedLanguages[i];
                        Repaint();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }

        private void ValidateIds()
        {
            bool areIdsValid = true;
            var allIds = new List<string>();
            foreach (var entry in _source.Entries)
            {
                allIds.Add(entry.ShadowId);
                if (entry.ShadowId == String.Empty)
                {
                    areIdsValid = false;
                }
            }
            if (areIdsValid)
            {
                areIdsValid = allIds.Count == allIds.Distinct().Count();
            }

            if (!areIdsValid)
            {
                EditorUtility.DisplayDialog("Validate IDs", "Error in ids", "Okay");
            }
            else
            {
                EditorUtility.DisplayDialog("Validate IDs", "everything allright", "Okay");
            }
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
            string path = Application.dataPath + "/textDatabase.csv";
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
