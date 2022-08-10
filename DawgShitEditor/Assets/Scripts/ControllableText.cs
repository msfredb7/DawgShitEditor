using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngineX;

[RequireComponent(typeof(UPaintGUIControllableObject))]
public class ControllableText : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private float _fontSizeChangeSpeed = 20;
    [SerializeField] private float _sizeChangeSpeed;
    [SerializeField] private TextMeshProUGUI _textField;
    [SerializeField] private TMP_FontAsset[] _fontCarousel;
    [SerializeField] private TMP_InputField _inputField;

    private UPaintGUIControllableObject _controllable;
    private int _currentFontCarouselIndex = 0;
    private bool _ignoreNextEndEdit;
    private bool _ignoreSubmitButtonOnNextUpdate;

    public float FontSize
    {
        get => _textField.fontSize;
        set => _textField.fontSize = value;
    }
    public float SizeDeltaX
    {
        get => GetComponent<RectTransform>().sizeDelta.x;
        set => GetComponent<RectTransform>().sizeDelta = new Vector2(value, GetComponent<RectTransform>().sizeDelta.y);
    }
    public int FontIndex
    {
        get => _currentFontCarouselIndex;
        set {
            _currentFontCarouselIndex = value;
            _textField.font = _fontCarousel[_currentFontCarouselIndex];
        }
    }
    public string Text
    {
        get => _textField.text;
        set => _textField.text = value;
    }

    private void Awake()
    {
        _controllable = GetComponent<UPaintGUIControllableObject>();
        _inputField.onEndEdit.AddListener(OnEndEditInputField);
        _inputField.onSubmit.AddListener(OnSubmitInputField);
    }

    private void Update()
    {
        if (_controllable.Selected)
        {
            HandleInputs();
        }
        _ignoreNextEndEdit = false;
        _ignoreSubmitButtonOnNextUpdate = false;
    }

    private void HandleInputs()
    {
        if (!_ignoreSubmitButtonOnNextUpdate &&
            !_inputField.gameObject.activeSelf &&
            (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            // change text
            _inputField.gameObject.SetActive(true);
            _inputField.text = _textField.text;
            _inputField.fontAsset = _textField.font;
            _inputField.pointSize = _textField.fontSize;
            _inputField.Select();
            var caret = _inputField.transform.Find("Caret") as RectTransform;
            caret.anchorMin = Vector2.zero;
            caret.anchorMax = Vector2.one;
            caret.sizeDelta = Vector2.zero;
            _textField.gameObject.SetActive(false);
        }

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Keypad8))
        {
            // increase font size
            FontSize += _fontSizeChangeSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.Keypad2))
        {
            // decrease font size
            FontSize -= _fontSizeChangeSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.Keypad6))
        {
            // widden
            SizeDeltaX += _sizeChangeSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Keypad4))
        {
            // thighten
            SizeDeltaX -= _sizeChangeSpeed * Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            // delete
            Destroy(gameObject);
        }

        if (Input.GetKeyDown(KeyCode.F) || Input.GetKey(KeyCode.Keypad5))
        {
            // font swap
            FontIndex = (FontIndex + 1) % _fontCarousel.Length;
        }
    }

    private void OnEndEditInputField(string newText)
    {
        if (_ignoreNextEndEdit)
            return;

        _textField.gameObject.SetActive(true);
        _textField.text = newText;
        _inputField.gameObject.SetActive(false);
        _button.Select();
        _ignoreSubmitButtonOnNextUpdate = true;
    }

    private void OnSubmitInputField(string value)
    {
        if (_inputField.isFocused && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            int deleteStart = Mathf.Min(_inputField.selectionFocusPosition, _inputField.selectionAnchorPosition);
            int deleteEnd = Mathf.Max(_inputField.selectionFocusPosition, _inputField.selectionAnchorPosition);
            int deleteCount = deleteEnd - deleteStart;
            int caret = deleteStart;

            if (deleteCount > 0)
            {
                string newText = _inputField.text.Remove(deleteStart, deleteCount);
                _inputField.SetTextWithoutNotify(newText);
            }

            _inputField.ActivateInputField();
            _inputField.selectionFocusPosition = caret;
            _inputField.selectionAnchorPosition = caret;
            _ignoreNextEndEdit = true;
        }
    }
}
