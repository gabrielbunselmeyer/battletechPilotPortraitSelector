using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using UnityEngine.UI;
using ErasPortraitSelector.ui;
using ErasPortraitSelector.helpers;

namespace ErasPortraitSelector
{
    public static class PortraitPickerPopup
    {
        private static GameObject popupRoot;
        private static ScrollRect activeScrollRect;
        private static float savedScrollPosition = 1f;
        private static readonly Dictionary<string, Sprite> thumbnailCache
            = new Dictionary<string, Sprite>();

        public static void Show(
            Pilot pilot,
            SGBarracksMWDetailPanel detailPanel,
            SGBarracksWidget barracks,
            SGBarracksDossierPanel dossier)
        {
            List<PortraitEntry> portraits = ImageSearchHelper.ScanPortraitFiles();

            if (portraits.Count == 0)
            {
                GenericPopupBuilder.Create(
                    "Portrait Selector",
                    "No portraits found.\n" +
                    "Place .png files in the " +
                    "CommanderPortraitLoader/Portraits/ folder.")
                    .AddButton("OK", null, true, null)
                    .Render();
                return;
            }

            if (popupRoot != null)
            {
                GameObject.Destroy(popupRoot);
            }

            BuildPopup(pilot, portraits, detailPanel, barracks, dossier);
        }

        private static void BuildPopup(
            Pilot pilot,
            List<PortraitEntry> portraits,
            SGBarracksMWDetailPanel detailPanel,
            SGBarracksWidget barracks,
            SGBarracksDossierPanel dossier)
        {
            Font btFont = BTStyle.GetFont();

            popupRoot = new GameObject("PortraitPickerPopup");
            Canvas canvas = popupRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            CanvasScaler scaler = popupRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            popupRoot.AddComponent<GraphicRaycaster>();
            var runner = popupRoot.AddComponent<CoroutineRunner>();

            // Backdrop
            GameObject backdrop = CreateUIElement("Backdrop",
                popupRoot.transform);
            StretchToFill(backdrop.GetComponent<RectTransform>());
            Image backdropImg = backdrop.AddComponent<Image>();
            backdropImg.color = BTStyle.Backdrop;
            Button backdropBtn = backdrop.AddComponent<Button>();
            backdropBtn.onClick.AddListener(() => Close());

            // Outer border frame
            GameObject borderFrame = CreateUIElement("BorderFrame",
                backdrop.transform);
            RectTransform borderRect =
                borderFrame.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.1f, 0.05f);
            borderRect.anchorMax = new Vector2(0.9f, 0.95f);
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            Image borderImg = borderFrame.AddComponent<Image>();
            borderImg.color = BTStyle.PanelBorder;

            // Main panel (inset 2px inside border)
            GameObject panel = CreateUIElement("Panel",
                borderFrame.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            StretchToFill(panelRect);
            panelRect.offsetMin = new Vector2(2f, 2f);
            panelRect.offsetMax = new Vector2(-2f, -2f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = BTStyle.PanelBg;
            // Block clicks from reaching the backdrop
            panel.AddComponent<Button>();

            // Title bar
            GameObject titleBar = CreateUIElement("TitleBar",
                panel.transform);
            RectTransform titleRect = titleBar.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 52f);
            titleRect.anchoredPosition = Vector2.zero;
            titleBar.AddComponent<Image>().color = BTStyle.TitleBg;

            // Title text
            GameObject titleText = CreateUIElement("TitleText",
                titleBar.transform);
            RectTransform titleTextRect =
                titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(16f, 0f);
            titleTextRect.offsetMax = new Vector2(-56f, 0f);
            Text titleLabel = titleText.AddComponent<Text>();
            titleLabel.text =
                $"SELECT PORTRAIT \u2014 {pilot.Callsign.ToUpperInvariant()}" +
                $"  ({portraits.Count} available)";
            titleLabel.font = btFont;
            titleLabel.fontSize = 20;
            titleLabel.fontStyle = FontStyle.Bold;
            titleLabel.color = BTStyle.TextPrimary;
            titleLabel.alignment = TextAnchor.MiddleLeft;

            // Orange accent line under the title bar
            GameObject accentLine = CreateUIElement("AccentLine",
                panel.transform);
            RectTransform accentRect =
                accentLine.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(0f, 2f);
            accentRect.anchoredPosition = new Vector2(0f, -52f);
            accentLine.AddComponent<Image>().color = BTStyle.DividerLine;

            // Close button
            GameObject closeBtn = CreateUIElement("CloseBtn",
                titleBar.transform);
            RectTransform closeBtnRect =
                closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1f, 0f);
            closeBtnRect.anchorMax = new Vector2(1f, 1f);
            closeBtnRect.pivot = new Vector2(1f, 0.5f);
            closeBtnRect.sizeDelta = new Vector2(52f, 0f);
            closeBtnRect.anchoredPosition = Vector2.zero;
            Image closeBtnImg = closeBtn.AddComponent<Image>();
            closeBtnImg.color = BTStyle.CancelBtnBg;
            Button closeBtnComp = closeBtn.AddComponent<Button>();
            closeBtnComp.targetGraphic = closeBtnImg;
            closeBtnComp.onClick.AddListener(() => Close());
            ColorBlock closeCB = closeBtnComp.colors;
            closeCB.normalColor = Color.white;
            closeCB.highlightedColor = new Color(1.2f, 1f, 1f, 1f);
            closeCB.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            closeBtnComp.colors = closeCB;

