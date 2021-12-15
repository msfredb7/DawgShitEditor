using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngineX;

public class UPaintGUIControllableObject : MonoBehaviour, IDragHandler, IBeginDragHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private RectTransform _optionsContainer;
    [SerializeField] private Button _optionPrefab;

    public bool Selected { get; private set; }
    private RectTransform _transform;
    private Vector2 _startDragPosition;

    private void Awake()
    {
        _transform = GetComponent<RectTransform>();
        DisplayOptions(false);
    }


    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        _startDragPosition = _transform.anchoredPosition;
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        _transform.anchoredPosition = _startDragPosition + (Vector2)(eventData.pointerCurrentRaycast.worldPosition - eventData.pointerPressRaycast.worldPosition);
    }

    void ISelectHandler.OnSelect(BaseEventData eventData)
    {
        DisplayOptions(true);
        Selected = true;
    }

    void IDeselectHandler.OnDeselect(BaseEventData eventData)
    {
        Selected = false;
        DisplayOptions(false);
    }

    public void DisplayOptions(bool display)
    {
        _optionsContainer.gameObject.SetActive(display);
    }
}
