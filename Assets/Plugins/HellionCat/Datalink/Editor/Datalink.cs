using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;
using System.Text;
using System.Linq;

namespace HellionCat.DataLink
{
    public class Datalink : EditorWindow
    {
        #region variables
        #region const
        /// <summary>
        /// This is the link to the original copy of the google app scripts.
        /// DO NOT MODIFY THIS, or else you will break the install button.
        /// </summary>
        private const string OriginGoogleScriptsAppUrl = "https://script.google.com/d/1GQw6tctCmyhfXIEFsDVuVqGHnZJs4JV6wFFY4X8ciBFWHoTGnWKo6Dsq/newcopy";
        /// <summary>
        /// Delimiter used between column when sending or receiving data from the google sheets
        /// </summary>
        private const string DriveColumnDelimiter = "|cl|";
        /// <summary>
        /// Delimiter used between row when sending or receiving data from the google sheets
        /// </summary>
        private const string DriveLineDelimiter = "|rw|";
        /// <summary>
        /// Delimiter used between column for the csv
        /// </summary>
        private const string CSVColumnDelimiter = ";";
        /// <summary>
        /// Delimiter used between row for the csv
        /// </summary>
        private const string CSVLineDelimiter = "/n";
        /// <summary>
        /// Delimiter used between sheets when sending or receiving data from the google sheets
        /// </summary>
        public const string SheetDelimiter = "|sh|";
        /// <summary>
        /// Delimiter used between the sheet name and data when sending or receiving data from the google sheets
        /// </summary>
        public const string SheetDataDelimiter = "|shD|";
        /// <summary>
        /// The width of a file rect in the editor window
        /// </summary>
        public const int CaseWidth = 100;
        /// <summary>
        /// The header to write in the google sheets
        /// </summary>
        public const string DriveHeader = "Property|cl|Type|cl|Value(s)";
        /// <summary>
        /// The header to write in the csv
        /// </summary>
        public const string CSVHeader = "Property;Type;Value(s)";
        #endregion
        /// <summary>
        /// Filter used to get the files in the asset folder
        /// </summary>
        private readonly string[] m_filters = { "Assets" };
        /// <summary>
        /// Titles of the toolbar
        /// </summary>
        private readonly string[] m_toolBar = { "Local","Drive" };
        /// <summary>
        /// Array with all information about the scriptable objects of the project
        /// </summary>
        [SerializeField] private ObjectData[] m_objects;
        /// <summary>
        /// A file containing the configuration data of this tool
        /// </summary>
        [SerializeField] private TextAsset m_config;
        /// <summary>
        /// The url of the google scripts app
        /// </summary>
        [SerializeField] private string m_url;
        /// <summary>
        /// The path of the folder than contain the CSVs files
        /// </summary>
        [SerializeField] private string m_csvPath;
        /// <summary>
        /// The current scroll position for the scroll view
        /// </summary>
        private Vector2 m_scroll;
        /// <summary>
        /// The string containing the content of the search bar
        /// </summary>
        private string m_search;
        /// <summary>
        /// The number of frame to wait before saving changes in the config file
        /// </summary>
        private float m_timer;
        /// <summary>
        /// Determine if the request "Verify installation" has been launched
        /// </summary>
        private static bool s_testing;
        /// <summary>
        /// Prevent some action if we are waiting for a callback
        /// </summary>
        private static bool s_waitingCallback;
        /// <summary>
        /// Result of the request "Verify installation"
        /// </summary>
        private int m_testResult = -1;
        /// <summary>
        /// Sprite used for representing a scriptable in the editor
        /// </summary>
        public static Sprite FileIcon;
        /// <summary>
        /// USed to know if there is duplicate name for the scripables
        /// </summary>
        private bool m_isDuplicateName;
        /// <summary>
        /// The duplicated name
        /// </summary>
        private string m_duplicateName = "";
        /// <summary>
        /// The type of message we are sending to the user
        /// </summary>
        private static MessageType s_messageType;
        /// <summary>
        /// Additional information to send with the message
        /// </summary>
        private static string s_additionalInformation = "";
        /// <summary>
        /// A style used to get centered label in the editor
        /// </summary>
        private GUIStyle m_centerLabel;
        /// <summary>
        /// The name of the config file
        /// </summary>
        private const string ConfigFileName = "HC_dlink_config";
        /// <summary>
        /// The name of the icon file
        /// </summary>
        private const string FileIconName = "file_icon";
        
        public static bool WaitingCallback => s_waitingCallback;
        public static bool SendInDrive = true;
        #endregion

        /// <summary>
        /// Show the editor window
        /// </summary>
        [MenuItem("Window/HellionCat/DataLink")]
        public static void ShowWindow()
        {
            GetWindow(typeof(Datalink),false, "DataLink");
        }

        /// <summary>
        /// Initialize the editor
        /// </summary>
        private void OnEnable()
        {
            SendInDrive = PlayerPrefs.GetInt("Datalink_OnDrive", 1) == 1;
            minSize = new Vector2(400, 350);
            FileIcon = Resources.Load<Sprite>(FileIconName);
            //We check if it's the first time you open the window
            if (m_config || !string.IsNullOrEmpty(m_url))
            {
                if (m_objects == null || m_objects.Length <= 0)
                {
                    SetMessage(MessageType.Empty);
                }
                return;
            }
            Reload_SO();
            //Next, we use the config file to load the url string
            if (!m_config && string.IsNullOrEmpty(m_url))
            {
                m_config = Resources.Load<TextAsset>(ConfigFileName);
                //if the config file does not exist, we are creating it
                if (!m_config)
                {
                    var l_path = Application.dataPath + "/Resources";
                    if (!Directory.Exists(l_path))
                    {
                        Directory.CreateDirectory(l_path);
                    }

                    var l_data = new List<string> {"",""};
                    for (var l_i = 0; l_i < m_objects.Length; l_i++)
                    {
                        l_data.Add($"{m_objects[l_i].m_itemId};{m_objects[l_i].m_sendOnline}");
                    }

                    l_path += $"/{ConfigFileName}.txt";
                    File.WriteAllLines(l_path, l_data.ToArray());
                    AssetDatabase.Refresh();
                    m_config = Resources.Load<TextAsset>(ConfigFileName);
                }
                var l_configData = m_config.text.Split('\n');
                m_url = l_configData[0];
                m_csvPath = l_configData[1];
                //Once the file has been loaded/created, we can set up which file should be sent or not
                if (m_objects != null)
                {
                    for (var l_i = 2; l_i < l_configData.Length; l_i++)
                    {
                        var l_found = false;
                        var l_split = l_configData[l_i].Split(';');
                        for (var l_j = 0; l_j < m_objects.Length && !l_found; l_j++)
                        {
                            if (m_objects[l_j].m_itemId == l_split[0])
                            {
                                m_objects[l_j].m_sendOnline = l_split[1].Contains("True");
                                l_found = true;
                            }
                        }
                    }
                }
            }
            m_timer = 50;
        }

