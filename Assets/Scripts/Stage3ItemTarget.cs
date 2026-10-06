using UnityEngine;
using UnityEngine.EventSystems;

public class Stage3ItemTarget : MonoBehaviour, IPointerDownHandler
{
    private Stage3Quiz quiz;
    private int slot;
    public void Bind(Stage3Quiz owner, int itemSlot) { quiz = owner; slot = itemSlot; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            quiz?.SelectItem(slot, eventData.position);
    }
}
