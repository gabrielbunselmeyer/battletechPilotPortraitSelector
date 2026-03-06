using System;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ErasPortraitSelector.helpers;

namespace ErasPortraitSelector.Patches
{
    /// <summary>
    /// After each roster slot refreshes, check if we have a custom
    /// portrait for this pilot and force-set it.
    /// Also hooks Ctrl+Click for the portrait picker.
    /// </summary>
    [HarmonyPatch(typeof(SGBarracksRosterSlot), "Refresh")]
    public static class SGBarracksRosterSlot_Refresh_Patch
    {
        private static readonly FieldInfo pilotField =
            typeof(SGBarracksRosterSlot).GetField("pilot",
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Public);

        private static readonly FieldInfo portraitField =
            typeof(SGBarracksRosterSlot).GetField("portrait",
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Public);

        static void Postfix(SGBarracksRosterSlot __instance)
        {
            try
            {
                // Ctrl+Click hook
                HBSDOTweenToggle toggle = Traverse.Create(__instance)
                    .Field("toggle").GetValue<HBSDOTweenToggle>();

                if (toggle != null)
                {
                    var handler = toggle.gameObject
                        .GetComponent<CtrlClickHandler>();
                    if (handler == null)
                    {
                        handler = toggle.gameObject
                            .AddComponent<CtrlClickHandler>();
                    }
                    handler.slot = __instance;
                }

                // Force custom portrait on slot
                if (pilotField == null || portraitField == null)
                {
                    return;
                }

                Pilot pilot = pilotField.GetValue(__instance) as Pilot;
                if (pilot == null)
                {
                    return;
                }

                string icon = pilot.pilotDef.Description.Icon;
                if (string.IsNullOrEmpty(icon))
                {
                    return;
                }

                // Check if this is a portrait we've applied
                if (!PilotDef_GetPortraitSprite_Patch
                        .knownIcons.Contains(icon))
                {
                    return;
                }

                // Get or load the sprite
                Sprite sprite;
                if (PilotDef_GetPortraitSprite_Patch
                        .appliedPortraits.TryGetValue(icon, out sprite)
                    && sprite != null)
                {
                    // Use cached sprite
                }
                else
                {
                    sprite = TextureLoadingHelper.LoadSpriteForIcon(icon);
                    if (sprite != null)
                    {
                        PilotDef_GetPortraitSprite_Patch
                            .appliedPortraits[icon] = sprite;
                    }
                }

                if (sprite == null)
                {
                    return;
                }

                Image portraitImage =
                    portraitField.GetValue(__instance) as Image;
                if (portraitImage != null)
                {
                    portraitImage.sprite = sprite;
                }
            }
            catch (Exception e)
            {
                Mod.Log($"Refresh postfix error: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Detects Ctrl+Click on roster slot and opens portrait picker.
    /// </summary>
    public class CtrlClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public SGBarracksRosterSlot slot;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Input.GetKey(KeyCode.LeftControl) &&
                !Input.GetKey(KeyCode.RightControl))
            {
                return;
            }

            if (slot == null)
            {
                return;
            }

            try
            {
                Pilot pilot = Traverse.Create(slot)
                    .Field("pilot").GetValue<Pilot>();
                if (pilot == null)
                {
                    return;
                }

                SGBarracksWidget barracks =
                    slot.GetComponentInParent<SGBarracksWidget>();
                if (barracks == null)
                {
                    return;
                }

                SGBarracksMWDetailPanel detailPanel =
                    Traverse.Create(barracks)
                        .Field("mechWarriorDetails")
                        .GetValue<SGBarracksMWDetailPanel>();

                SGBarracksDossierPanel dossier = null;
                try
                {
                    dossier = Traverse.Create(detailPanel)
                        .Field("dossier")
                        .GetValue<SGBarracksDossierPanel>();
                }
                catch { }

                Mod.Log($"Ctrl+Click: opening picker for {pilot.Callsign}");
                PortraitPickerPopup.Show(pilot, detailPanel, barracks, dossier);
            }
            catch (Exception e)
            {
                Mod.Log($"CtrlClickHandler error: {e}");
            }
        }
    }
}