        /// <summary>
        /// Save the config file when closing the editor
        /// </summary>
        private void OnDisable()
        {
            SaveConfig();
        }

        /// <summary>
        /// Handle the drawing of the editor on screen
        /// </summary>
        private void OnGUI()
        {
            if (!GUI.skin || GUI.skin.horizontalSlider == null || !m_config)
                return;
            if (m_centerLabel == null)
                m_centerLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            //If the game is playing, the tool is blocked to avoid issues
            if (Application.isPlaying)
            {
                var l_gui = EditorStyles.label.normal.textColor;
                EditorStyles.label.normal.textColor = new Color(0.9f, 0.45f, 0);
                EditorGUILayout.LabelField("You can't use this tool while playing");
                EditorStyles.label.normal.textColor = l_gui;
                return;
            }
            //If your testing the link or waiting a callback, the editor prevent any modifications too
            if (s_testing || s_waitingCallback)
                GUI.enabled = false;

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            var l_previous = SendInDrive;
            SendInDrive = GUILayout.Toolbar((SendInDrive)?1:0, m_toolBar) ==1;
            if(l_previous!=SendInDrive)
                GUI.FocusControl(null);

            //This part manage the first section of the tool : the online set up
            if (SendInDrive)
            {
                #region webLink
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Web service URL", GUILayout.Width(105));
                m_url = EditorGUILayout.TextField("", m_url);
                if (m_testResult >= 0)
                {
                    var l_gui = EditorStyles.label.normal.textColor;
                    EditorStyles.label.normal.textColor = (m_testResult == 0) ? Color.red : new Color(0, 0.6f, 0);
                    EditorGUILayout.LabelField((m_testResult == 0) ? "X" : "V", GUILayout.Width(20));
                    EditorStyles.label.normal.textColor = l_gui;
                }
                EditorGUILayout.EndHorizontal();
                if (m_url.Trim() != m_config.text.Split('\n')[0].Trim())
                {
                    m_timer -= Time.deltaTime;
                    if (m_timer < 0)
                    {
                        SaveConfig();
                        m_timer += 50;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Install the google apps script"))
                {
                    Application.OpenURL(OriginGoogleScriptsAppUrl);
                }
                if (GUILayout.Button("Verify the installation"))
                {
                    s_testing = true;
                    SetMessage(MessageType.Loading);
                    Bridge.GetLastModificationTime(m_url, TestCb);
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
            #endregion
            }
            else
            {
                #region local
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("CSV Folder", GUILayout.Width(75));
                m_csvPath = EditorGUILayout.TextField(m_csvPath);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                    m_csvPath = EditorUtility.OpenFolderPanel("Folder", (string.IsNullOrEmpty(m_csvPath)) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) :
                                                                m_csvPath, "");
                EditorGUILayout.EndHorizontal();
                #endregion
            }
            DrawUiLine(new Color(0.57f, 0.57f, 0.57f), 4);
            
            //If there is no url, we don't display the next parts
            if ((SendInDrive && string.IsNullOrWhiteSpace(m_url) )|| (!SendInDrive && string.IsNullOrWhiteSpace(m_csvPath)))
            {
                EditorGUILayout.EndVertical();
                GUI.enabled = true;
                return;
            }

            var l_totalAmountToDisplay = 0;
            //This section manage the search part
            if (s_messageType != MessageType.Empty && m_objects!=null && m_objects.Length > 0)
            {
                #region search
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search", GUILayout.Width(50));
                var l_searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);
                l_searchStyle.margin = new RectOffset(l_searchStyle.margin.left, l_searchStyle.margin.right, l_searchStyle.margin.top, l_searchStyle.margin.bottom);
                var l_search = EditorGUILayout.TextField("", m_search, l_searchStyle);

                if (!string.IsNullOrEmpty(l_search) && !l_search.Equals(m_search))
                {
                    m_search = l_search;
                    l_totalAmountToDisplay = m_objects.Count(p_o =>
                        string.IsNullOrEmpty(m_search) || p_o.m_object.name.ToLower().Contains(m_search.ToLower()));
                }
                else
                    l_totalAmountToDisplay = m_objects.Length;
                

                if (!string.IsNullOrEmpty(l_search))
                {                
                    if (GUILayout.Button("", new GUIStyle("ToolbarSeachCancelButton")))
                    {
                        // Remove the focus if cleared
                        m_search = "";
                        GUI.FocusControl(null);
                    }
                }
                else
                {         
                    if (GUILayout.Button("", new GUIStyle("ToolbarSeachCancelButtonEmpty")))
                    {
                        // Remove the focus if cleared
                        m_search = "";
                        GUI.FocusControl(null);
                    }
                }
                EditorGUILayout.EndHorizontal();
                DrawUiLine(new Color(0.57f, 0.57f, 0.57f), 4);
                #endregion
            }

            //This section display all the Scriptable Objects of the project 
            #region content
            var l_size = EditorGUIUtility.currentViewWidth - 20;
            var l_colNumb = (int)(l_size / (CaseWidth * 1.5f));
            if (l_colNumb < 1)
                l_colNumb = 1;
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            var l_colAmount = l_totalAmountToDisplay > l_colNumb ? l_colNumb : l_totalAmountToDisplay;
            l_totalAmountToDisplay -= l_colAmount;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth((CaseWidth * l_colAmount) * 1.5f));
            var l_count = 0;
            if (!s_testing && (m_objects != null && m_objects.Length > 0))
            {
                for (var l_i = 0; l_i < m_objects.Length; l_i++)
                {
                    var l_increase = false;
                    if (m_objects[l_i].m_object != null)
                    {
                        if (string.IsNullOrEmpty(m_search) || m_objects[l_i].m_object.name.ToLower().Contains(m_search.ToLower()))
                        {
                            try
                            {
                                m_objects[l_i].Display((SendInDrive) ? m_url.Trim() : m_csvPath);
                                l_count++;
                                l_increase = true;
                            }
                            catch
                            {
                                // Empty try catch to avoid an error in the editor when a scriptable is deleted between the gui calls
                            }
                        }
                    }
                    else
                    {
                        //If an object has been deleted, we refresh the list
                        Reload_SO();
                        break;
                    }
                    if (l_count > 0 && l_increase && (l_count) % l_colNumb == 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        l_colAmount = l_totalAmountToDisplay > l_colNumb ? l_colNumb : l_totalAmountToDisplay;
                        l_totalAmountToDisplay -= l_colAmount;
                        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth((CaseWidth * l_colAmount) * 1.5f));
                        if (l_colAmount == 0)
                        {
                            break;
                        }
                    }
                }
                GUI.enabled = !s_waitingCallback;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            #endregion
            DrawUiLine(new Color(0.57f, 0.57f, 0.57f), 4);

            //This section display a message if you have two or more objects with the same name
            #region Duplicate
            if (m_isDuplicateName)
            {
                var l_mColor = EditorStyles.label.normal.textColor;
                m_centerLabel.normal.textColor = Color.red;
                EditorGUILayout.LabelField($"2 or more objects in the list have the name {m_duplicateName}.",
                    m_centerLabel);
                EditorGUILayout.LabelField("We highly recommended to rename one of them to avoid problem.", m_centerLabel);
                m_centerLabel.normal.textColor = l_mColor;
            }
            #endregion
            //This section display some feedback to the user about the progression of the last request, if there are no objects in the project, ...
            #region message
            if (s_messageType != MessageType.None)
            {
                var l_mColor = EditorStyles.label.normal.textColor;
                switch (s_messageType)
                {
                    case MessageType.Success:
                        m_centerLabel.normal.textColor = new Color(0, 0.6f, 0);
                        break;
                    case MessageType.Timeout:
                        m_centerLabel.normal.textColor = new Color(0.8f, 0.4f, 0);
                        break;
                    case MessageType.Fail:
                        m_centerLabel.normal.textColor = Color.red;
                        break;
                    case MessageType.Loading:
                        m_centerLabel.normal.textColor = Color.blue;
                        break;
                    case MessageType.Empty:
                        m_centerLabel.normal.textColor = new Color(0.8f, 0.4f, 0);
                        break;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(s_additionalInformation, m_centerLabel);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    s_messageType = MessageType.None;
                    s_additionalInformation = "";
                }
                EditorGUILayout.EndHorizontal();
                m_centerLabel.normal.textColor = l_mColor;
            }
            #endregion
            //This section display some global options, like refreshing the window, sending all scriptable objects or loading them all
            #region bottom button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh the asset list"))
            {
                SaveConfig();
                Reload_SO();
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            if (s_messageType == MessageType.Empty)
                GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("Load all"))
            {
                LoadAll();
            }
            if (GUILayout.Button((SendInDrive)? "Send all":"Save all"))
            {
                SendAll();
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            #endregion
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            GUI.enabled = true;
        }

        /// <summary>
        /// Write the configs to the disk
        /// </summary>
        private void SaveConfig()
        {
            PlayerPrefs.SetInt("Datalink_OnDrive", (SendInDrive)?1:0);
            if (!m_config || m_objects == null)
                return;
            var l_data = new List<string> {m_url,m_csvPath};
            for (var l_i = 0; l_i < m_objects.Length; l_i++)
            {
                l_data.Add($"{m_objects[l_i].m_itemId};{m_objects[l_i].m_sendOnline}");
            }
            File.WriteAllLines(AssetDatabase.GetAssetPath(m_config), l_data.ToArray());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Reload the scriptable objects to refresh to editor
        /// </summary>
        private void Reload_SO()
        {
            GUI.FocusControl(null);
            m_isDuplicateName = false;
            m_objects = null;
            //First, we find all ScriptableObject
            var l_itemsId = AssetDatabase.FindAssets("t:ScriptableObject", m_filters);
            //And if we dont find anything, we stop here and display a message
            if (l_itemsId == null || l_itemsId.Length <= 0)
            {
                Debug.LogWarning("They're no ScriptableObject in the projet");
                SetMessage(MessageType.Empty);
                return;
            }
            if (s_messageType == MessageType.Empty)
                s_messageType = MessageType.None;
            //We check if there are some objects with the same name
            m_objects = new ObjectData[l_itemsId.Length];

            for (var l_i = 0; l_i < m_objects.Length; l_i++)
            {
                m_objects[l_i] = new ObjectData(l_itemsId[l_i]);
                if (l_i > 0)
                {
                    for (var l_j = 0; l_j < l_i; l_j++)
                    {
                        if (m_objects[l_i].m_object.name.Equals(m_objects[l_j].m_object.name))
                        {
                            //If there are doubles, we deactivate the objects
                            m_objects[l_i].m_duplicate = true; m_objects[l_i].m_sendOnline = false;
                            m_objects[l_j].m_duplicate = true; m_objects[l_j].m_sendOnline = false;
                            //and then display a message with the name of the first double
                            if (!m_isDuplicateName)
                            {
                                m_isDuplicateName = true;
                                m_search = m_objects[l_i].m_object.name;
                                m_duplicateName = m_objects[l_i].m_object.name;
                            }
                        }
                    }
                }
            }

            //If the config file has been read, we set up which file should be sent or not
            if (m_config)
            {
                var l_configData = m_config.text.Split('\n');
                m_url = l_configData[0];
                m_csvPath = l_configData[1];

                for (var l_i = 2; l_i < l_configData.Length; l_i++)
                {
                    var l_found = false;
                    var l_split = l_configData[l_i].Split(';');
                    for (var l_j = 0; l_j < m_objects.Length && !l_found; l_j++)
                    {
                        if (m_objects[l_j].m_itemId == l_split[0])
                        {
                            m_objects[l_j].m_sendOnline = (!m_objects[l_j].m_duplicate) && l_split[1].Contains("True");
                            l_found = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Automatically repaint the editor ~10 times per seconds,
        /// Avoiding the need for the user to move te mouse to repaint it.
        /// </summary>
        private void OnInspectorUpdate()
        {
            if(m_objects != null && m_objects.Length>0)
                Repaint();
        }

        /// <summary>
        /// Draw a line in the ui
        /// </summary>
        /// <param name="p_color">The color of the line</param>
        /// <param name="p_thickness">The thickness of the line</param>
        /// <param name="p_padding">the padding before and after the line</param>
        private static void DrawUiLine(Color p_color, int p_thickness = 2, int p_padding = 10)
        {
            var l_r = EditorGUILayout.GetControlRect(GUILayout.Height(p_padding + p_thickness));
            l_r.height = p_thickness;
            l_r.y += (int)(p_padding / 2f);
            l_r.x -= 2;
            EditorGUI.DrawRect(l_r, p_color);
        }

        /// <summary>
        /// Send all the scriptable object to the google scripts app
        /// If a search has been done, only send the result of it
        /// </summary>
        private void SendAll()
        {
            GUI.FocusControl(null);
            AssetDatabase.SaveAssets();

            if (SendInDrive)
            {
                var l_result = new StringBuilder("");
                var l_number = 0;
                for (var l_i = 0; l_i < m_objects.Length; l_i++)
                {
                    if (m_objects[l_i].m_sendOnline && (string.IsNullOrEmpty(m_search) || m_objects[l_i].m_object.name.ToLower().Contains(m_search.ToLower())))
                    {
                        if (l_number == 0)
                            l_result.AppendFormat("{0}{1}{2}", m_objects[l_i].m_object.name, SheetDataDelimiter, m_objects[l_i].ConvertObjectIntoSingleString());
                        else
                            l_result.AppendFormat("{0}{1}{2}{3}", SheetDelimiter, m_objects[l_i].m_object.name, SheetDataDelimiter, m_objects[l_i].ConvertObjectIntoSingleString());
                        l_number++;
                        m_objects[l_i].m_waitingCallback = true;
                    }
                }
                if (l_number > 0)
                {
                    s_waitingCallback = true;
                    SetMessage(MessageType.Loading, "Loading, this operation may take a while...");
                    Bridge.SetAllSpreadsheetData(m_url, SaveAllCb, l_result.ToString(), l_number * 15);
                }
                else
                    SetMessage(MessageType.Timeout, "The selection is empty");
            }
            else
            {
                if(m_objects == null || m_objects.Length<=0)
                {
                    SetMessage(MessageType.Timeout, "The selection is empty");
                    return;
                }
                for (var l_i = 0; l_i < m_objects.Length; l_i++)
                {
                    if (m_objects[l_i].m_sendOnline && (string.IsNullOrEmpty(m_search) || m_objects[l_i].m_object.name.ToLower().Contains(m_search.ToLower())))
                        m_objects[l_i].Send(m_csvPath);
                }
            }
        }

        /// <summary>
        /// Handle the response from save all
        /// </summary>
        /// <param name="p_obj">The result of the request</param>
        private void SaveAllCb(SendDataInfo p_obj)
        {
            for (var l_i = 0; l_i < m_objects.Length; l_i++)
            {
                m_objects[l_i].m_waitingCallback = false;
            }
            s_waitingCallback = false;
            SetMessage(p_obj.success, p_obj.timedOut);
            SaveConfig();
        }

        /// <summary>
        /// Load all the scriptable object from the google scripts app
        /// If a search has been done, only load the found scriptable object
        /// </summary>
        private void LoadAll()
        {
            GUI.FocusControl(null);
            if (SendInDrive)
            {
                var l_data = new StringBuilder("");
                var l_number = 0;
                for (var l_i = 0; l_i < m_objects.Length; l_i++)
                {
                    if (m_objects[l_i].m_sendOnline && (string.IsNullOrEmpty(m_search) || m_objects[l_i].m_object.name.ToLower().Contains(m_search.ToLower())))
                    {
                        l_number++;
                        m_objects[l_i].m_waitingCallback = true;
                        l_data.AppendFormat("{0}{1}", m_objects[l_i].m_object.name, SheetDelimiter);
                    }
                }
                if (l_number > 0)
                {
                    s_waitingCallback = true;
                    SetMessage(MessageType.Loading, "Loading, this operation may take a while...");
                    Bridge.GetAllSpreadsheetData(m_url, LoadAllCb, l_data.ToString(), 15 * l_number);
                }
                else
                {
                    SetMessage(MessageType.Timeout, "The selection is empty");
                }
            }
            else
            {
                if (m_objects == null || m_objects.Length <= 0)
                {
                    SetMessage(MessageType.Timeout, "The selection is empty");
                    return;
                }
                for (var l_i = 0; l_i < m_objects.Length; l_i++)
                {
                    if (m_objects[l_i].m_sendOnline && (string.IsNullOrEmpty(m_search) || m_objects[l_i].m_object.name.ToLower().Contains(m_search.ToLower())))
                        m_objects[l_i].Load(m_csvPath);
                }
            }
        }

        /// <summary>
        /// Handle the response from load all
        /// </summary>
        /// <param name="p_obj">The result of the request</param>
        private void LoadAllCb(GetDataInfo p_obj)
        {
            if (!p_obj.success || p_obj.timedOut)
            {
                s_waitingCallback = false;
                if (p_obj.timedOut)
                    SetMessage(MessageType.Timeout);
                else
                    SetMessage(MessageType.Fail, p_obj.data);
                return;
            }
            if (string.IsNullOrEmpty(p_obj.data))
            {
                s_waitingCallback = false;
                SetMessage(MessageType.Fail, "nothing to read");
                return;
            }

            var l_data = p_obj.data.Split(new[] { SheetDelimiter }, StringSplitOptions.None);
            for (var l_i = 0; l_i < l_data.Length; l_i++)
            {
                var l_line = l_data[l_i].Split(new[] { SheetDataDelimiter }, StringSplitOptions.None);

                for (var l_j = 0; l_j < m_objects.Length; l_j++)
                {
                    if (m_objects[l_j].m_object.name.Equals(l_line[0]))
                    {
                        m_objects[l_j].LoadCallback(l_line[1]);
                        break;
                    }
                }
            }
            s_waitingCallback = false;
            SetMessage(MessageType.Success);
            SaveConfig();
        }

        /// <summary>
        /// Handle the response from a get last update time request
        /// </summary>
        /// <param name="p_ti">The result of the request</param>
        private void TestCb(GetTimeInfo p_ti)
        {
            s_testing = false;
            m_testResult = (p_ti.timedOut) ? 0 : 1;
            s_waitingCallback = false;
            SetMessage(p_ti.success, p_ti.timedOut);
            SaveConfig();
        }


        public static string GetLineDelimiter(bool p_drive = true)
        {
            return (p_drive) ? DriveLineDelimiter : CSVLineDelimiter;
        }

        public static string GetColumnDelimiter(bool p_drive = true)
        {
            return (p_drive) ? DriveColumnDelimiter : CSVColumnDelimiter;
        }

        /// <summary>
        /// Ensure to save the config file when the project has been changed
        /// </summary>
        private void OnProjectChange()
        {
            if (!string.IsNullOrEmpty(m_url))
                SaveConfig();
        }

        /// <summary>
        /// Ensure to save the config file when the selection has been changed
        /// </summary>
        private void OnSelectionChange()
        {
            if (!string.IsNullOrEmpty(m_url))
                SaveConfig();
        }

        /// <summary>
        /// Display a message in the editor
        /// </summary>
        /// <param name="p_success">Has the connexion succeeded ?</param>
        /// <param name="p_timeout">Has the request timed out ?</param>
        /// <param name="p_additional">Eventual additional info to display on the editor</param>
        public static void SetMessage(bool p_success, bool p_timeout, string p_additional = "")
        {
            if (p_success)
                SetMessage(MessageType.Success, p_additional);
            else if (p_timeout)
                SetMessage(MessageType.Timeout, p_additional);
            else
                SetMessage(MessageType.Fail, p_additional);
        }

        /// <summary>
        /// Display a message in the editor
        /// </summary>
        /// <param name="p_message">The type of the message</param>
        /// <param name="p_additional">Eventual additional info to display on the editor</param>
        public static void SetMessage(MessageType p_message, string p_additional = "")
        {
            s_messageType = p_message;
            s_additionalInformation = p_additional;
            if (string.IsNullOrEmpty(p_additional))
            {
                switch (p_message)
                {
                    case MessageType.Success:
                        s_additionalInformation = "Success";
                        break;
                    case MessageType.Timeout:
                        s_additionalInformation = "Timeout";
                        break;
                    case MessageType.Fail:
                        break;
                    case MessageType.Loading:
                        s_additionalInformation = "Loading";
                        break;
                    case MessageType.Empty:
                        s_additionalInformation = "They're no ScriptableObject in the project";
                        break;
                }
            }
        }
    }

    /// <summary>
    /// This class hold data about the ScriptableObject
    /// </summary>
    [Serializable]
    public class ObjectData
    {
        #region variables
        /// <summary>
        /// The object to synchronize
        /// </summary>
        public ScriptableObject m_object;
        /// <summary>
        /// The serialized object linked to the scriptable object
        /// </summary>
        private SerializedObject m_serializedObject;
        /// <summary>
        /// The itemId of the Object
        /// </summary>
        public string m_itemId;
        /// <summary>
        /// Whether the object is to be synchronized
        /// </summary>
        public bool m_sendOnline = true;
        /// <summary>
        /// Block the options about the file if a request is pending
        /// </summary>
        public bool m_waitingCallback;
        /// <summary>
        /// Whether this scriptable object has a duplicate name
        /// </summary>
        public bool m_duplicate;
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_id">The id of the scriptable object</param>
        public ObjectData(string p_id)
        {
            m_itemId = p_id;
            m_object = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(p_id));
            m_serializedObject = new SerializedObject(m_object);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_object">The scriptable object to represent</param>
        public ObjectData(ScriptableObject p_object)
        {
            m_object = p_object;
            m_serializedObject = new SerializedObject(m_object);
        }
        #endregion

        /// <summary>
        /// Display the information and options for this object 
        /// </summary>
        /// <param name="p_url">The url of the google apps script</param>
        public void Display(string p_url)
        {
            //If we are waiting for a callback, we are preventing modification
            GUI.enabled = !m_waitingCallback && !m_duplicate && !Datalink.WaitingCallback;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(GUILayout.Width(Datalink.CaseWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
            var l_guiStyle = new GUIStyle(EditorStyles.label) {fontStyle = FontStyle.Bold, wordWrap = true};
            m_sendOnline = EditorGUILayout.ToggleLeft(m_object.name, m_sendOnline, l_guiStyle, GUILayout.Width(Datalink.CaseWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.75f));

            EditorGUILayout.EndHorizontal();
            //Button used to access the files
            if (GUILayout.Button(new GUIContent(Datalink.FileIcon.texture), GetBtnStyle(), GUILayout.Width(Datalink.CaseWidth), GUILayout.Height(Datalink.CaseWidth)))
                Selection.activeObject = m_object;

            //If we are waiting for a response from a request or if there are any duplicate, we are aborting
            if (m_waitingCallback || m_duplicate)
            {
                var l_gui = EditorStyles.label.normal.textColor;
                EditorStyles.label.normal.textColor = (m_duplicate) ? Color.red : new Color(0.8f, 0.4f, 0);
                EditorGUILayout.LabelField((m_duplicate) ? "Duplicate name" : "Communication", GUILayout.Width(100));
                EditorStyles.label.normal.textColor = l_gui;
            }
            //else, we are displaying the buttons
            else
            {
                GUI.enabled = m_sendOnline && !Datalink.WaitingCallback;
                EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
                if (GUILayout.Button("Load"))
                {
                    Load(p_url);
                }
                if (GUILayout.Button((Datalink.SendInDrive)?"Send":"Save"))
                {
                    Send(p_url);
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = true;
            EditorGUILayout.EndVertical();

        }

        /// <summary>
        /// Display the information and options for this object 
        /// </summary>
        /// <param name="p_url">The url of the google apps script</param>
        public void DisplayInObject(string p_url)
        {
            GUI.enabled = !m_waitingCallback;
            EditorGUILayout.LabelField("Sync with google");

            if (m_waitingCallback)
            {
                var l_gui = EditorStyles.label.normal.textColor;
                EditorStyles.label.normal.textColor = new Color(0.9f, 0.45f, 0);
                EditorGUILayout.LabelField("Communication");
                EditorStyles.label.normal.textColor = l_gui;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                Load(p_url);
            }
            if (GUILayout.Button("Send"))
            {
                Send(p_url);
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        #region Send
        
        /// <summary>
        /// Handle the result of a send request
        /// </summary>
        /// <param name="p_data">the result of the request</param>
        public void SendCallback(SendDataInfo p_data)
        {
            m_waitingCallback = false;
            Datalink.SetMessage(p_data.success, p_data.timedOut);
        }

        /// <summary>
        /// Convert the properties of the object into a simple string and send them to the google apps scripts
        /// </summary>
        public void Send(string p_url)
        {
            AssetDatabase.SaveAssets();

            if (Datalink.SendInDrive)
            {
                Datalink.SetMessage(MessageType.Loading);
                Bridge.SetSpreadsheetData(p_url, SendCallback, ConvertObjectIntoSingleString(), m_object.name);

                m_waitingCallback = true;
            }
            else
            {
                string l_path = Path.Combine(p_url,$"{m_object.name}.csv");
                File.WriteAllLines(l_path, ConvertObjectIntoStringArray());
                Datalink.SetMessage(MessageType.Success);
            }
        }

        /// <summary>
        /// Convert the properties of the object into a simple string
        /// </summary>
        /// <returns>The properties in a string format</returns>
        public string ConvertObjectIntoSingleString()
        {
            m_serializedObject = new SerializedObject(m_object);
            var l_data = ConvertObjectIntoStringArray();
            var l_result = new StringBuilder(l_data[0]);
            for (var l_i = 1; l_i < l_data.Length; l_i++)
            {
                l_result.AppendFormat("{1}{0}", l_data[l_i], Datalink.GetLineDelimiter(Datalink.SendInDrive));
            }

            return l_result.ToString();
        }

        /// <summary>
        /// Convert the properties of the object into an array of strings
        /// </summary>
        /// <returns>The properties in a string array format</returns>
        public string[] ConvertObjectIntoStringArray()
        {
            if(m_serializedObject == null)
                m_serializedObject = new SerializedObject(m_object);
            var l_data = new List<string> {(Datalink.SendInDrive)?Datalink.DriveHeader:Datalink.CSVHeader};
            var l_sp = m_serializedObject.GetIterator().Copy();
            l_sp.NextVisible(true);
            do
            {
                var l_range = ConvertPropertyIntoString(l_sp);
                if (l_range != null)
                    l_data.AddRange(l_range);
            } while (l_sp.NextVisible(false));

            return l_data.ToArray();
        }

        /// <summary>
        /// Convert the data of the property into a string (or an array) depending to its type 
        /// </summary>
        /// <param name="p_property">The property to convert</param>
        /// <returns>The properties in a string format</returns>
        private string[] ConvertPropertyIntoString(SerializedProperty p_property)
        {
            var l_data = new List<string>();
            var l_result = new StringBuilder();
            l_result.AppendFormat("{0}{2}{1}{2}", p_property.name, p_property.propertyType.ToString(),
                Datalink.GetColumnDelimiter(Datalink.SendInDrive));

            switch (p_property.propertyType)
            {
                case SerializedPropertyType.Generic:
                    if (p_property.isArray)
                    {
                        if (p_property.arraySize > 0 && ConvertPropertyIntoString(p_property.GetArrayElementAtIndex(0)) == null)
                            return null;
                        l_result = new StringBuilder().AppendFormat("{0}{3}{1}{3}{2}", p_property.name, "array", p_property.arraySize, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    }
                    else
                    {
                        l_result = new StringBuilder().AppendFormat("{0}{2}{1}", p_property.name, p_property.propertyType.ToString(), Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    }
                    break;
                case SerializedPropertyType.Integer:
                    l_result.Append(p_property.intValue.ToString());
                    break;
                case SerializedPropertyType.Boolean:
                    l_result.Append(p_property.boolValue.ToString());
                    break;
                case SerializedPropertyType.Float:
                    l_result.Append(p_property.floatValue.ToString(CultureInfo.InvariantCulture).Replace('.', ','));
                    break;
                case SerializedPropertyType.String:
                    l_result.Append(p_property.stringValue);
                    break;
                case SerializedPropertyType.Color:
                    l_result.AppendFormat("{0}{4}{1}{4}{2}{4}{3}", p_property.colorValue.r, p_property.colorValue.g, p_property.colorValue.b, p_property.colorValue.a, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.ObjectReference:
                    return null;
                case SerializedPropertyType.LayerMask:
                    return null;
                case SerializedPropertyType.Enum:
                    l_result.Append(p_property.enumValueIndex.ToString());
                    break;
                case SerializedPropertyType.Vector2:
                    l_result.AppendFormat("{0}{2}{1}", p_property.vector2Value.x, p_property.vector2Value.y, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.Vector3:
                    l_result.AppendFormat("{0}{3}{1}{3}{2}", p_property.vector3Value.x, p_property.vector3Value.y, p_property.vector3Value.z, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.Vector4:
                    l_result.AppendFormat("{0}{4}{1}{4}{2}{4}{3}", p_property.vector4Value.x, p_property.vector4Value.y, p_property.vector4Value.z, p_property.vector4Value.w, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.Rect:
                    l_result.AppendFormat("{0}{4}{1}{4}{2}{4}{3}", p_property.rectValue.x, p_property.rectValue.y, p_property.rectValue.width, p_property.rectValue.height, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.ArraySize:
                    l_result.Append(p_property.arraySize.ToString());
                    break;
                case SerializedPropertyType.Character:
                    return null;
                case SerializedPropertyType.AnimationCurve:
                    l_result = new StringBuilder().AppendFormat("{0}{2}{1}", p_property.name, p_property.propertyType.ToString(), Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    for (var l_i = 0; l_i < p_property.animationCurveValue.length; l_i++)
                    {
                        l_result.AppendFormat("{6}{0}_{1}_{2}_{3}_{4}_{5}", p_property.animationCurveValue.keys[l_i].time, p_property.animationCurveValue.keys[l_i].value,
                                                                                        p_property.animationCurveValue.keys[l_i].inTangent, p_property.animationCurveValue.keys[l_i].outTangent,
                                                                                        p_property.animationCurveValue.keys[l_i].inWeight, p_property.animationCurveValue.keys[l_i].outWeight, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    }
                    break;
                case SerializedPropertyType.Bounds:
                    l_result.AppendFormat("{0}{6}{1}{6}{2}{6}{3}{6}{4}{6}{5}", p_property.boundsValue.center.x, p_property.boundsValue.center.y, p_property.boundsValue.center.z,
                        p_property.boundsValue.size.x, p_property.boundsValue.size.y, p_property.boundsValue.size.z, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.Gradient:
                    return null;
                case SerializedPropertyType.Quaternion:
                    l_result.AppendFormat("{0}{4}{1}{4}{2}{4}{3}", p_property.quaternionValue.x, p_property.quaternionValue.y, p_property.quaternionValue.z, p_property.quaternionValue.w, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.ExposedReference:
                    return null;
                case SerializedPropertyType.FixedBufferSize:
                    return null;
                case SerializedPropertyType.Vector2Int:
                    l_result.AppendFormat("{0}{2}{1}", p_property.vector2IntValue.x, p_property.vector2IntValue.y, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.Vector3Int:
                    l_result.AppendFormat("{0}{3}{1}{3}{2}", p_property.vector3IntValue.x, p_property.vector3IntValue.y, p_property.vector3IntValue.z, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.RectInt:
                    l_result.AppendFormat("{0}{4}{1}{4}{2}{4}{3}", p_property.rectIntValue.x, p_property.rectIntValue.y, p_property.rectIntValue.width, p_property.rectIntValue.height, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                case SerializedPropertyType.BoundsInt:
                    l_result.AppendFormat("{0}{6}{1}{6}{2}{6}{3}{6}{4}{6}{5}", p_property.boundsIntValue.center.x, p_property.boundsIntValue.center.y, p_property.boundsIntValue.center.z,
                        p_property.boundsIntValue.size.x, p_property.boundsIntValue.size.y, p_property.boundsIntValue.size.z, Datalink.GetColumnDelimiter(Datalink.SendInDrive));
                    break;
                default:
                    return null;
            }

            l_data.Add(l_result.ToString());

            //Manage some types that have child properties
            if (p_property.propertyType == SerializedPropertyType.Generic)
            {
                if (p_property.isArray)
                {
                    l_data.AddRange(SplitArray(p_property));
                }
                else if (p_property.hasChildren)
                {
                    l_data.AddRange(SplitObject(p_property));
                }
            }
            return l_data.ToArray();
        }

        /// <summary>
        /// Convert all entities of a SerializedProperty array into an array of strings
        /// </summary>
        /// <param name="p_array">The SerializedProperty array to convert</param>
        /// <returns>The string representing the SerializedProperty array</returns>
        private string[] SplitArray(SerializedProperty p_array)
        {
            var l_data = new List<string>();

            for (var l_i = 0; l_i < p_array.arraySize; l_i++)
            {
                var l_sp = p_array.GetArrayElementAtIndex(l_i);

                var l_stringData = ConvertPropertyIntoString(l_sp);
                if (l_stringData != null)
                {
                    for (var l_j = 0; l_j < l_stringData.Length; l_j++)
                    {
                        l_data.Add($"{l_i} {l_stringData[l_j]}");
                    }
                }
            }
            return l_data.ToArray();
        }

        /// <summary>
        /// Convert all properties of a SerializedProperty object into an array of strings
        /// </summary>
        /// <param name="p_object">the SerializedProperty object to convert</param>
        /// <returns>The string representing the SerializedProperty object</returns>
        private string[] SplitObject(SerializedProperty p_object)
        {
            var l_data = new List<string>();
            var l_sp = p_object.Copy();
            l_sp.NextVisible(true);
            do
            {
                var l_stringData = ConvertPropertyIntoString(l_sp);
                if (l_stringData != null)
                {
                    for (var l_j = 0; l_j < l_stringData.Length; l_j++)
                    {
                        l_data.Add($"-{l_stringData[l_j]}");
                    }
                }
            } while (l_sp.NextVisible(false) && l_sp.propertyPath.Contains(p_object.propertyPath));
            return l_data.ToArray();
        }
        #endregion

        #region Load
        /// <summary>
        /// Load the data for the scriptable object from the google apps scripts
        /// </summary>
        /// <param name="p_url">the url of the google apps scripts</param>
        public void Load(string p_url)
        {
            Datalink.SetMessage(MessageType.Loading);
            if (Datalink.SendInDrive)
            {
                Bridge.GetSpreadsheetData(p_url, LoadCallback, m_object.name);

                m_waitingCallback = true;
            }
            else
            {
                string l_path = Path.Combine(p_url, $"{m_object.name}.csv");
                if (File.Exists(l_path))
                {
                    LoadCallback(File.ReadAllText(l_path));
                    Datalink.SetMessage(MessageType.Success);
                }
                else
                {
                    Datalink.SetMessage(MessageType.Fail,$"The file at path {l_path} doesn't exist");
                }
            }
        }

        /// <summary>
        /// Handle the response for a load request
        /// </summary>
        /// <param name="p_info">The response for the request</param>
        public void LoadCallback(GetDataInfo p_info)
        {
            m_waitingCallback = false;
            if (p_info.success)
            {
                var l_lines = p_info.data.Split(new[] { Datalink.GetLineDelimiter(Datalink.SendInDrive) }, StringSplitOptions.RemoveEmptyEntries);
                var l_splits = new string[l_lines.Length][];
                for (var l_i = 0; l_i < l_lines.Length; l_i++)
                {
                    l_splits[l_i] = l_lines[l_i].Split(new[] { Datalink.GetColumnDelimiter(Datalink.SendInDrive) }, StringSplitOptions.RemoveEmptyEntries);
                }
                SetProperties(l_splits);
            }
            Datalink.SetMessage(p_info.success, p_info.timedOut);
        }

        /// <summary>
        /// Handle the load from a load all request
        /// </summary>
        /// <param name="p_info">The data for this object</param>
        public void LoadCallback(string p_info)
        {
            m_waitingCallback = false;
            var l_lines = p_info.Split(new[] { Datalink.GetLineDelimiter(Datalink.SendInDrive) }, StringSplitOptions.RemoveEmptyEntries);
            var l_splits = new string[l_lines.Length][];
            for (var l_i = 0; l_i < l_lines.Length; l_i++)
            {
                l_splits[l_i] = l_lines[l_i].Split(new[] { Datalink.GetColumnDelimiter(Datalink.SendInDrive)}, StringSplitOptions.RemoveEmptyEntries);
            }
            SetProperties(l_splits);
        }

        /// <summary>
        /// Use the data of the google sheet to setup the scriptableObject
        /// </summary>
        /// <param name="p_data">The data from the google sheet split to be usable</param>
        public void SetProperties(string[][] p_data)
        {
            if (m_serializedObject == null)
                m_serializedObject = new SerializedObject(m_object);

            for (var l_i = 1; l_i < p_data.Length; l_i++)
            {
                var l_property = m_serializedObject.FindProperty(p_data[l_i][0]);
                if (l_property != null && (l_property.name.Contains("m_Script") || l_property.propertyType == SerializedPropertyType.ObjectReference))
                    l_property = null;
                if (l_property != null)
                {
                    l_i = ConvertStringToProperty(l_property, p_data, l_i);
                }
            }
            m_serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Set the property with the info of a string array
        /// </summary>
        /// <param name="p_prop">The property to set</param>
        /// <param name="p_data">Array with all info</param>
        /// <param name="p_line">The line(s) to read to set up the property</param>
        /// <returns>Return the index number of this property</returns>
        private int ConvertStringToProperty(SerializedProperty p_prop, IReadOnlyList<string[]> p_data, int p_line)
        {
            if (p_prop != null && (p_prop.propertyType == SerializedPropertyType.Generic || p_data[p_line].Length > 2))
            {
                int l_intValue;
                float l_floatValue;
                switch (p_prop.propertyType)
                {
                    case SerializedPropertyType.Generic:
                        if (p_prop.isArray)
                        {
                            //we are parsing the number of element in the array
                            if (int.TryParse(p_data[p_line][p_data[p_line].Length - 1], out l_intValue))
                            {
                                p_prop.arraySize = l_intValue;
                            }
                            var l_count = 0;
                            //The lines after are considered as lines of the array
                            //we read them to fill the array
                            p_line++;
                            while (p_line < p_data.Count && l_count < p_prop.arraySize)
                            {
                                p_line = ConvertStringToProperty(p_prop.GetArrayElementAtIndex(l_count), p_data, p_line);
                                p_line++; l_count++;
                            }
                            p_line--;
                        }
                        else if (p_prop.hasChildren)
                        {
                            //we check the position in the hierarchy of object
                            var l_mySplitLength = p_data[p_line][0].Split('-').Length;
                            p_line++;
                            //the line after are considered properties that belong to this object as long as they have the same position in the hierarchy of objects
                            while (p_line < p_data.Count && p_data[p_line][0].Split('-').Length == l_mySplitLength+1)
                            {
                                p_line = ConvertStringToProperty(p_prop.FindPropertyRelative(p_data[p_line][0].Split('-')[l_mySplitLength]), p_data, p_line);
                                p_line++;
                            }
                            p_line--;
                        }
                        break;
                    case SerializedPropertyType.Integer:
                        if (int.TryParse(p_data[p_line][2], out l_intValue))
                        {
                            p_prop.intValue = l_intValue;
                        }
                        break;
                    case SerializedPropertyType.Boolean:
                        p_prop.boolValue = (p_data[p_line][2].ToLower().Contains("true") || p_data[p_line][2] == "1");
                        break;
                    case SerializedPropertyType.Float:
                        if (float.TryParse(p_data[p_line][2].Replace('.', ','), out l_floatValue))
                        {
                            p_prop.floatValue = l_floatValue;
                        }
                        break;
                    case SerializedPropertyType.String:
                        p_prop.stringValue = p_data[p_line][2];

                        break;
                    case SerializedPropertyType.Color:
                        var l_color = new Color(0, 0, 0, 0);
                        if (float.TryParse(p_data[p_line][2], out l_floatValue))
                        {
                            l_color.r = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][3], out l_floatValue))
                        {
                            l_color.g = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][4], out l_floatValue))
                        {
                            l_color.b = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][5], out l_floatValue))
                        {
                            l_color.a = l_floatValue;
                        }
                        p_prop.colorValue = l_color;
                        break;
                    case SerializedPropertyType.LayerMask:
                        break;
                    case SerializedPropertyType.Enum:
                        if (int.TryParse(p_data[p_line][2], out l_intValue))
                        {
                            p_prop.enumValueIndex = l_intValue;
                        }
                        break;
                    case SerializedPropertyType.Vector2:
                        var l_result = new Vector2(0, 0);
                        if (float.TryParse(p_data[p_line][2].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_result.x = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][3].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_result.y = l_floatValue;
                        }
                        p_prop.vector2Value = l_result;
                        break;
                    case SerializedPropertyType.Vector3:
                        var l_v3Result = new Vector3(0, 0);
                        if (float.TryParse(p_data[p_line][2].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_v3Result.x = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][3].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_v3Result.y = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][4].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_v3Result.z = l_floatValue;
                        }
                        p_prop.vector3Value = l_v3Result;
                        break;
                    case SerializedPropertyType.Vector4:
                        var l_v4Result = new Vector4(0, 0);
                        if (float.TryParse(p_data[p_line][2].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_v4Result.x = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][3].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_v4Result.y = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][4].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_v4Result.z = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][5].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_v4Result.w = l_floatValue;
                        }
                        p_prop.vector4Value = l_v4Result;
                        break;
                    case SerializedPropertyType.Rect:
                        var l_rect = new Rect(0, 0, 0, 0);
                        if (float.TryParse(p_data[p_line][2].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_rect.x = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][3].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_rect.y = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][4].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_rect.width = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][5].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_rect.height = l_floatValue;
                        }
                        p_prop.rectValue = l_rect;
                        break;
                    case SerializedPropertyType.ArraySize:
                        break;
                    case SerializedPropertyType.Character:
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        var l_kf = new List<Keyframe>();
                        string[] l_splitKeyframe; float[] l_valueKeyframe;
                        for (var l_i = 2; l_i < p_data[p_line].Length; l_i++)
                        {
                            l_splitKeyframe = p_data[p_line][l_i].Split('_');
                            l_valueKeyframe = new float[l_splitKeyframe.Length];

                            for (var l_j = 0; l_j < l_valueKeyframe.Length; l_j++)
                            {
                                float.TryParse(l_splitKeyframe[l_j], out l_valueKeyframe[l_j]);
                            }

                            l_kf.Add(new Keyframe(l_valueKeyframe[0], l_valueKeyframe[1], l_valueKeyframe[2], l_valueKeyframe[3], l_valueKeyframe[4], l_valueKeyframe[5]));
                        }
                        p_prop.animationCurveValue = new AnimationCurve(l_kf.ToArray());
                        break;
                    case SerializedPropertyType.Bounds:
                        var l_center = new Vector3();
                        var l_size = new Vector3();
                        if (float.TryParse(p_data[p_line][2].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_center.x = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][3].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_center.y = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][4].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_center.z = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][5].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_size.x = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][6].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_size.y = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][7].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_size.z = l_floatValue;
                        }
                        p_prop.boundsValue = new Bounds(l_center, l_size);
                        break;
                    case SerializedPropertyType.Gradient:
                        break;
                    case SerializedPropertyType.Quaternion:
                        var l_quat = new Quaternion(0, 0, 0, 0);
                        if (float.TryParse(p_data[p_line][2].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_quat.x = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][3].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_quat.y = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][4].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_quat.z = l_floatValue;
                        }
                        if (float.TryParse(p_data[p_line][5].ToString(CultureInfo.InvariantCulture).Replace('.', ','), out l_floatValue))
                        {
                            l_quat.w = l_floatValue;
                        }
                        p_prop.quaternionValue = l_quat;
                        break;
                    case SerializedPropertyType.ExposedReference:
                        break;
                    case SerializedPropertyType.FixedBufferSize:
                        break;
                    case SerializedPropertyType.Vector2Int:
                        var l_intResult = new Vector2Int(0, 0);
                        if (int.TryParse(p_data[p_line][2], out l_intValue))
                        {
                            l_intResult.x = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][3], out l_intValue))
                        {
                            l_intResult.y = l_intValue;
                        }
                        p_prop.vector2IntValue = l_intResult;
                        break;
                    case SerializedPropertyType.Vector3Int:
                        var l_int3Result = new Vector3Int(0, 0, 0);
                        if (int.TryParse(p_data[p_line][2], out l_intValue))
                        {
                            l_int3Result.x = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][3], out l_intValue))
                        {
                            l_int3Result.y = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][4], out l_intValue))
                        {
                            l_int3Result.z = l_intValue;
                        }
                        p_prop.vector3IntValue = l_int3Result;
                        break;
                    case SerializedPropertyType.RectInt:
                        var l_intRect = new RectInt(0, 0, 0, 0);
                        if (int.TryParse(p_data[p_line][2], out l_intValue))
                        {
                            l_intRect.x = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][3], out l_intValue))
                        {
                            l_intRect.y = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][4], out l_intValue))
                        {
                            l_intRect.width = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][5], out l_intValue))
                        {
                            l_intRect.height = l_intValue;
                        }
                        p_prop.rectIntValue = l_intRect;
                        break;
                    case SerializedPropertyType.BoundsInt:
                        var l_centerI = new Vector3Int();
                        var l_sizeI = new Vector3Int();
                        if (int.TryParse(p_data[p_line][2], out l_intValue))
                        {
                            l_centerI.x = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][3], out l_intValue))
                        {
                            l_centerI.y = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][4], out l_intValue))
                        {
                            l_centerI.z = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][5], out l_intValue))
                        {
                            l_sizeI.x = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][6], out l_intValue))
                        {
                            l_sizeI.y = l_intValue;
                        }
                        if (int.TryParse(p_data[p_line][7], out l_intValue))
                        {
                            l_sizeI.z = l_intValue;
                        }
                        p_prop.boundsIntValue = new BoundsInt(l_centerI, l_sizeI);
                        break;
                }
            }
            return p_line;
        }
        #endregion

        /// <summary>
        /// GUIStyle used to display a button like a label
        /// </summary>
        /// <returns>The GUIStyle</returns>
        private GUIStyle GetBtnStyle()
        {
            var l_s = new GUIStyle();
            var l_b = l_s.border;
            l_b.left = 0;
            l_b.top = 0;
            l_b.right = 0;
            l_b.bottom = 0;
            l_s.alignment = TextAnchor.MiddleCenter;
            return l_s;
        }
    }

    /// <summary>
    /// Enumeration containing the possible types of messages
    /// </summary>
    public enum MessageType
    {
        None = 0,
        Success = 1,
        Timeout = 2,
        Fail = 3,
        Loading = 4,
        Empty = 5,
    }
}