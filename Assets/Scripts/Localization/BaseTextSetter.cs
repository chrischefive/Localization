using UnityEditor;
using UnityEngine;

namespace Chrische.Localization
{
    public abstract class BaseTextSetter : MonoBehaviour
    {
        protected TextDataBase _textDataBase = default;
        
        [SerializeField] 
        protected TextId _id;
        protected virtual void Awake()
        {
            _textDataBase = (TextDataBase)AssetDatabase.LoadAssetAtPath("Assets/textDataBase.asset", typeof(TextDataBase));
            if (!_textDataBase)
            {
                Debug.Log("#BaseTextSetter#: cant load textdatebase asset");
            }
        }

        public abstract void UpdateText();
    }
}

