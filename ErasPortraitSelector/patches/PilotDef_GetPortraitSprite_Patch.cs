using System;
using System.Collections.Generic;
using BattleTech;
using ErasPortraitSelector.helpers;
using HarmonyLib;
using UnityEngine;

namespace ErasPortraitSelector.Patches
{
    [HarmonyPatch(typeof(PilotDef), "GetPortraitSprite")]
    [HarmonyPriority(Priority.Last)]
    public static class PilotDef_GetPortraitSprite_Patch
    {
        // Portraits we've applied this session
        internal static Dictionary<string, Sprite> appliedPortraits
                = new Dictionary<string, Sprite>();

        // Icons we know we've set — even if sprite was garbage collected
        internal static HashSet<string> knownIcons = new HashSet<string>();

        static void Postfix(PilotDef __instance, ref Sprite __result)
        {
            try
            {
                string icon = __instance.Description.Icon;
                if (string.IsNullOrEmpty(icon))
                {
                    return;
                }

                // Check our sprite cache
                if (appliedPortraits.TryGetValue(icon, out Sprite cached))
                {
                    if (cached != null)
                    {
                        __result = cached;
                        return;
                    }
                    // Sprite was destroyed — remove and reload below
                    appliedPortraits.Remove(icon);
                }

                // Only reload for icons we've previously set
                if (knownIcons.Contains(icon))
                {
                    Sprite loaded =
                        TextureLoadingHelper.LoadSpriteForIcon(icon);
                    if (loaded != null)
                    {
                        appliedPortraits[icon] = loaded;
                        __result = loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Mod.Log($"GetPortraitSprite patch error: {e.Message}");
            }
        }
    }
}
