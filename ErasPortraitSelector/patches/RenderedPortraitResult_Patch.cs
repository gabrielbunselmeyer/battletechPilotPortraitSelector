using System;
using System.Collections.Generic;
using BattleTech.Portraits;
using ErasPortraitSelector.helpers;
using HarmonyLib;
using UnityEngine;

namespace ErasPortraitSelector.Patches
{
    /// <summary>
    /// Patches the portrait rendering to return our custom texture
    /// when the game re-renders portraits (e.g., leaving and returning
    /// to barracks). Works alongside CPL's own patch.
    /// </summary>
    [HarmonyPatch(typeof(RenderedPortraitResult), "get_Item")]
    [HarmonyPriority(Priority.First)]
    public static class RenderedPortraitResult_GetItem_Patch
    {
        // Cache of icon name -> Texture2D to avoid reloading from disk
        internal static Dictionary<string, Texture2D> textureCache
            = new Dictionary<string, Texture2D>();

        static void Postfix(RenderedPortraitResult __instance,
            ref Texture2D __result)
        {
            try
            {
                // Check if settings has a custom icon
                if (__instance.settings == null)
                {
                    return;
                }

                if (__instance.settings.Description == null)
                {
                    return;
                }

                string icon = __instance.settings.Description.Icon;
                if (string.IsNullOrEmpty(icon))
                {
                    return;
                }

                // Only intercept if this is a portrait we've applied
                if (!PilotDef_GetPortraitSprite_Patch
                        .appliedPortraits.ContainsKey(icon))
                {
                    return;
                }

                // Check texture cache first
                if (textureCache.TryGetValue(icon, out Texture2D cached))
                {
                    if (cached != null)
                    {
                        __result = cached;
                        return;
                    }
                }

                // Load from disk
                Texture2D tex = TextureLoadingHelper.LoadTextureForIcon(icon);
                if (tex != null)
                {
                    textureCache[icon] = tex;
                    __result = tex;
                }
            }
            catch (Exception e)
            {
                Mod.Log($"RenderedPortraitResult patch error: {e.Message}");
            }
        }
    }
}
