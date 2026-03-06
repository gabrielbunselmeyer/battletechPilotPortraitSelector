using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ErasPortraitSelector.helpers
{
    static class TextureLoadingHelper
    {
        private static bool ddsLoaderResolved = false;
        private static MethodInfo loadTextureDXTMethod = null;

        private const int THUMB_SIZE = 128;

        /// <summary>
        /// Shared method to load a portrait texture by icon name,
        /// searching all configured paths for .png and .dds files.
        /// </summary>
        public static Texture2D LoadTextureForIcon(string iconName)
        {
            List<string> searchPaths = ImageSearchHelper.GetImageSearchPaths();

            foreach (string searchPath in searchPaths)
            {
                string ddsPath = Path.Combine(
                    searchPath, iconName + ".dds");

                if (File.Exists(ddsPath))
                {
                    Texture2D tex = LoadTextureFromFile(ddsPath);
                    if (tex != null)
                    {
                        return tex;
                    }
                }

                string pngPath = Path.Combine(
                    searchPath, iconName + ".png");

                if (File.Exists(pngPath))
                {
                    Texture2D tex = LoadTextureFromFile(pngPath);
                    if (tex != null)
                    {
                        return tex;
                    }
                }
            }

            return null;
        }

        public static Sprite LoadSpriteForIcon(string iconName)
        {
            Texture2D tex = LoadTextureForIcon(iconName);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
        }

        public static Texture2D LoadTextureFromFile(string path)
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);

                if (path.EndsWith(".dds",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Texture2D cplResult = TryLoadDDSViaCPL(data);
                    if (cplResult != null)
                    {
                        return cplResult;
                    }

                    return LoadDDS(data);
                }

                Texture2D tex = new(2, 2);
                if (tex.LoadImage(data))
                {
                    return tex;
                }
            }
            catch (Exception e)
            {
                Mod.Log(
                    $"Failed to load texture {path}: {e.Message}");
            }
            return null;
        }

        private static Texture2D TryLoadDDSViaCPL(byte[] data)
        {
            if (!ddsLoaderResolved)
            {
                ddsLoaderResolved = true;
                try
                {
                    foreach (var asm in
                        AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (asm.GetName().Name ==
                            "CommanderPortraitLoader")
                        {
                            Type tmType = asm.GetType(
                                "CommanderPortraitLoader.TextureManager");
                            if (tmType != null)
                            {
                                loadTextureDXTMethod = tmType.GetMethod(
                                    "LoadTextureDXT",
                                    BindingFlags.Public |
                                    BindingFlags.Static);
                            }
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Mod.Log(
                        $"CPL DDS loader lookup: {e.Message}");
                }
            }

            if (loadTextureDXTMethod != null)
            {
                try
                {
                    return (Texture2D)loadTextureDXTMethod.Invoke(
                        null, [data]);
                }
                catch { }
            }
            return null;
        }

        private static Texture2D LoadDDS(byte[] data)
        {
            try
            {
                if (data.Length < 128)
                {
                    return null;
                }

                if (data[0] != 0x44 || data[1] != 0x44 ||
                    data[2] != 0x53 || data[3] != 0x20)
                {
                    return null;
                }

                int height = BitConverter.ToInt32(data, 12);
                int width = BitConverter.ToInt32(data, 16);
                int fourCC = BitConverter.ToInt32(data, 84);
                TextureFormat format;

                if (fourCC == 0x31545844)
                {
                    format = TextureFormat.DXT1;
                }
                else if (fourCC == 0x35545844)
                {
                    format = TextureFormat.DXT5;
                }
                else
                {
                    Mod.Log(
                        $"Unsupported DDS FourCC: {fourCC}");
                    return null;
                }

                int dataOffset = 128;
                int dataLength = data.Length - dataOffset;
                byte[] pixelData = new byte[dataLength];
                Array.Copy(data, dataOffset, pixelData, 0, dataLength);

                Texture2D tex = new Texture2D(
                    width, height, format, false);
                tex.LoadRawTextureData(pixelData);
                tex.Apply();
                return tex;
            }
            catch (Exception e)
            {
                Mod.Log(
                    $"DDS load error: {e.Message}");
                return null;
            }
        }

        public static Texture2D LoadAndResizeTexture(string path)
        {
            try
            {
                Texture2D fullTex = LoadTextureFromFile(path);
                if (fullTex == null)
                {
                    return null;
                }

                return DownscaleTexture(fullTex, THUMB_SIZE);
            }
            catch { }
            return null;
        }

        private static Texture2D DownscaleTexture(
            Texture2D source, int maxSize)
        {
            if (source.width <= maxSize && source.height <= maxSize)
            {
                return source;
            }

            float ratio = Mathf.Min(
                (float)maxSize / source.width,
                (float)maxSize / source.height);
            int newW = Mathf.Max(1, (int)(source.width * ratio));
            int newH = Mathf.Max(1, (int)(source.height * ratio));

            RenderTexture rt = RenderTexture.GetTemporary(newW, newH);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(newW, newH,
                TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(source);

            return result;
        }
    }
}
