using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextSlider : MonoBehaviour, IEventSystemHandler
{
    [SerializeField]
    private float _value;
    [SerializeField]
    private int _decimalPlaces;

    public float value 
        { 
        get 
        {
            return _value;
        } 
        set 
        {
            _value = value;
            _textInput.SetTextWithoutNotify(_value.ToString("F" + _decimalPlaces));
            _slider.SetValueWithoutNotify(_value);
        }
    }

    [SerializeField]
    private TextSliderEvent _onValueChanged;
    private InputField _textInput;
    private Slider _slider;

    [Serializable]
    public class TextSliderEvent : UnityEvent<float>
    {
        public TextSliderEvent() { }
    }


    // Start is called before the first frame update
    void Awake()
    {
        _textInput = GetComponentInChildren<InputField>();
        _slider = GetComponentInChildren<Slider>();
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
        _textInput.onValueChanged.AddListener(OnTextValueChanged);
    }

    public void OnSliderValueChanged(float v)
	{
        _value = v;
        _textInput.SetTextWithoutNotify(value.ToString("F" + _decimalPlaces));
        _onValueChanged.Invoke(value);
	}

    public void OnTextValueChanged(string text)
	{
        float value;
        if (float.TryParse(text, out value))
		{
            _value = value;
            _slider.SetValueWithoutNotify(value);
            _onValueChanged.Invoke(value);
        }
    }

}
