using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HellionCat.DataLink
{
    [CustomEditor(typeof(ScriptableObject), true)]
    public class SO_EditorUpdate : Editor
    {
        private ObjectData m_this;
        /// <summary>
        /// the url of the googlesheet
        /// </summary>
        private string m_url;

        private void OnEnable()
        {
            ScriptableObject obj = (ScriptableObject) target;
            m_this = new ObjectData(obj);

            //try to find the config file (if it exist) to get the url of the sheet
            TextAsset m_config = Resources.Load<TextAsset>("HC_dlink_config");
            if (m_config)
                m_url = m_config.text.Split('\n')[0];
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(m_this == null)
            {
                ScriptableObject obj = (ScriptableObject)target;
                m_this = new ObjectData(obj);
            }
            if (!string.IsNullOrEmpty(m_url.Trim()) && m_url.Length > 10)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical(GUI.skin.box);
                m_this.DisplayInObject(m_url.Trim());
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}