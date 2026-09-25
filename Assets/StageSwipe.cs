using UnityEngine;
using UnityEngine.EventSystems;

public class StageSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform content;
    [SerializeField] private float minX = -1032f;
    [SerializeField] private float maxX = 0f;

    private Vector2 dragStartPointer;
    private Vector2 dragStartContent;

    private void Awake()
    {
        if (content == null) content = transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPointer = eventData.position;
        dragStartContent = content.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaX = eventData.position.x - dragStartPointer.x;
        Vector2 p = dragStartContent + new Vector2(deltaX, 0f);
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = dragStartContent.y;
        content.anchoredPosition = p;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 p = content.anchoredPosition;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        content.anchoredPosition = p;
    }
}
