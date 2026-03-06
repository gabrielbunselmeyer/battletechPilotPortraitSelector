using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;

namespace ErasPortraitSelector
{
    public class ModSettings
    {
        public bool DebugLogging = false;
    }

    public static class Mod
    {
        internal static string ModDirectory;
        private static string LogPath;

        public static ModSettings Settings = new();
        internal static bool ShouldLog => Settings.DebugLogging;

        public static void Init(string directory, string settingsJSON)
        {
            try
            {
                if (!string.IsNullOrEmpty(settingsJSON))
                {
                    Settings = JsonConvert.DeserializeObject<ModSettings>(settingsJSON) ?? new ModSettings();
                }
            }
            catch (Exception) { }

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
