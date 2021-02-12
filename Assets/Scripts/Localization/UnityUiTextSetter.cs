using UnityEngine;
using UnityEngine.UI;

namespace Chrische.Localization
{
    public class UnityUiTextSetter : BaseTextSetter
    {
        private Text _textField = default;

        protected override void Awake()
        {
            base.Awake();
            
            _textField = GetComponent<Text>();
            if (!_textField)
            {
                Debug.Log("#UnityUiTextSetter#: cant find textfield");
            }
        }

        public override void UpdateText()
        {
            var text = _textDataBase.GetText(_id);
            _textField.text = text;
        }

        private void OnEnable()
        {
            UpdateText();
        }
    }
}

