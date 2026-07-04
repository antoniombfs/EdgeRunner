#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates the small, original PNG art kit consumed by FinalDemoVisualPolish.
/// The generator is deterministic and only writes visual assets under Resources.
/// </summary>
public static class FinalDemoArtKitGenerator
{
    private const string OutputFolder = "Assets/Resources/DemoFinal/Art";

    [InitializeOnLoadMethod]
    private static void GenerateMissingAssetsOnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            string playerPath = Path.Combine(OutputFolder, "player_runner.png");
            string generatorPath = "Assets/EdgeRunner/Editor/Demo/FinalDemoArtKitGenerator.cs";
            if (!File.Exists(Path.Combine(OutputFolder, "platform_slab.png")) ||
                !File.Exists(Path.Combine(OutputFolder, "goal_portal_unlocked.png")) ||
                !File.Exists(playerPath) ||
                File.GetLastWriteTimeUtc(generatorPath) > File.GetLastWriteTimeUtc(playerPath))
            {
                Generate();
            }
        };
    }

    [MenuItem("EdgeRunner/Demo Final/Regenerate Presentation Art Kit")]
    public static void Generate()
    {
        Directory.CreateDirectory(OutputFolder);
        WritePlatform();
        WritePlayer();
        WriteAndroid();
        WritePowerCell("pickup_powercell_cyan.png", new Color32(46, 230, 255, 255), new Color32(191, 252, 255, 255));
        WritePowerCell("pickup_powercell_gold.png", new Color32(255, 184, 42, 255), new Color32(255, 244, 169, 255));
        WritePortal("goal_portal_unlocked.png", new Color32(33, 245, 216, 255), new Color32(103, 255, 235, 92));
        WritePortal("goal_portal_locked.png", new Color32(255, 42, 118, 255), new Color32(255, 38, 89, 82));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[FinalDemoArtPass] assets generated/loaded path=" + OutputFolder);
    }

    private static Texture2D NewCanvas(int width, int height, string name)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(new Color32[width * height]);
        return texture;
    }

    private static void Save(Texture2D texture, string filename)
    {
        texture.Apply(false, false);
        File.WriteAllBytes(Path.Combine(OutputFolder, filename), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void Rect(Texture2D texture, int x, int y, int width, int height, Color32 color)
    {
        int maxX = Mathf.Min(texture.width, x + width);
        int maxY = Mathf.Min(texture.height, y + height);
        for (int py = Mathf.Max(0, y); py < maxY; py++)
        for (int px = Mathf.Max(0, x); px < maxX; px++)
            texture.SetPixel(px, py, color);
    }

    private static void Pixel(Texture2D texture, int x, int y, Color32 color)
    {
        if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            texture.SetPixel(x, y, color);
    }

    private static void WritePlatform()
    {
        Texture2D t = NewCanvas(64, 24, "DemoFinal_PlatformSlab");
        Color32 body = new Color32(15, 46, 58, 255);
        Color32 bodyTop = new Color32(22, 66, 78, 255);
        Color32 bevel = new Color32(9, 29, 38, 255);
        Color32 cyan = new Color32(34, 235, 255, 255);
        Color32 cyanSoft = new Color32(26, 126, 147, 255);
        Rect(t, 0, 0, 64, 24, body);
        Rect(t, 0, 19, 64, 3, bodyTop);
        Rect(t, 0, 22, 64, 2, cyan);
        Rect(t, 0, 21, 64, 1, cyanSoft);
        Rect(t, 0, 0, 64, 2, bevel);
        Rect(t, 0, 2, 64, 1, new Color32(18, 69, 77, 255));
        Rect(t, 0, 0, 3, 24, bevel);
        Rect(t, 61, 0, 3, 24, bevel);
        Save(t, "platform_slab.png");
    }

    private static void WritePlayer()
    {
        Texture2D t = NewCanvas(48, 64, "DemoFinal_PlayerRunner");
        Color32 outline = new Color32(4, 13, 20, 255);
        Color32 armour = new Color32(20, 42, 54, 255);
        Color32 armourLight = new Color32(45, 76, 88, 255);
        Color32 joint = new Color32(10, 27, 37, 255);
        Color32 cyan = new Color32(37, 231, 255, 255);
        Color32 cyanHot = new Color32(184, 251, 255, 255);
        // Compact side-profile sprint pose: forward helmet, angled torso and separated stride.
        Rect(t, 14, 46, 20, 13, outline); Rect(t, 16, 48, 17, 10, armour);
        Rect(t, 23, 51, 14, 5, cyan); Rect(t, 25, 53, 11, 2, cyanHot);
        Rect(t, 12, 32, 22, 16, outline); Rect(t, 15, 34, 17, 13, armour);
        Rect(t, 18, 35, 13, 8, armourLight); Rect(t, 22, 37, 7, 4, cyan);
        Rect(t, 8, 32, 8, 11, outline); Rect(t, 5, 25, 8, 10, outline);
        Rect(t, 9, 33, 5, 8, armourLight); Rect(t, 7, 27, 5, 7, armour);
        Rect(t, 31, 33, 7, 10, outline); Rect(t, 35, 26, 8, 10, outline);
        Rect(t, 33, 34, 4, 7, armourLight); Rect(t, 37, 28, 5, 7, armour);
        Rect(t, 13, 17, 10, 17, outline); Rect(t, 15, 18, 7, 15, armour);
        Rect(t, 7, 9, 12, 10, outline); Rect(t, 9, 11, 10, 7, armourLight);
        Rect(t, 25, 18, 10, 16, outline); Rect(t, 28, 10, 9, 11, outline);
        Rect(t, 27, 19, 7, 14, armour); Rect(t, 30, 11, 6, 9, armourLight);
        Rect(t, 32, 6, 12, 6, outline); Rect(t, 34, 8, 9, 3, armourLight);
        Rect(t, 12, 30, 24, 5, joint);
        Pixel(t, 16, 44, cyanHot); Pixel(t, 31, 44, cyan);
        Save(t, "player_runner.png");
    }

    private static void WriteAndroid()
    {
        Texture2D t = NewCanvas(48, 64, "DemoFinal_IndustrialAndroid");
        Color32 outline = new Color32(7, 9, 13, 255);
        Color32 metal = new Color32(40, 46, 51, 255);
        Color32 metalLight = new Color32(78, 86, 90, 255);
        Color32 dark = new Color32(19, 23, 28, 255);
        Color32 red = new Color32(255, 35, 52, 255);
        Color32 redHot = new Color32(255, 166, 158, 255);
        Rect(t, 14, 45, 20, 14, outline); Rect(t, 16, 47, 16, 10, dark);
        Rect(t, 18, 50, 12, 4, red); Rect(t, 21, 51, 6, 2, redHot);
        Rect(t, 8, 31, 32, 9, outline); Rect(t, 11, 33, 26, 5, metalLight);
        Rect(t, 13, 25, 22, 17, outline); Rect(t, 16, 27, 16, 13, metal);
        Rect(t, 20, 29, 8, 8, dark); Rect(t, 22, 31, 4, 5, red);
        Rect(t, 7, 21, 8, 13, outline); Rect(t, 9, 23, 5, 10, metal);
        Rect(t, 34, 21, 8, 13, outline); Rect(t, 35, 23, 5, 10, metal);
        Rect(t, 14, 7, 9, 19, outline); Rect(t, 16, 9, 6, 16, dark);
        Rect(t, 27, 7, 9, 19, outline); Rect(t, 28, 9, 6, 16, dark);
        Rect(t, 11, 4, 13, 5, outline); Rect(t, 27, 4, 13, 5, outline);
        Save(t, "android_industrial.png");
    }

    private static void WritePowerCell(string filename, Color32 accent, Color32 hot)
    {
        Texture2D t = NewCanvas(32, 32, filename);
        Color32 frame = new Color32(12, 27, 35, 255);
        Color32 frameLight = new Color32(59, 82, 88, 255);
        Rect(t, 9, 4, 14, 24, frame); Rect(t, 7, 8, 18, 16, frame);
        Rect(t, 10, 6, 12, 20, frameLight); Rect(t, 11, 8, 10, 16, accent);
        Rect(t, 13, 10, 6, 12, hot); Rect(t, 11, 3, 10, 3, accent); Rect(t, 11, 26, 10, 3, accent);
        Save(t, filename);
    }

    private static void WritePortal(string filename, Color32 accent, Color32 field)
    {
        Texture2D t = NewCanvas(64, 80, filename);
        Color32 dark = new Color32(10, 28, 35, 255);
        Color32 metal = new Color32(31, 61, 69, 255);
        Rect(t, 7, 7, 11, 64, dark); Rect(t, 46, 7, 11, 64, dark);
        Rect(t, 10, 10, 5, 58, metal); Rect(t, 49, 10, 5, 58, metal);
        Rect(t, 12, 12, 2, 54, accent); Rect(t, 50, 12, 2, 54, accent);
        Rect(t, 13, 66, 39, 8, dark); Rect(t, 16, 69, 32, 2, accent);
        Rect(t, 19, 13, 26, 52, field); Rect(t, 30, 15, 4, 48, new Color32(accent.r, accent.g, accent.b, 145));
        Rect(t, 4, 4, 18, 6, dark); Rect(t, 42, 4, 18, 6, dark);
        Save(t, filename);
    }
}
#endif
