using UnityEngine;
using UnityEngine.EventSystems;

public class CursorPanelArea : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private HandListSelector handListSelector;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (handListSelector.IsHandSelected())
        {
            handListSelector.SetCursorEnabled(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        handListSelector.SetCursorEnabled(false);
    }
}