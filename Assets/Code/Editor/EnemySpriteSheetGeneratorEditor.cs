#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Herramienta de Editor para configurar automáticamente todas las hojas de sprites de enemigos
/// en Assets/Sprites/ (FilterMode.Point, Uncompressed, Readable, Grid Slice) y generar sus
/// AnimationClips y AnimatorControllers en Assets/Animations/.
/// </summary>
public static class EnemySpriteSheetGeneratorEditor
{
    public struct EnemySheetSpec
    {
        public string assetName;
        public int cols;
        public int rows;
        public float ppu;

        public EnemySheetSpec(string name, int c, int r, float p = 16f)
        {
            assetName = name;
            cols = c;
            rows = r;
            ppu = p;
        }
    }

    private static readonly EnemySheetSpec[] AllEnemySpecs = new[]
    {
        new EnemySheetSpec("SombraErrante_SpriteSheet.png", 8, 1, 64f),
        new EnemySheetSpec("SombraLanzadora_SpriteSheet.png", 8, 1, 64f),
        new EnemySheetSpec("SombraGuardiana_SpriteSheet.png", 8, 1, 52f),
        new EnemySheetSpec("goblin_spritesheet.png", 6, 4, 16f),
        new EnemySheetSpec("goblin_bomber_spritesheet.png", 6, 4, 16f),
        new EnemySheetSpec("slime_spritesheet.png", 6, 4, 16f),
        new EnemySheetSpec("mushroom_spritesheet.png", 8, 4, 16f),
        new EnemySheetSpec("worm_spritesheet.png", 6, 4, 16f),
        new EnemySheetSpec("Orc-Grunt.png", 24, 8, 16f),
        new EnemySheetSpec("Orc-Peon-Cyan.png", 24, 8, 16f),
        new EnemySheetSpec("Orc-Peon-Red.png", 24, 8, 16f),
        new EnemySheetSpec("Orc-Soldier-Cyan.png", 24, 8, 16f),
        new EnemySheetSpec("Orc-Soldier-Red.png", 24, 8, 16f),
        new EnemySheetSpec("Mage-Cyan.png", 24, 8, 16f),
        new EnemySheetSpec("Mage-Red.png", 24, 8, 16f),
        new EnemySheetSpec("Soldier-Blue.png", 24, 8, 16f),
        new EnemySheetSpec("Soldier-Red.png", 24, 8, 16f),
        new EnemySheetSpec("Soldier-Yellow.png", 24, 8, 16f),
        new EnemySheetSpec("Human-Soldier-Red.png", 24, 8, 16f),
        new EnemySheetSpec("Warrior-Blue.png", 24, 8, 16f),
        new EnemySheetSpec("Warrior-Red.png", 24, 8, 16f)
    };

    [MenuItem("Tools/Ecos de San Felipe/Generar y Configurar Hojas de Enemigos")]
    [MenuItem("San Felipe/👾 Generar y Configurar Hojas de Enemigos")]
    public static void ConfigurarTodasLasHojasDeEnemigos()
    {
        System.IO.Directory.CreateDirectory("Assets/Animations");
        int configurados = 0;

        foreach (EnemySheetSpec spec in AllEnemySpecs)
        {
            string path = "Assets/Sprites/" + spec.assetName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = spec.ppu;
            importer.isReadable = true;
            importer.SaveAndReimport();

            GenerarClipsBasicosParaEnemigo(path, spec);
            configurados++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green><b>[Ecos de San Felipe]</b> ¡{configurados} hojas de enemigos configuradas y animadas en Assets/Animations/!</color>");
    }

    private static void GenerarClipsBasicosParaEnemigo(string texturePath, EnemySheetSpec spec)
    {
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object obj in allAssets)
        {
            if (obj is Sprite sp) sprites.Add(sp);
        }

        if (sprites.Count < 2) return;

        string baseName = System.IO.Path.GetFileNameWithoutExtension(spec.assetName);
        if (baseName.StartsWith("Sombra")) return; // Las 3 Sombras ya tienen sus clips Vuelo y Ataque dedicados

        int idleCount = Mathf.Min(4, sprites.Count);
        Sprite[] idleFrames = sprites.GetRange(0, idleCount).ToArray();
        string clipPath = $"Assets/Animations/{baseName}_Idle.anim";

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNew = false;
        if (clip == null)
        {
            clip = new AnimationClip();
            isNew = true;
        }
        else
        {
            clip.ClearCurves();
        }

        clip.frameRate = 8f;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[idleFrames.Length];
        for (int i = 0; i < idleFrames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / 8f,
                value = idleFrames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew) AssetDatabase.CreateAsset(clip, clipPath);
        else EditorUtility.SetDirty(clip);
    }
}
#endif

