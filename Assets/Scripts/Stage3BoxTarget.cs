using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Stage3BoxTarget : MonoBehaviour
{
    [SerializeField] private string boxId;
    [Tooltip("任意。指定したRectTransformを入口の判定範囲に使う。Colliderは不要。")]
    [SerializeField] private RectTransform hitArea;
    [Tooltip("入口の範囲。箱画像に対する左・下・右・上の比率（0〜1）。")]
    [SerializeField] private Vector4 openingBounds = new Vector4(0.08f, 0.25f, 0.92f, 0.9f);
    [Tooltip("箱の入口に近づいた時に入るよう、判定を広げる距離。")]
    [SerializeField, Min(0)] private float dropPadding = 12f;
    private readonly Vector3[] copyCorners = new Vector3[4];
    public string BoxId => boxId;
    public RectTransform Rect => (RectTransform)transform;
    public Image Image => GetComponent<Image>();

    public bool Contains(Vector2 screenPoint, Camera camera, RectTransform carriedItem = null)
    {
        var area = hitArea != null ? hitArea : Rect;
        var bounds = area.rect;
        if (bounds.width <= 0 || bounds.height <= 0) return false;
        if (hitArea == null)
        {
            bounds = UnityEngine.Rect.MinMaxRect(
                bounds.xMin + bounds.width * openingBounds.x,
                bounds.yMin + bounds.height * openingBounds.y,
                bounds.xMin + bounds.width * openingBounds.z,
                bounds.yMin + bounds.height * openingBounds.w);
        }
        float padding = Mathf.Max(0, dropPadding);
        bounds = UnityEngine.Rect.MinMaxRect(bounds.xMin - padding, bounds.yMin - padding,
            bounds.xMax + padding, bounds.yMax + padding);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, camera, out var local)
            && bounds.Contains(local)) return true;
        if (carriedItem == null) return false;
        // Detect the visible copy touching the entrance, even before its center reaches the box.
        carriedItem.GetWorldCorners(copyCorners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var corner in copyCorners)
        {
            Vector2 point = area.InverseTransformPoint(corner);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return bounds.Overlaps(UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y));
    }
}
