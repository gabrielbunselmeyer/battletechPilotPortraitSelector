using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace ErasPortraitSelector
{
    public static class Mod
    {
        internal static string ModDirectory;
        private static string LogPath;

        internal static readonly bool ShouldLog = false;

        public static void Init(string directory, string settingsJSON)
        {
            ModDirectory = directory;
            LogPath = Path.Combine(directory, "ErasPortraitSelector.log");
            File.WriteAllText(LogPath, "");

            var harmony = new Harmony("era.ErasPortraitSelector");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log("Initialized");
        }

        public static void Log(string message)
        {
            if (!ShouldLog)
            {
                return;
            }

            try
            {
                using var writer = new StreamWriter(LogPath, true);
                writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
            catch { }
        }
    }
}
