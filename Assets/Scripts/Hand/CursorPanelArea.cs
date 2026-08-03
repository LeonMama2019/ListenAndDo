using UnityEngine;
using UnityEngine.EventSystems;

public class CursorPanelArea : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private HandListSelector hand;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hand.SetCursorEnabled(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hand.SetCursorEnabled(false);
    }
}