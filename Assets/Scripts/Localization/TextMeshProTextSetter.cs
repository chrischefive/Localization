using Chrische.Localization;
using TMPro;
using UnityEngine;

public class TextMeshProTextSetter : BaseTextSetter
{
    private TextMeshProUGUI _textField = default;

    protected override void Awake()
    {
        base.Awake();
            
        _textField = GetComponent<TextMeshProUGUI>();
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
