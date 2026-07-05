using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace RuneScapeForges
{
    // Content-driven codex: pages live on disk under
    // assets/runescape/config/codex/. Adding a page = drop a .vtml body next
    // to a new entry in index.json — zero code change.
    //
    // Loaded ONCE at first open (per session), then cached; a mid-session
    // author can force-reload with the /codex reload command (see mod init).
    public class CodexPage
    {
        public string Key;
        public string Label;
        public string Category;
        public string File;
        public string[] SeeAlso;
        public string Body;              // resolved at load time from File
    }

    class CodexIndex
    {
        public List<CodexPage> sections;
    }

    public static class CodexLibrary
    {
        static List<CodexPage> pages;
        static Dictionary<string, CodexPage> byKey;
        static List<string> categoryOrder;
        public const string BasePath = "config/codex/";

        public static List<CodexPage> Pages { get { EnsureLoaded(); return pages; } }
        public static List<string> CategoryOrder { get { EnsureLoaded(); return categoryOrder; } }

        public static void Invalidate()
        {
            pages = null; byKey = null; categoryOrder = null;
        }

        public static CodexPage Get(string key)
        {
            EnsureLoaded();
            CodexPage p;
            return byKey.TryGetValue(key, out p) ? p : null;
        }

        static ICoreAPI api;
        public static void Bind(ICoreAPI a) { api = a; }

        static void EnsureLoaded()
        {
            if (pages != null) return;
            pages = new List<CodexPage>();
            byKey = new Dictionary<string, CodexPage>();
            categoryOrder = new List<string>();

            if (api == null) return;

            IAsset idx = api.Assets.TryGet(new AssetLocation("runescape", BasePath + "index.json"));
            if (idx == null)
            {
                api.Logger.Error("[codex] index.json missing at assets/runescape/{0}index.json", BasePath);
                return;
            }

            CodexIndex parsed;
            try
            {
                parsed = JsonConvert.DeserializeObject<CodexIndex>(idx.ToText());
            }
            catch (System.Exception e)
            {
                api.Logger.Error("[codex] failed to parse index.json: {0}", e.Message);
                return;
            }
            if (parsed == null || parsed.sections == null) return;

            HashSet<string> seenCats = new HashSet<string>();
            for (int i = 0; i < parsed.sections.Count; i++)
            {
                CodexPage p = parsed.sections[i];
                if (p == null || string.IsNullOrEmpty(p.Key)) continue;
                if (string.IsNullOrEmpty(p.Category)) p.Category = "General";
                if (string.IsNullOrEmpty(p.Label)) p.Label = p.Key;

                p.Body = LoadBody(p);
                pages.Add(p);
                byKey[p.Key] = p;
                if (seenCats.Add(p.Category)) categoryOrder.Add(p.Category);
            }
        }

        static string LoadBody(CodexPage p)
        {
            if (string.IsNullOrEmpty(p.File)) return "<em>(no body file specified)</em>";
            IAsset a = api.Assets.TryGet(new AssetLocation("runescape", BasePath + p.File));
            if (a == null) return "<em>(missing body file: " + p.File + ")</em>";
            return a.ToText();
        }

        // Case-insensitive substring search across label, category, and body.
        // Returns keys of matching pages, in the original page order.
        public static List<string> Search(string query)
        {
            EnsureLoaded();
            List<string> hits = new List<string>();
            if (pages == null) return hits;
            if (string.IsNullOrWhiteSpace(query))
            {
                for (int i = 0; i < pages.Count; i++) hits.Add(pages[i].Key);
                return hits;
            }
            string q = query.ToLowerInvariant();
            for (int i = 0; i < pages.Count; i++)
            {
                CodexPage p = pages[i];
                if (p.Label != null && p.Label.ToLowerInvariant().Contains(q)) { hits.Add(p.Key); continue; }
                if (p.Category != null && p.Category.ToLowerInvariant().Contains(q)) { hits.Add(p.Key); continue; }
                if (p.Body != null && p.Body.ToLowerInvariant().Contains(q)) { hits.Add(p.Key); continue; }
            }
            return hits;
        }
    }
}
