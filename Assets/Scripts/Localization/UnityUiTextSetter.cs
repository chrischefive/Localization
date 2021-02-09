using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Chrische.Localization
{
    public class UnityUiTextSetter : MonoBehaviour
    {
        [ShowInInspector] 
        [SerializeField] 
        private TextDataBase _textDataBase = default;

        [ShowInInspector] 
        [SerializeField] 
        private TextId _id;

        private Text _textField = default;

        private void Awake()
        {
            _textField = GetComponent<Text>();
            if (!_textField)
            {
                Debug.Log("#UnityUiTextSetter#: cant find textfield");
            }

            if (!_textDataBase)
            {
                Debug.Log("#UnityUiTextSetter#: no textDatabase attached");
            }
        }

        private void OnEnable()
        {
            _textField.text = _textDataBase.GetText(_id);
        }
    }
}

