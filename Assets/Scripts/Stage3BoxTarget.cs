using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Stage3BoxTarget : MonoBehaviour
{
    [SerializeField] private string boxId;
    [Tooltip("任意。指定したRectTransformを入口の判定範囲に使う。Colliderは不要。")]
    [SerializeField] private RectTransform hitArea;
    [Tooltip("Hit Area未指定時の、箱画像内の入口範囲（0〜1）。")]
    [SerializeField] private Rect opening = new Rect(0.08f, 0.25f, 0.84f, 0.65f);
    public string BoxId => boxId;
    public RectTransform Rect => (RectTransform)transform;
    public Image Image => GetComponent<Image>();

    public bool Contains(Vector2 screenPoint, Camera camera)
    {
        if (hitArea != null)
            return RectTransformUtility.RectangleContainsScreenPoint(hitArea, screenPoint, camera);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screenPoint, camera, out var local))
            return false;
        var bounds = Rect.rect;
        if (bounds.width <= 0 || bounds.height <= 0) return false;
        var normalized = new Vector2((local.x - bounds.xMin) / bounds.width,
            (local.y - bounds.yMin) / bounds.height);
        return opening.Contains(normalized);
    }
}
