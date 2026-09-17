using UnityEngine;

/// <summary>
/// Spriteの縦横比を保ったまま、最大辺を共通サイズへ揃える。
/// 同じGameObjectのBoxCollider2DもSpriteの実寸へ合わせる。
/// </summary>
public static class SpriteDisplayNormalizer
{
    public static void Normalize(SpriteRenderer renderer, float maxDisplaySize = 2.5f)
    {
        if (renderer == null || renderer.sprite == null || maxDisplaySize <= 0f)
            return;

        Vector2 spriteSize = renderer.sprite.bounds.size;
        float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        if (largestSide <= 0f)
            return;

        float uniformScale = maxDisplaySize / largestSide;
        Vector3 currentScale = renderer.transform.localScale;
        renderer.transform.localScale =
            new Vector3(uniformScale, uniformScale, currentScale.z);

        BoxCollider2D boxCollider = renderer.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.offset = renderer.sprite.bounds.center;
            boxCollider.size = spriteSize;
        }
    }
}
