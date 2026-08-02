using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small dependency-free material and sprite factory used by the Nightfall redesign.
/// Everything is generated at runtime, remains crisp at any resolution, and can be
/// reused by future characters, enemies, pickups and UI screens.
/// </summary>
public static class CutePixelKit
{
    public static readonly Color Night = Hex("17152B");
    public static readonly Color NightSoft = Hex("24203D");
    public static readonly Color Plum = Hex("4A315F");
    public static readonly Color Ink = Hex("21192B");
    public static readonly Color Cream = Hex("FFF2C9");
    public static readonly Color Paper = Hex("F7E8C5");
    public static readonly Color Mint = Hex("8EE3C2");
    public static readonly Color MintDark = Hex("3B9E86");
    public static readonly Color Sky = Hex("8EC5FF");
    public static readonly Color Lavender = Hex("BFA7FF");
    public static readonly Color Peach = Hex("FFB98B");
    public static readonly Color Coral = Hex("FF758F");
    public static readonly Color Berry = Hex("A4496B");
    public static readonly Color Gold = Hex("FFD56A");
    public static readonly Color Leaf = Hex("63B36F");
    public static readonly Color LeafDark = Hex("2E6F55");
    public static readonly Color White = Hex("FFFDF6");
    public static readonly Color Shadow = new Color(0.05f, 0.035f, 0.09f, 0.52f);

    // Original tiny-mascot palette: milk, berry, blush and mint accents keep
    // characters gentle while the dark plum outline preserves gameplay clarity.
    public static readonly Color MascotOutline = Hex("4B3042");
    public static readonly Color MascotCream = Hex("FFF1D4");
    public static readonly Color MascotPink = Hex("F4A7B9");
    public static readonly Color MascotBlush = Hex("F29A9A");
    public static readonly Color MascotMint = Hex("9ADCC6");
    public static readonly Color MascotLilac = Hex("C8B5EF");
    public static readonly Color MascotBrown = Hex("A86D62");
    public static readonly Color MascotGold = Hex("FFD576");

    private static Sprite whiteSprite;
    private static Font friendlyFont;