            GameObject closeLbl = CreateUIElement("Label",
                closeBtn.transform);
            StretchToFill(closeLbl.GetComponent<RectTransform>());
            Text closeText = closeLbl.AddComponent<Text>();
            closeText.text = "\u2715";
            closeText.font = btFont;
            closeText.fontSize = 22;
            closeText.fontStyle = FontStyle.Bold;
            closeText.color = BTStyle.TextPrimary;
            closeText.alignment = TextAnchor.MiddleCenter;

            // Scroll area
            GameObject scrollArea = CreateUIElement("ScrollArea",
                panel.transform);
            RectTransform scrollAreaRect =
                scrollArea.GetComponent<RectTransform>();
            scrollAreaRect.anchorMin = Vector2.zero;
            scrollAreaRect.anchorMax = Vector2.one;
            scrollAreaRect.offsetMin = new Vector2(10f, 60f);
            scrollAreaRect.offsetMax = new Vector2(-10f, -58f);

            ScrollRect scroll = scrollArea.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 40f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport (leaves room for scrollbar on right)
            GameObject viewport = CreateUIElement("Viewport",
                scrollArea.transform);
            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();
            StretchToFill(viewportRect);
            viewportRect.offsetMax = new Vector2(-14f, 0f);
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = BTStyle.ViewportBg;
            viewport.AddComponent<Mask>().showMaskGraphic = true;
            scroll.viewport = viewportRect;

            // Content
            GameObject content = CreateUIElement("Content",
                viewport.transform);
            RectTransform contentRect =
                content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentRect.anchoredPosition = Vector2.zero;

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(148f, 178f);
            grid.spacing = new Vector2(10f, 10f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            grid.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter fitter =
                content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            activeScrollRect = scroll;

            // Styled vertical scrollbar
            BuildScrollbar(scrollArea, scroll);

            // Create portrait cells
            string currentIcon = pilot.pilotDef.Description.Icon;
            List<PortraitCell> cells = new List<PortraitCell>();

            foreach (PortraitEntry entry in portraits)
            {
                bool isCurrent = (entry.Name == currentIcon);
                cells.Add(BuildPortraitCell(
                    entry, isCurrent, content.transform, btFont,
                    pilot, detailPanel, barracks, dossier));
            }

            // Bottom bar
            GameObject bottomBar = CreateUIElement("BottomBar",
                panel.transform);
            RectTransform bottomRect =
                bottomBar.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.sizeDelta = new Vector2(0f, 52f);
            bottomRect.anchoredPosition = Vector2.zero;
            bottomBar.AddComponent<Image>().color = BTStyle.TitleBg;

            // Orange accent line above the bottom bar
            GameObject bottomAccent = CreateUIElement("BottomAccent",
                panel.transform);
            RectTransform bottomAccentRect =
                bottomAccent.GetComponent<RectTransform>();
            bottomAccentRect.anchorMin = new Vector2(0f, 0f);
            bottomAccentRect.anchorMax = new Vector2(1f, 0f);
            bottomAccentRect.pivot = new Vector2(0.5f, 0f);
            bottomAccentRect.sizeDelta = new Vector2(0f, 2f);
            bottomAccentRect.anchoredPosition = new Vector2(0f, 52f);
            bottomAccent.AddComponent<Image>().color = BTStyle.DividerLine;

            // Cancel button
            GameObject cancelBtn = CreateUIElement("CancelBtn",
                bottomBar.transform);
            RectTransform cancelRect =
                cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0.5f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.5f);
            cancelRect.sizeDelta = new Vector2(160f, 38f);
            Image cancelBtnImg = cancelBtn.AddComponent<Image>();
            cancelBtnImg.color = BTStyle.BtnNormal;
            Button cancelBtnComp = cancelBtn.AddComponent<Button>();
            cancelBtnComp.targetGraphic = cancelBtnImg;
            cancelBtnComp.onClick.AddListener(() => Close());
            ColorBlock cancelCB = cancelBtnComp.colors;
            cancelCB.normalColor = Color.white;
            cancelCB.highlightedColor =
                new Color(1.15f, 1.15f, 1.25f, 1f);
            cancelCB.pressedColor =
                new Color(0.75f, 0.75f, 0.85f, 1f);
            cancelBtnComp.colors = cancelCB;

