using UnityEngine;
using UnityEngine.EventSystems;

public class StageSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform content;
    [SerializeField] private float minX = -1032f;
    [SerializeField] private float maxX = 0f;
    private RectTransform viewport;
    private Vector2 dragStartPointer;
    private Vector2 dragStartContent;
    private int? dragPointer;

    private void Awake()
    {
        if (content == null) content = transform as RectTransform;
        viewport = content.parent as RectTransform;
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        UpdateBounds();
        RectTransform first = content.Find("Stage1") as RectTransform;
        if (first != null)
        {
            Vector2 position = content.anchoredPosition;
            position.x = AlignedPosition(first, false);
            content.anchoredPosition = position;
        }
    }

    private float AlignedPosition(RectTransform stage, bool rightEdge)
    {
        float viewportEdge = rightEdge ? viewport.rect.xMax : viewport.rect.xMin;
        float stageEdge = rightEdge ? stage.rect.xMax : stage.rect.xMin;
        float edgeInViewport = viewport.InverseTransformPoint(
            stage.TransformPoint(new Vector3(stageEdge, stage.rect.center.y, 0))).x;
        return content.anchoredPosition.x + viewportEdge - edgeInViewport;
    }

    private void UpdateBounds()
    {
        if (viewport == null) return;
        RectTransform first = content.Find("Stage1") as RectTransform;
        RectTransform last = content.Find("Stage6") as RectTransform;
        if (first == null || last == null) return;
        // Start with Stage1 flush left; stop with Stage6 flush right.
        maxX = AlignedPosition(first, false);
        // If all cards fit, retain the left alignment without scrolling.
        minX = Mathf.Min(maxX, AlignedPosition(last, true));
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (dragPointer.HasValue || viewport == null ||
            e.button != PointerEventData.InputButton.Left) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, e.position, e.pressEventCamera, out dragStartPointer)) return;
        UpdateBounds();
        dragStartContent = content.anchoredPosition;
        dragPointer = e.pointerId;
        // A swipe over a stage button must not select it on release.
        e.eligibleForClick = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragPointer != e.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, e.position, e.pressEventCamera, out Vector2 point)) return;
        Vector2 p = dragStartContent;
        p.x = Mathf.Clamp(p.x + point.x - dragStartPointer.x, minX, maxX);
        content.anchoredPosition = p;
        e.eligibleForClick = false;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (dragPointer != e.pointerId) return;
        OnDrag(e);
        dragPointer = null;
    }

    private void OnDisable() => dragPointer = null;
}
