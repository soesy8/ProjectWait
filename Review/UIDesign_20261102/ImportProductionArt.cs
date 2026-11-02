using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using Object = UnityEngine.Object;

public static class ImportProductionArt
{
    public static object Main() => Import("");
    public static object Cartoon() => Import("Cartoon/");
    static object Import(string variant)
    {
        string root = "Assets/M/UI/Storybook/Art/" + variant;
        var report = new List<object>();
        AssetDatabase.Refresh();
        foreach (var path in Directory.GetFiles(root, "*.png"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(path));
            int width = source.width, height = source.height;
            int left = width, right = 0, bottom = height, top = 0;
            var pixels = source.GetPixels32();
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= (name == "SpeechTail" ? 160 : 24)) continue;
                left = Math.Min(left, x); right = Math.Max(right, x);
                bottom = Math.Min(bottom, y); top = Math.Max(top, y);
            }
            bool transparent = pixels.Any(p => p.a == 0);
            Object.DestroyImmediate(source);
            if (!transparent || right <= left) throw new Exception(name + " has no usable transparency.");
            left = Math.Max(0, left - 4); right = Math.Min(width, right + 5);
            bottom = Math.Max(0, bottom - 4); top = Math.Min(height, top + 5);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new Exception("Missing importer: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new Exception("No sprite provider for " + name);
            provider.InitSpriteEditorDataProvider();
            var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
            if (edit == null) throw new Exception("Sprite editing capability unavailable: " + name);
            var capability = edit.GetEditCapability();
            foreach (var required in new[] { EEditCapability.EditSpriteRect, EEditCapability.EditBorder, EEditCapability.EditPivot, EEditCapability.CreateAndDeleteSprite })
                if (!capability.HasCapability(required)) throw new Exception("Sprite capability not supported: " + required);
            var rect = new Rect(left, bottom, right - left, top - bottom);
            var old = provider.GetSpriteRects().FirstOrDefault(s => s.name == name);
            var id = old == null ? GUID.Generate() : old.spriteID;
            Vector4 border = Vector4.zero;
            if (name == "DialogueFrame") border = new Vector4(rect.width * .14f, rect.height * .18f, rect.width * .14f, rect.height * .18f);
            if (name.StartsWith("Button")) border = new Vector4(rect.width * .16f, rect.height * .28f, rect.width * .16f, rect.height * .28f);
            if (name == "ObservationNote") border = new Vector4(rect.width * .25f, rect.height * .25f, rect.width * .21f, rect.height * .33f);
            var sprite = new SpriteRect { name = name, rect = rect, pivot = Vector2.one * .5f, alignment = SpriteAlignment.Center, border = border, spriteID = id };
            provider.SetSpriteRects(new[] { sprite });
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names == null) throw new Exception("Sprite name mapping unavailable: " + name);
            names.SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(name, id) });
            provider.Apply(); importer.SaveAndReimport();
            var asset = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
            report.Add(new { name, width, height, transparent, rect = new[] { rect.x, rect.y, rect.width, rect.height }, border = new[] { border.x, border.y, border.z, border.w }, imported = new[] { asset.rect.width, asset.rect.height } });
        }
        File.WriteAllText("Review/UIDesign_20261102/" + (variant == "" ? "" : "cartoon-") + "art-import.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
