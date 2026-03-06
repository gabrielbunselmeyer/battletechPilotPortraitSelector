using System;
using UnityEngine;
using UnityEngine.UI;

namespace ErasPortraitSelector.ui
{
    static class BTStyle
    {
        // Panels & chrome
        public static readonly Color PanelBg =
            new Color32(15, 17, 26, 250);
        public static readonly Color PanelBorder =
            new Color32(62, 64, 92, 255);
        public static readonly Color TitleBg =
            new Color32(28, 30, 48, 255);
        public static readonly Color ViewportBg =
            new Color32(10, 12, 20, 255);
        public static readonly Color Backdrop =
            new Color(0f, 0f, 0.02f, 0.82f);

        // Accent
        public static readonly Color AccentOrange =
            new Color32(247, 155, 38, 255);
        public static readonly Color DividerLine =
            new Color32(247, 155, 38, 180);

        // Buttons
        public static readonly Color BtnNormal =
            new Color32(50, 52, 78, 255);
        public static readonly Color CancelBtnBg =
            new Color32(120, 30, 30, 255);

        // Text
        public static readonly Color TextPrimary =
            new Color32(220, 220, 225, 255);
        public static readonly Color TextSecondary =
            new Color32(155, 158, 170, 255);
        public static readonly Color TextHighlight =
            new Color32(247, 155, 38, 255);

        // Portrait cells
        public static readonly Color CellBg =
            new Color32(24, 26, 42, 255);
        public static readonly Color CellBorder =
            new Color32(50, 52, 76, 255);
        public static readonly Color ErrorCell =
            new Color32(80, 30, 30, 255);

        // Scrollbar
        public static readonly Color ScrollTrack =
            new Color32(18, 18, 32, 255);
        public static readonly Color ScrollHandle =
            new Color32(62, 64, 96, 255);

        // Runtime font discovery
        private static bool fontResolved;
        private static Font cachedFont;

        public static Font GetFont()
        {
            if (!fontResolved)
            {
                fontResolved = true;
                cachedFont = DiscoverFont();
            }
            return cachedFont ??
                Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Font DiscoverFont()
        {
            try
            {
                // Search all loaded Text components for a
                // non-default font that BattleTech is using.
                Text[] allText = Resources.FindObjectsOfTypeAll<Text>();
                Font best = null;

                foreach (Text t in allText)
                {
                    if (t.font == null)
                    {
                        continue;
                    }

                    string n = t.font.name.ToLowerInvariant();

                    if (n == "arial" || n.Contains("fallback"))
                    {
                        continue;
                    }

                    best = t.font;

                    if (n.Contains("unispace") ||
                        n.Contains("rajdhani") ||
                        n.Contains("mech"))
                    {
                        Mod.Log($"Found BT font: {t.font.name}");
                        return t.font;
                    }
                }

                if (best != null)
                {
                    Mod.Log($"Using discovered font: {best.name}");
                    return best;
                }

            }
            catch (Exception e)
            {
                Mod.Log($"Font discovery: {e.Message}");
            }

            return null;
        }
    }
}
