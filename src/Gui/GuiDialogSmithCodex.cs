using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace RuneScapeForges
{
    // v2 — the Super Codex.
    //
    // Content-driven: pages live in assets/runescape/config/codex/ (see
    // CodexLibrary). This class is the reader — search, cross-links,
    // categories, bookmarks, back button. Adding a page = drop a .vtml,
    // update index.json, no code change.
    //
    // Links: body text uses <a href="key">label</a>. Click follows the
    // history stack; the ← button pops it.
    //
    // Bookmarks: capi.Settings.Strings[BookmarkKey] persists across sessions.
    public class GuiDialogSmithCodex : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "smithcodexdialog";

        const string BookmarkKey = "runescape.codex.bookmarks";
        const double NavWidth = 200;
        const double NavRowHeight = 26;
        const double NavRowGap = 2;
        const double BodyWidth = 500;
        const double BodyHeight = 500;
        const double Pad = 12;
        const double TopBarHeight = 32;
        const int VisibleNavRows = 18;

        string activeKey;
        string activeCategory;               // null = show all
        string searchQuery = "";
        int navScroll;                        // top row index
        readonly List<string> history = new List<string>();   // used as a stack; last element = top
        HashSet<string> bookmarks;

        List<string> visibleKeys = new List<string>();

        public GuiDialogSmithCodex(ICoreClientAPI capi) : base(capi)
        {
            bookmarks = LoadBookmarks();
            List<CodexPage> pages = CodexLibrary.Pages;
            activeKey = pages != null && pages.Count > 0 ? pages[0].Key : null;
            RecomputeVisible();
            ComposeDialog();
        }

        void RecomputeVisible()
        {
            visibleKeys.Clear();
            List<string> searchHits = CodexLibrary.Search(searchQuery);
            for (int i = 0; i < searchHits.Count; i++)
            {
                CodexPage p = CodexLibrary.Get(searchHits[i]);
                if (p == null) continue;
                if (activeCategory != null && p.Category != activeCategory) continue;
                visibleKeys.Add(p.Key);
            }
            if (navScroll >= visibleKeys.Count) navScroll = 0;
        }

        void ComposeDialog()
        {
            // ─ layout ─
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(Pad);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            // Top bar: search box + category filter + bookmark + back
            double y = 30; // below title bar
            ElementBounds searchBounds = ElementBounds.Fixed(0, y, NavWidth, TopBarHeight - 6);
            ElementBounds catRowBounds = ElementBounds.Fixed(0, y + TopBarHeight, NavWidth, 22);
            ElementBounds backBounds = ElementBounds.Fixed(NavWidth + Pad, y, 60, TopBarHeight - 6);
            ElementBounds bookmarkBounds = ElementBounds.Fixed(NavWidth + Pad + 68, y, 100, TopBarHeight - 6);
            ElementBounds titleBounds = ElementBounds.Fixed(NavWidth + Pad + 176, y, BodyWidth - 176, TopBarHeight);

            // Nav column: rows below the search+category bar
            double navTop = y + TopBarHeight + 26 + 6;
            double navRowsHeight = VisibleNavRows * (NavRowHeight + NavRowGap);
            ElementBounds navPanelBounds = ElementBounds.Fixed(0, navTop, NavWidth, navRowsHeight);

            // Body panel to the right of nav
            ElementBounds bodyBounds = ElementBounds.Fixed(NavWidth + Pad, navTop, BodyWidth, BodyHeight);

            GuiComposer composer = capi.Gui.CreateCompo("smithcodex-dialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Smith's Codex", () => TryClose())
                .BeginChildElements(bgBounds);

            // Search box
            composer.AddTextInput(searchBounds, OnSearchChanged, CairoFont.WhiteSmallText(), "search");

            // Category filter buttons — one per category + "All"
            List<string> cats = new List<string>();
            cats.Add("All");
            List<string> catOrder = CodexLibrary.CategoryOrder;
            for (int i = 0; i < catOrder.Count; i++) cats.Add(catOrder[i]);
            double cx = 0;
            double catBtnWidth = NavWidth / (cats.Count > 0 ? cats.Count : 1);
            if (catBtnWidth < 30) catBtnWidth = 30;
            for (int i = 0; i < cats.Count; i++)
            {
                string cat = cats[i];
                int capI = i;
                ElementBounds cb = ElementBounds.Fixed(cx, y + TopBarHeight, catBtnWidth - 2, 22);
                composer.AddSmallButton(cat, () => OnCategoryClick(capI == 0 ? null : cat), cb,
                    EnumButtonStyle.Small, "cat-" + i);
                cx += catBtnWidth;
            }

            // Back button
            composer.AddSmallButton("<", () => OnBackClick(), backBounds, EnumButtonStyle.Small, "back");

            // Bookmark toggle
            bool isBookmarked = activeKey != null && bookmarks.Contains(activeKey);
            composer.AddSmallButton(isBookmarked ? "★ Saved" : "☆ Save", () => OnBookmarkClick(),
                bookmarkBounds, EnumButtonStyle.Small, "bookmark");

            // Body title
            CodexPage current = activeKey == null ? null : CodexLibrary.Get(activeKey);
            string titleText = current == null ? "" : current.Label;
            composer.AddStaticText(titleText,
                CairoFont.WhiteDetailText().WithFontSize(20), titleBounds);

            // Nav rows — grouped by category with headers.
            composer.AddInset(navPanelBounds.FlatCopy().WithFixedPadding(2), 3, 0.6f);

            double rowY = 0;
            int shown = 0;
            string lastCat = null;
            // Star row: bookmarks appear at the top when no search
            if (string.IsNullOrEmpty(searchQuery) && activeCategory == null && bookmarks.Count > 0)
            {
                ElementBounds hb = ElementBounds.Fixed(4, navTop + rowY, NavWidth - 8, NavRowHeight - 4);
                composer.AddStaticText("★ Bookmarks", CairoFont.WhiteDetailText().WithFontSize(13), hb);
                rowY += NavRowHeight; shown++;
                foreach (string bk in bookmarks)
                {
                    CodexPage bp = CodexLibrary.Get(bk);
                    if (bp == null) continue;
                    ElementBounds rb = ElementBounds.Fixed(10, navTop + rowY, NavWidth - 14, NavRowHeight - 4);
                    string bcap = bk;
                    composer.AddSmallButton(bp.Label, () => Navigate(bcap), rb,
                        bk == activeKey ? EnumButtonStyle.Normal : EnumButtonStyle.Small, "bk-" + bk);
                    rowY += NavRowHeight; shown++;
                    if (shown >= VisibleNavRows) break;
                }
                if (shown < VisibleNavRows) { rowY += 6; shown++; }
            }

            for (int i = 0; i < visibleKeys.Count && shown < VisibleNavRows; i++)
            {
                CodexPage p = CodexLibrary.Get(visibleKeys[i]);
                if (p == null) continue;
                if (p.Category != lastCat)
                {
                    ElementBounds hb = ElementBounds.Fixed(4, navTop + rowY, NavWidth - 8, NavRowHeight - 4);
                    composer.AddStaticText(p.Category, CairoFont.WhiteDetailText().WithFontSize(13), hb);
                    lastCat = p.Category;
                    rowY += NavRowHeight; shown++;
                    if (shown >= VisibleNavRows) break;
                }
                ElementBounds b = ElementBounds.Fixed(10, navTop + rowY, NavWidth - 14, NavRowHeight - 4);
                string cap = p.Key;
                composer.AddSmallButton(p.Label, () => Navigate(cap), b,
                    p.Key == activeKey ? EnumButtonStyle.Normal : EnumButtonStyle.Small, "nav-" + p.Key);
                rowY += NavRowHeight; shown++;
            }

            // Body — richtext with link callback + "See also" appended if any
            string body = current == null ? "<em>(empty)</em>" : current.Body;
            if (current != null && current.SeeAlso != null && current.SeeAlso.Length > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(body);
                sb.Append("\n\n<strong>See also</strong>\n");
                for (int i = 0; i < current.SeeAlso.Length; i++)
                {
                    CodexPage sp = CodexLibrary.Get(current.SeeAlso[i]);
                    if (sp == null) continue;
                    if (i > 0) sb.Append(" · ");
                    sb.Append("<a href=\"").Append(sp.Key).Append("\">").Append(sp.Label).Append("</a>");
                }
                body = sb.ToString();
            }
            composer.AddRichtext(body,
                CairoFont.WhiteDetailText().WithLineHeightMultiplier(1.15),
                bodyBounds, OnLinkClicked, "body");

            composer.EndChildElements().Compose();
            SingleComposer = composer;

            // Prefill search field on rebuild.
            if (!string.IsNullOrEmpty(searchQuery))
            {
                SingleComposer.GetTextInput("search").SetValue(searchQuery, false);
            }
        }

        void OnSearchChanged(string q)
        {
            searchQuery = q ?? "";
            RecomputeVisible();
            // Auto-focus first hit if the current page falls off the visible list.
            if (activeKey != null && !visibleKeys.Contains(activeKey) && visibleKeys.Count > 0)
            {
                Navigate(visibleKeys[0]);
                return;
            }
            ComposeDialog();
        }

        bool OnCategoryClick(string cat)
        {
            activeCategory = cat;
            RecomputeVisible();
            if (activeKey != null && !visibleKeys.Contains(activeKey) && visibleKeys.Count > 0)
                activeKey = visibleKeys[0];
            ComposeDialog();
            return true;
        }

        bool OnBackClick()
        {
            if (history.Count == 0) return true;
            int last = history.Count - 1;
            activeKey = history[last];
            history.RemoveAt(last);
            ComposeDialog();
            return true;
        }

        bool OnBookmarkClick()
        {
            if (activeKey == null) return true;
            if (bookmarks.Contains(activeKey)) bookmarks.Remove(activeKey);
            else bookmarks.Add(activeKey);
            SaveBookmarks();
            ComposeDialog();
            return true;
        }

        void OnLinkClicked(LinkTextComponent link)
        {
            if (link == null || link.Href == null) return;
            string href = link.Href.ToString();
            // Strip any scheme prefix VS adds (rare — richtext hrefs come through raw).
            if (href.StartsWith("codex:")) href = href.Substring(6);
            CodexPage p = CodexLibrary.Get(href);
            if (p == null)
            {
                capi.ShowChatMessage("Codex: no page named '" + href + "'");
                return;
            }
            Navigate(href);
        }

        // Returns true so a raw `() => Navigate(key)` satisfies the
        // ActionConsumable delegate that AddSmallButton expects.
        bool Navigate(string key)
        {
            if (key == null || key == activeKey) return true;
            if (activeKey != null) history.Add(activeKey);
            if (history.Count > 32) history.RemoveAt(0);   // trim oldest entry
            activeKey = key;
            RecomputeVisible();
            ComposeDialog();
            return true;
        }

        // ─── bookmarks persistence ────────────────────────────────────────
        // Stored as a single pipe-delimited string in client settings. The
        // ISettingsClass<List<string>> path leaks a System.Collections
        // dependency the in-game compiler can't reference (CS0012); a plain
        // string setting stays classpath-safe and round-trips fine at this size.

        HashSet<string> LoadBookmarks()
        {
            HashSet<string> set = new HashSet<string>();
            string stored = null;
            try { stored = capi.Settings.String[BookmarkKey]; }
            catch (System.Exception) { stored = null; }
            if (string.IsNullOrEmpty(stored)) return set;
            string[] parts = stored.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i])) set.Add(parts[i]);
            }
            return set;
        }

        void SaveBookmarks()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (string k in bookmarks)
            {
                if (!first) sb.Append('|');
                sb.Append(k);
                first = false;
            }
            try { capi.Settings.String[BookmarkKey] = sb.ToString(); }
            catch (System.Exception) { /* settings unavailable — session-only */ }
        }
    }
}
