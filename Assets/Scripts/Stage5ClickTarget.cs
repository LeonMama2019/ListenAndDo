using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Stage5ClickTarget : MonoBehaviour, IPointerClickHandler
{
    private Action<PointerEventData> action;
    public void Bind(Action<PointerEventData> callback) => action = callback;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            action?.Invoke(eventData);
    }
}
