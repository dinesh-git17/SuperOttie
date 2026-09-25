using UnityEditor;
using UnityEngine;

namespace SuperOttie.Editor
{
    /// <summary>
    /// Folder-based import conventions so art and audio dropped into the project are configured
    /// correctly without hand-editing importer settings.
    /// </summary>
    public sealed class AssetImportRules : AssetPostprocessor
    {
        public const int SpritePixelsPerUnit = 128;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 2048;

            if (assetPath.Contains("/AppIcon/"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.npotScale = TextureImporterNPOTScale.None;
                return;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // required for tiled draw mode
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.spriteAlignment = (int)(UsesFeetPivot(assetPath) ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            settings.spritePixelsPerUnit = assetPath.Contains("/Sprites/") ? SpritePixelsPerUnit : 100;
            ti.SetTextureSettings(settings);
        }

        /// <summary>Characters and props stand on the ground, so their pivot is at their feet.</summary>
        public static bool UsesFeetPivot(string path) =>
            path.Contains("/Sprites/Player/") || path.Contains("/Sprites/Enemies/") || path.Contains("/Sprites/Decor/");

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            if (assetPath.Contains("/Music/"))
            {
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                ai.forceToMono = false;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
                ai.forceToMono = true;
            }
            ai.loadInBackground = false;
            ai.defaultSampleSettings = s;
        }
    }
}