            Outline cancelOutline = cancelBtn.AddComponent<Outline>();
            cancelOutline.effectColor = BTStyle.PanelBorder;
            cancelOutline.effectDistance = new Vector2(1f, 1f);

            GameObject cancelLbl = CreateUIElement("Label",
                cancelBtn.transform);
            StretchToFill(cancelLbl.GetComponent<RectTransform>());
            Text cancelText = cancelLbl.AddComponent<Text>();
            cancelText.text = "CANCEL";
            cancelText.font = btFont;
            cancelText.fontSize = 15;
            cancelText.fontStyle = FontStyle.Bold;
            cancelText.color = BTStyle.TextPrimary;
            cancelText.alignment = TextAnchor.MiddleCenter;

            runner.StartCoroutine(
                LazyLoadPortraits(cells, scroll, viewport));
        }

        private static void BuildScrollbar(
            GameObject scrollArea, ScrollRect scroll)
        {
            GameObject scrollbarObj = CreateUIElement("Scrollbar",
                scrollArea.transform);
            RectTransform scrollbarRect =
                scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(12f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image scrollTrackImg = scrollbarObj.AddComponent<Image>();
            scrollTrackImg.color = BTStyle.ScrollTrack;

            // Sliding area
            GameObject slidingArea = CreateUIElement("SlidingArea",
                scrollbarObj.transform);
            StretchToFill(slidingArea.GetComponent<RectTransform>());

            // Handle
            GameObject handle = CreateUIElement("Handle",
                slidingArea.transform);
            RectTransform handleRect =
                handle.GetComponent<RectTransform>();
            StretchToFill(handleRect);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = BTStyle.ScrollHandle;

            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImg;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            ColorBlock scrollCB = scrollbar.colors;
            scrollCB.normalColor = Color.white;
            scrollCB.highlightedColor =
                new Color(1.2f, 1.2f, 1.3f, 1f);
            scrollCB.pressedColor =
                new Color(0.9f, 0.9f, 1f, 1f);
            scrollbar.colors = scrollCB;

            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;
        }

        private static PortraitCell BuildPortraitCell(
            PortraitEntry entry, bool isCurrent, Transform parent,
            Font btFont, Pilot pilot,
            SGBarracksMWDetailPanel detailPanel,
            SGBarracksWidget barracks,
            SGBarracksDossierPanel dossier)
        {
            GameObject cell = CreateUIElement(
                $"Portrait_{entry.Name}", parent);

            // Cell background (also serves as button target graphic)
            Image cellBg = cell.AddComponent<Image>();
            cellBg.color = BTStyle.CellBg;
            cellBg.type = Image.Type.Simple;

            // Border: orange for current portrait, subtle for others
            Outline cellOutline = cell.AddComponent<Outline>();
            if (isCurrent)
            {
                cellOutline.effectColor = BTStyle.AccentOrange;
                cellOutline.effectDistance = new Vector2(2f, 2f);
            }
            else
            {
                cellOutline.effectColor = BTStyle.CellBorder;
                cellOutline.effectDistance = new Vector2(1f, 1f);
            }

            // Portrait image area (inset inside cell)
            GameObject imgArea = CreateUIElement("Image",
                cell.transform);
            RectTransform imgRect =
                imgArea.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0f, 0.14f);
            imgRect.anchorMax = new Vector2(1f, 1f);
            imgRect.offsetMin = new Vector2(4f, 0f);
            imgRect.offsetMax = new Vector2(-4f, -4f);
            Image portraitImg = imgArea.AddComponent<Image>();
            portraitImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            portraitImg.type = Image.Type.Simple;
            portraitImg.preserveAspect = true;

            // Label area at bottom of cell
            GameObject labelObj = CreateUIElement("Label",
                cell.transform);
            RectTransform labelRect =
                labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.14f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelObj.AddComponent<Image>().color =
                new Color(0f, 0f, 0f, 0.6f);

            GameObject labelTextObj = CreateUIElement("Text",
                labelObj.transform);
            StretchToFill(labelTextObj.GetComponent<RectTransform>());
            Text label = labelTextObj.AddComponent<Text>();
            string displayName = entry.Name;
            if (displayName.Length > 18)
            {
                displayName = displayName.Substring(0, 16) + "\u2026";
            }

            if (isCurrent)
            {
                displayName = "\u2605 " + displayName;
            }

            label.text = displayName;
            label.font = btFont;
            label.fontSize = 11;
            label.color = isCurrent
                ? BTStyle.TextHighlight
                : BTStyle.TextSecondary;
            label.alignment = TextAnchor.MiddleCenter;

            // Button behaviour on whole cell
            Button btn = cell.AddComponent<Button>();
            btn.targetGraphic = cellBg;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(1.3f, 1.3f, 1.5f, 1f);
            colors.pressedColor =
                new Color(0.8f, 0.8f, 0.9f, 1f);
            btn.colors = colors;

            string capturedName = entry.Name;
            string capturedPath = entry.FullPath;
            btn.onClick.AddListener(() =>
            {
                PortraitDisplayController.ApplyPortrait(pilot, capturedName, capturedPath,
                    detailPanel, barracks, dossier);

                Close();
            });

            return new PortraitCell
            {
                Entry = entry,
                ImageComponent = portraitImg,
                CellObject = cell
            };
        }

