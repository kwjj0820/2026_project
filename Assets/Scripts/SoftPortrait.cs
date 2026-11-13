using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

// A UI-native edge fade, preserving the source sprite and its aspect ratio.
[RequireComponent(typeof(CanvasRenderer))]
public class SoftPortrait : MaskableGraphic
{
    private Sprite sprite;
    public override Texture mainTexture => sprite != null ? sprite.texture : Texture2D.whiteTexture;

    public void SetPortrait(Sprite value)
    {
        sprite = value;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (sprite == null) return;
        Rect bounds = GetPixelAdjustedRect();
        float scale = Mathf.Min(bounds.width / sprite.rect.width, bounds.height / sprite.rect.height);
        Vector2 size = sprite.rect.size * scale;
        Vector2 origin = bounds.center - size * 0.5f;
        Vector4 uv = DataUtility.GetOuterUV(sprite);
        const int steps = 24;
        for (int y = 0; y <= steps; y++)
        for (int x = 0; x <= steps; x++)
        {
            float u = (float)x / steps, v = (float)y / steps;
            float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
            Color tint = color;
            tint.a *= Mathf.SmoothStep(0, 1, edge / 0.14f);
            mesh.AddVert(origin + new Vector2(u * size.x, v * size.y), tint,
                new Vector2(Mathf.Lerp(uv.x, uv.z, u), Mathf.Lerp(uv.y, uv.w, v)));
        }
        for (int y = 0; y < steps; y++)
        for (int x = 0; x < steps; x++)
        {
            int a = y * (steps + 1) + x;
            mesh.AddTriangle(a, a + steps + 1, a + 1);
            mesh.AddTriangle(a + 1, a + steps + 1, a + steps + 2);
        }
    }
}
