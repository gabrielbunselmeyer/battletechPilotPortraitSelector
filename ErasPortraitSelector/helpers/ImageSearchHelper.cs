using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace ErasPortraitSelector.helpers
{
    static class ImageSearchHelper
    {
        private static bool cplPathsResolved = false;
        private static List<string> cachedImagePaths = null;

        private static List<PortraitEntry> cachedPortraitList = null;

        public static List<string> GetImageSearchPaths()
        {
            ResolveCommanderPortraitLoaderPaths();

            if (cachedImagePaths != null && cachedImagePaths.Count > 0)
            {
                return cachedImagePaths;
            }

            return new List<string>
            {
                Path.Combine(
                    Mod.ModDirectory, "Portraits")
            };
        }

        private static void ResolveCommanderPortraitLoaderPaths()
        {
            if (cplPathsResolved)
            {
                return;
            }

            cplPathsResolved = true;

            try
            {
                foreach (var asm in
                    AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "CommanderPortraitLoader")
                    {
                        Type cplType = asm.GetType(
                            "CommanderPortraitLoader" +
                            ".CommanderPortraitLoader");
                        if (cplType != null)
                        {
                            var imgField = cplType.GetField(
                                "searchablePaths",
                                BindingFlags.Public |
                                BindingFlags.Static);
                            if (imgField?.GetValue(null)
                                is List<string> p && p.Count > 0)
                            {
                                cachedImagePaths = p;
                                Mod.Log(
                                    $"Found CPL paths: " +
                                    $"{p.Count} entries");
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Mod.Log(
                    $"CPL detection error: {e.Message}");
            }
        }

        public static List<PortraitEntry> ScanPortraitFiles()
        {
            if (cachedPortraitList != null)
            {
                return cachedPortraitList;
            }

            List<PortraitEntry> result = new List<PortraitEntry>();
            List<string> searchPaths = GetImageSearchPaths();
            string[] extensions = { "*.png", "*.dds" };
            HashSet<string> seenNames = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath))
                {
                    continue;
                }

                foreach (string ext in extensions)
                {
                    foreach (string file in
                        Directory.GetFiles(searchPath, ext))
                    {
                        string name =
                            Path.GetFileNameWithoutExtension(file);
                        if (!seenNames.Add(name))
                        {
                            continue;
                        }

                        result.Add(new PortraitEntry
                        {
                            Name = name,
                            FullPath = file
                        });
                    }
                }
            }

            result.Sort((a, b) =>
                string.Compare(a.Name, b.Name,
                    StringComparison.OrdinalIgnoreCase));

            cachedPortraitList = result;
            return result;
        }
    }
}
