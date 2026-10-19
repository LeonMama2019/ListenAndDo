using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Stage5 placement geometry, including transparent scissors masks.
public static class Stage6PlacementGeometry
{
    private static Vector3[] VisibleVertices(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        var sprite = image.sprite;
        var vertices = sprite.vertices;
        var size = rect.rect.size;
        if (image.preserveAspect)
        {
            float ratio = sprite.rect.width / sprite.rect.height;
            if (size.x / size.y > ratio) size.x = size.y * ratio;
            else size.y = size.x / ratio;
        }
        var result = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 normalized = (vertices[i] * sprite.pixelsPerUnit + sprite.pivot) / sprite.rect.size;
            Vector2 local = rect.rect.center + (normalized - Vector2.one * 0.5f) * size;
            result[i] = rect.TransformPoint(local);
        }
        return result;
    }
    public static bool FullyInside(RectTransform item, RectTransform area)
    {
        foreach (var vertex in VisibleVertices(item))
        {
            Vector2 p = area.InverseTransformPoint(vertex);
            if (p.x < area.rect.xMin - 0.01f || p.x > area.rect.xMax + 0.01f
                || p.y < area.rect.yMin - 0.01f || p.y > area.rect.yMax + 0.01f) return false;
        }
        return true;
    }
    private static readonly Dictionary<Sprite, List<Rect>> scissorsMasks = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScissorsMasks() => scissorsMasks.Clear();

    private static bool IsScissors(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        return image != null && image.sprite != null
            && image.sprite.name.StartsWith("scissors", StringComparison.OrdinalIgnoreCase);
    }

    private static Rect DrawRect(RectTransform transform)
    {
        var image = transform.GetComponent<Image>();
        var size = transform.rect.size;
        if (image.preserveAspect)
        {
            float ratio = image.sprite.rect.width / image.sprite.rect.height;
            if (size.x / size.y > ratio) size.x = size.y * ratio;
            else size.y = size.x / ratio;
        }
        return new Rect(transform.rect.center - size * 0.5f, size);
    }

    // Exact horizontal runs of non-transparent source pixels. Equal runs on adjacent rows
    // are merged, retaining handle holes and the empty space next to the blade.
    private static List<Rect> ScissorsMask(Sprite sprite)
    {
        if (scissorsMasks.TryGetValue(sprite, out var cached)) return cached;
        if (!sprite.texture.isReadable || sprite.packed)
        {
            Debug.LogError("Stage5: scissors画像はRead/Writeを有効にし、Sprite Atlasに含めず使用してください。");
            scissorsMasks[sprite] = null;
            return null;
        }
        var pixels = sprite.texture.GetPixels32();
        int textureWidth = sprite.texture.width;
        int xOffset = Mathf.RoundToInt(sprite.rect.x), yOffset = Mathf.RoundToInt(sprite.rect.y);
        int width = Mathf.RoundToInt(sprite.rect.width), height = Mathf.RoundToInt(sprite.rect.height);
        const byte minimumAlpha = 26; // Ignore almost invisible antialiased edges (10%).
        var runs = new List<Rect>();
        var previous = new Dictionary<long, int>();
        for (int y = 0; y < height; y++)
        {
            var current = new Dictionary<long, int>();
            int x = 0;
            while (x < width)
            {
                while (x < width && pixels[(yOffset + y) * textureWidth + xOffset + x].a < minimumAlpha) x++;
                int start = x;
                while (x < width && pixels[(yOffset + y) * textureWidth + xOffset + x].a >= minimumAlpha) x++;
                if (start == x) continue;
                long key = ((long)start << 32) | (uint)x;
                if (previous.TryGetValue(key, out int index))
                {
                    var run = runs[index]; run.yMax = (y + 1f) / height; runs[index] = run;
                    current[key] = index;
                }
                else
                {
                    current[key] = runs.Count;
                    runs.Add(Rect.MinMaxRect((float)start / width, (float)y / height, (float)x / width, (y + 1f) / height));
                }
            }
            previous = current;
        }
        scissorsMasks[sprite] = runs;
        return runs;
    }

    private static Vector2[] RunCorners(RectTransform item, Rect normalized, RectTransform space)
    {
        var draw = DrawRect(item);
        float x0 = draw.xMin + normalized.xMin * draw.width, x1 = draw.xMin + normalized.xMax * draw.width;
        float y0 = draw.yMin + normalized.yMin * draw.height, y1 = draw.yMin + normalized.yMax * draw.height;
        return new[] {
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x0, y0))),
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x0, y1))),
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x1, y1))),
            (Vector2)space.InverseTransformPoint(item.TransformPoint(new Vector3(x1, y0))) };
    }
    private static bool QuadsOverlap(Vector2[] a, Vector2[] b)
    {
        foreach (var polygon in new[] { a, b })
            for (int i = 0; i < 2; i++)
            {
                var edge = polygon[i + 1] - polygon[i];
                var axis = new Vector2(-edge.y, edge.x).normalized;
                float aMin = float.PositiveInfinity, aMax = float.NegativeInfinity;
                float bMin = float.PositiveInfinity, bMax = float.NegativeInfinity;
                for (int j = 0; j < 4; j++)
                {
                    float pa = Vector2.Dot(a[j], axis), pb = Vector2.Dot(b[j], axis);
                    aMin = Mathf.Min(aMin, pa); aMax = Mathf.Max(aMax, pa);
                    bMin = Mathf.Min(bMin, pb); bMax = Mathf.Max(bMax, pb);
                }
                if (aMax <= bMin + 0.00001f || bMax <= aMin + 0.00001f) return false;
            }
        return true;
    }

    public static bool Overlaps(RectTransform first, RectTransform second)
    {
        if (!IsScissors(first) && !IsScissors(second)) return MeshOverlaps(first, second);
        if (!IsScissors(first)) { var swap = first; first = second; second = swap; }
        var mask = ScissorsMask(first.GetComponent<Image>().sprite);
        if (mask == null) return MeshOverlaps(first, second);
        var full = new Rect(0, 0, 1, 1);
        var otherRect = RunCorners(second, full, second);
        if (!QuadsOverlap(RunCorners(first, full, second), otherRect)) return false;
        List<Rect> otherMask = null;
        if (IsScissors(second))
        {
            otherMask = ScissorsMask(second.GetComponent<Image>().sprite);
            if (otherMask == null) return MeshOverlaps(first, second);
        }
        foreach (var run in mask)
        {
            var quad = RunCorners(first, run, second);
            if (!QuadsOverlap(quad, otherRect)) continue;
            if (otherMask == null) return true;
            foreach (var otherRun in otherMask)
                if (QuadsOverlap(quad, RunCorners(second, otherRun, second))) return true;
        }
        return false;
    }

    // Existing mesh rule is kept unchanged for pairs without scissors.
    private static bool MeshOverlaps(RectTransform first, RectTransform second)
    {
        var a = VisibleVertices(first); var b = VisibleVertices(second);
        var space = first.GetComponentInParent<Canvas>().rootCanvas.transform;
        for (int i = 0; i < a.Length; i++) a[i] = space.InverseTransformPoint(a[i]);
        for (int i = 0; i < b.Length; i++) b[i] = space.InverseTransformPoint(b[i]);
        var at = first.GetComponent<Image>().sprite.triangles;
        var bt = second.GetComponent<Image>().sprite.triangles;
        for (int i = 0; i < at.Length; i += 3)
            for (int j = 0; j < bt.Length; j += 3)
                if (TrianglesOverlap(new[] { a[at[i]], a[at[i + 1]], a[at[i + 2]] },
                    new[] { b[bt[j]], b[bt[j + 1]], b[bt[j + 2]] })) return true;
        return false;
    }
    private static bool TrianglesOverlap(Vector3[] a, Vector3[] b)
    {
        foreach (var polygon in new[] { a, b })
            for (int i = 0; i < 3; i++)
            {
                Vector2 edge = polygon[(i + 1) % 3] - polygon[i];
                Vector2 axis = new Vector2(-edge.y, edge.x).normalized;
                float aMin = float.PositiveInfinity, aMax = float.NegativeInfinity;
                float bMin = float.PositiveInfinity, bMax = float.NegativeInfinity;
                for (int j = 0; j < 3; j++)
                {
                    float pa = Vector2.Dot(a[j], axis), pb = Vector2.Dot(b[j], axis);
                    aMin = Mathf.Min(aMin, pa); aMax = Mathf.Max(aMax, pa);
                    bMin = Mathf.Min(bMin, pb); bMax = Mathf.Max(bMax, pb);
                }
                // Treat subpixel contact at the authored edges as touching.
                if (aMax <= bMin + 0.5f || bMax <= aMin + 0.5f) return false;
            }
        return true;
    }
}
