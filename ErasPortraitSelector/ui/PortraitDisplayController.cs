using System;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using ErasPortraitSelector.helpers;


namespace ErasPortraitSelector.ui
{
    static class PortraitDisplayController
    {
        public static void ApplyPortrait(
            Pilot pilot, string portraitName, string portraitPath,
            SGBarracksMWDetailPanel detailPanel,
            SGBarracksWidget barracks,
            SGBarracksDossierPanel dossier)
        {
            try
            {
                Mod.Log(
                    $"Applying portrait '{portraitName}' to " +
                    $"'{pilot.Callsign}'");

                // 1. Set the icon name — serialized to save
                pilot.pilotDef.Description.SetIcon(portraitName);

                // 2. Null out PortraitSettings so CPL uses icon on reload
                pilot.pilotDef.PortraitSettings = null;

                // 3. Load full image, register in caches
                Texture2D fullTex = TextureLoadingHelper.LoadTextureFromFile(portraitPath);
                if (fullTex != null)
                {
                    Sprite newSprite = Sprite.Create(fullTex,
                        new Rect(0, 0, fullTex.width, fullTex.height),
                        new Vector2(0.5f, 0.5f));

                    // Destroy old sprite/texture to avoid GPU memory leak
                    if (Patches.PilotDef_GetPortraitSprite_Patch
                            .appliedPortraits.TryGetValue(portraitName, out Sprite oldSprite)
                        && oldSprite != null)
                    {
                        if (oldSprite.texture != null)
                        {
                            UnityEngine.Object.Destroy(oldSprite.texture);
                        }

                        UnityEngine.Object.Destroy(oldSprite);
                    }
                    if (Patches.RenderedPortraitResult_GetItem_Patch
                            .textureCache.TryGetValue(portraitName, out Texture2D oldTex)
                        && oldTex != null)
                    {
                        UnityEngine.Object.Destroy(oldTex);
                    }
                    Patches.RenderedPortraitResult_GetItem_Patch
                        .textureCache.Remove(portraitName);

                    Patches.PilotDef_GetPortraitSprite_Patch
                        .appliedPortraits[portraitName] = newSprite;
                    Patches.PilotDef_GetPortraitSprite_Patch
                        .knownIcons.Add(portraitName);

                    Mod.Log(
                        "Registered sprite in caches");
                }

                // 4. Refresh the big portrait
                detailPanel.DisplayPilot(pilot);

                // 5. Find and refresh the matching roster slot
                //    Our Refresh postfix will set the sprite
                ForceRefreshRosterSlot(barracks, pilot);
            }
            catch (Exception e)
            {
                Mod.Log(
                    $"Error applying portrait: {e}");
            }
        }

        private static readonly FieldInfo slotPilotField =
            typeof(SGBarracksRosterSlot).GetField("pilot",
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Public);

        private static void ForceRefreshRosterSlot(
            SGBarracksWidget barracks, Pilot pilot)
        {
            try
            {
                if (slotPilotField == null)
                {
                    return;
                }

                SGBarracksRosterSlot[] allSlots =
                    barracks.GetComponentsInChildren<SGBarracksRosterSlot>(
                        true);

                if (allSlots == null)
                {
                    return;
                }

                foreach (SGBarracksRosterSlot slot in allSlots)
                {
                    Pilot slotPilot =
                        slotPilotField.GetValue(slot) as Pilot;
                    if (slotPilot == null)
                    {
                        continue;
                    }

                    if (slotPilot.GUID == pilot.GUID)
                    {
                        // Call Refresh — our postfix will set the sprite
                        slot.Refresh();
                        Mod.Log(
                            $"Forced refresh on roster slot for " +
                            $"{pilot.Callsign}");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Mod.Log(
                    $"ForceRefreshRosterSlot error: {e.Message}");
            }
        }
    }
}