    public static Color Hex(string hex)
    {
        Color value;
        return ColorUtility.TryParseHtmlString("#" + hex, out value) ? value : Color.magenta;
    }

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                whiteSprite = CreateSprite(
                    "Cute White Pixel",
                    new[] { "W" },
                    new Dictionary<char, Color> { { 'W', Color.white } },
                    1f);
            }

            return whiteSprite;
        }
    }

    public static Font FriendlyFont
    {
        get
        {
            if (friendlyFont != null) return friendlyFont;

            friendlyFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Chalkboard SE", "Arial Rounded MT Bold", "Trebuchet MS", "Arial" },
                18);
            return friendlyFont;
        }
    }

    public static Sprite CreateSprite(
        string name,
        string[] rows,
        Dictionary<char, Color> palette,
        float pixelsPerUnit = 16f,
        Vector2? pivot = null)
    {
        int height = rows.Length;
        int width = 1;
        for (int i = 0; i < rows.Length; i++) width = Mathf.Max(width, rows[i].Length);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = name + " Texture";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        for (int row = 0; row < height; row++)
        {
            string line = rows[row];
            int y = height - 1 - row;
            for (int x = 0; x < line.Length; x++)
            {
                Color color;
                if (palette.TryGetValue(line[x], out color)) pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            pivot ?? new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        sprite.name = name;
        return sprite;
    }

    /// <summary>
    /// Creates a crisp sprite from the authored 32px logical-cell sheet.
    /// The one-pixel inset keeps reference-cell guide lines out of runtime sprites.
    /// </summary>
    public static Sprite CreateAtlasSprite(
        Texture2D atlas,
        string name,
        int column,
        int rowFromTop,
        int cellSize = 32,
        float pixelsPerUnit = 32f)
    {
        if (atlas == null) return null;

        atlas.filterMode = FilterMode.Point;
        atlas.wrapMode = TextureWrapMode.Clamp;

        int inset = Mathf.Min(1, Mathf.Max(0, cellSize / 8));
        int x = column * cellSize + inset;
        int y = atlas.height - ((rowFromTop + 1) * cellSize) + inset;
        int size = cellSize - inset * 2;
        if (x < 0 || y < 0 || x + size > atlas.width || y + size > atlas.height) return null;

        Sprite sprite = Sprite.Create(
            atlas,
            new Rect(x, y, size, size),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        sprite.name = name;
        return sprite;
    }

    /// <summary>
    /// Copies one top-left-origin region from an authored pixel atlas into a
    /// standalone point-filtered texture. Runtime GUI styles can then use the
    /// result as a proper sliced panel without stretching the atlas itself.
    /// </summary>
    public static Texture2D CropAtlasTexture(
        Texture2D atlas,
        string name,
        int x,
        int yFromTop,
        int width,
        int height)
    {
        if (atlas == null || width <= 0 || height <= 0) return null;

        int y = atlas.height - yFromTop - height;
        if (x < 0 || y < 0 || x + width > atlas.width || y + height > atlas.height) return null;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels(atlas.GetPixels(x, y, width, height));
        texture.Apply(false, false);
        return texture;
    }

    /// <summary>
    /// Removes baked reference copy from the centre of an authored panel while
    /// preserving its pixel border, shadow and clipped corners. This keeps the
    /// production frame language but leaves the content area data-driven.
    /// </summary>
    public static Texture2D FlattenPanelInterior(
        Texture2D panel,
        string name,
        int border,
        Color fill)
    {
        if (panel == null) return null;

        Color[] pixels = panel.GetPixels();
        int left = Mathf.Clamp(border, 0, panel.width / 2);
        int right = Mathf.Clamp(border, 0, panel.width / 2);
        int bottom = Mathf.Clamp(border, 0, panel.height / 2);
        int top = Mathf.Clamp(border, 0, panel.height / 2);
        for (int y = bottom; y < panel.height - top; y++)
        {
            for (int x = left; x < panel.width - right; x++)
            {
                int index = y * panel.width + x;
                if (pixels[index].a > 0f) pixels[index] = fill;
            }
        }

        Texture2D texture = new Texture2D(panel.width, panel.height, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    public static Texture2D SolidTexture(Color color, string name = "Cute Solid")
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels(new[] { color, color, color, color });
        texture.Apply(false, true);
        return texture;
    }

    public static Texture2D PanelTexture(
        Color fill,
        Color edge,
        Color highlight,
        int size = 16,
        int corner = 3)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Cute Pixel Panel";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool clipped =
                    (x < corner && y < corner && x + y < corner - 1) ||
                    (x >= size - corner && y < corner && (size - 1 - x) + y < corner - 1) ||
                    (x < corner && y >= size - corner && x + (size - 1 - y) < corner - 1) ||
                    (x >= size - corner && y >= size - corner &&
                     (size - 1 - x) + (size - 1 - y) < corner - 1);

                if (clipped)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool border = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                bool topHighlight = y == size - 2 && x > 2 && x < size - 3;
                texture.SetPixel(x, y, border ? edge : (topHighlight ? highlight : fill));
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    public static GameObject SpriteObject(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        float scale,
        int order,
        Color? tint = null)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = new Vector3(position.x, position.y, 0f);
        gameObject.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.color = tint ?? Color.white;
        return gameObject;
    }

    public static GameObject RectObject(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        int order)
    {
        GameObject gameObject = SpriteObject(parent, name, WhiteSprite, position, 1f, order, color);
        gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
        return gameObject;
    }

    public static GameObject ShadowObject(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        int order)
    {
        Sprite shadowSprite = CreateSprite(
            name + " Sprite",
            new[]
            {
                ".............",
                "...SSSSSSS...",
                ".SSSSSSSSSSS.",
                "SSSSSSSSSSSSS",
                ".SSSSSSSSSSS.",
                "...SSSSSSS...",
                "............."
            },
            new Dictionary<char, Color> { { 'S', Shadow } },
            16f);
        GameObject shadow = SpriteObject(parent, name, shadowSprite, position, 1f, order);
        shadow.transform.localScale = new Vector3(size.x, size.y, 1f);
        return shadow;
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