        private static IEnumerator LazyLoadPortraits(
            List<PortraitCell> cells,
            ScrollRect scroll,
            GameObject viewport)
        {
            HashSet<int> loadedIndices = new HashSet<int>();

            yield return null;
            yield return null;

            if (scroll.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    scroll.content);
            }

            yield return null;

            // Restore saved scroll position
            scroll.verticalNormalizedPosition = savedScrollPosition;

            RectTransform viewportRect =
                viewport.GetComponent<RectTransform>();

            int initialBatch = 30;
            for (int i = 0; i < cells.Count && i < initialBatch; i++)
            {
                if (cells[i].CellObject == null)
                {
                    continue;
                }

                Sprite sprite = GetOrLoadThumbnail(cells[i].Entry);
                if (sprite != null)
                {
                    cells[i].ImageComponent.sprite = sprite;
                    cells[i].ImageComponent.color = Color.white;
                }
                else
                {
                    cells[i].ImageComponent.color = BTStyle.ErrorCell;
                }
                loadedIndices.Add(i);

                if ((i + 1) % 4 == 0)
                {
                    yield return null;
                }
            }

            while (popupRoot != null)
            {
                int loadedThisFrame = 0;

                for (int i = 0; i < cells.Count; i++)
                {
                    if (loadedIndices.Contains(i))
                    {
                        continue;
                    }

                    if (cells[i].CellObject == null)
                    {
                        continue;
                    }

                    if (!IsVisibleInViewport(
                            cells[i].CellObject
                                .GetComponent<RectTransform>(),
                            viewportRect))
                    {
                        continue;
                    }

                    Sprite sprite = GetOrLoadThumbnail(cells[i].Entry);
                    if (sprite != null)
                    {
                        cells[i].ImageComponent.sprite = sprite;
                        cells[i].ImageComponent.color = Color.white;
                    }
                    else
                    {
                        cells[i].ImageComponent.color =
                            BTStyle.ErrorCell;
                    }
                    loadedIndices.Add(i);
                    loadedThisFrame++;

                    if (loadedThisFrame >= 4)
                    {
                        yield return null;
                        loadedThisFrame = 0;
                    }
                }

                if (loadedIndices.Count >= cells.Count)
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private static bool IsVisibleInViewport(
            RectTransform cell, RectTransform viewport)
        {
            float margin = 300f;

            Vector3[] viewCorners = new Vector3[4];
            viewport.GetWorldCorners(viewCorners);
            float viewMin = viewCorners[0].y - margin;
            float viewMax = viewCorners[2].y + margin;

            Vector3[] cellCorners = new Vector3[4];
            cell.GetWorldCorners(cellCorners);
            float cellMin = cellCorners[0].y;
            float cellMax = cellCorners[2].y;

            return cellMax >= viewMin && cellMin <= viewMax;
        }

        private static Sprite GetOrLoadThumbnail(PortraitEntry entry)
        {
            if (thumbnailCache.TryGetValue(
                    entry.FullPath, out Sprite cached))
            {
                return cached;
            }

            Texture2D tex = TextureLoadingHelper.LoadAndResizeTexture(entry.FullPath);
            if (tex == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));

            thumbnailCache[entry.FullPath] = sprite;
            return sprite;
        }

        private static void Close()
        {
            if (popupRoot != null)
            {
                if (activeScrollRect != null)
                {
                    savedScrollPosition =
                        activeScrollRect.verticalNormalizedPosition;
                }

                activeScrollRect = null;
                GameObject.Destroy(popupRoot);
                popupRoot = null;
            }
        }

        private static GameObject CreateUIElement(
            string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public class CoroutineRunner : MonoBehaviour { }

    public class PortraitEntry
    {
        public string Name;
        public string FullPath;
    }

    public class PortraitCell
    {
        public PortraitEntry Entry;
        public Image ImageComponent;
        public GameObject CellObject;
    }
}
