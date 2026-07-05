using System.Collections.Generic;

namespace RuneScapeForges
{
    public struct SpeciesDrop
    {
        public string Code;
        public int Qty;
        public SpeciesDrop(string code, int qty) { Code = code; Qty = qty; }
    }

    public class HunterSpecies
    {
        public string Id;
        public string DisplayName;
        public int HunterLevelReq;
        public int XpReward;
        public string FeatherTone;     // red / yellow / orange / blue / stripy — for future custom-feather drops
        public SpeciesDrop[] Drops;
        public ClimateFilter Climate;

        public bool MatchesBiome(float tempC, float rain)
        {
            return Climate.Matches(tempC, rain);
        }
    }

    public struct ClimateFilter
    {
        public float MinTemp;
        public float MaxTemp;
        public float MinRain;
        public float MaxRain;

        public bool Matches(float tempC, float rain) =>
            tempC >= MinTemp && tempC <= MaxTemp &&
            rain  >= MinRain && rain  <= MaxRain;

        public static ClimateFilter Any => new ClimateFilter { MinTemp = -50, MaxTemp = 50, MinRain = 0, MaxRain = 1 };
        public static ClimateFilter Cold => new ClimateFilter { MinTemp = -50, MaxTemp = 5, MinRain = 0, MaxRain = 1 };
        public static ClimateFilter TemperateWet => new ClimateFilter { MinTemp = 5, MaxTemp = 22, MinRain = 0.4f, MaxRain = 1 };
        public static ClimateFilter TemperateDry => new ClimateFilter { MinTemp = 5, MaxTemp = 22, MinRain = 0, MaxRain = 0.4f };
        public static ClimateFilter Jungle => new ClimateFilter { MinTemp = 22, MaxTemp = 50, MinRain = 0.45f, MaxRain = 1 };
        public static ClimateFilter Desert => new ClimateFilter { MinTemp = 22, MaxTemp = 50, MinRain = 0, MaxRain = 0.25f };
    }

    // Bird-snare species table. Sourced from docs/RESEARCH_osrs_hunter.md §5.
    // Phase 1 uses vanilla VS chickens as the in-world entity stand-in; the snare
    // resolves the actual species + drop table from biome + a small jitter roll.
    public static class HunterSpeciesTable
    {
        public static readonly HunterSpecies[] BirdSnareSpecies = new[]
        {
            new HunterSpecies {
                Id = "crimsonswift",   DisplayName = "Crimson Swift",   HunterLevelReq = 1,
                XpReward = 34, FeatherTone = "red",   Climate = ClimateFilter.Jungle,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:feather", 2), new SpeciesDrop("game:poultry-raw", 1) },
            },
            new HunterSpecies {
                Id = "polarkebbit_proxy", DisplayName = "Polar Kebbit",  HunterLevelReq = 1,
                XpReward = 30, FeatherTone = "white", Climate = ClimateFilter.Cold,
                // Kebbit isn't a bird, but for phase-1 cold biome we use snare → fur-bearing small mammal.
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:feather", 1), new SpeciesDrop("game:poultry-raw", 1) },
            },
            new HunterSpecies {
                Id = "goldenwarbler", DisplayName = "Golden Warbler",   HunterLevelReq = 5,
                XpReward = 47, FeatherTone = "yellow", Climate = ClimateFilter.Desert,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:feather", 3), new SpeciesDrop("game:poultry-raw", 1) },
            },
            new HunterSpecies {
                Id = "copperlongtail", DisplayName = "Copper Longtail", HunterLevelReq = 9,
                XpReward = 61, FeatherTone = "orange", Climate = ClimateFilter.TemperateDry,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:feather", 3), new SpeciesDrop("game:poultry-raw", 1) },
            },
            new HunterSpecies {
                Id = "ceruleantwitch", DisplayName = "Cerulean Twitch", HunterLevelReq = 11,
                XpReward = 64, FeatherTone = "blue", Climate = ClimateFilter.Cold,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:feather", 3), new SpeciesDrop("game:poultry-raw", 1) },
            },
            new HunterSpecies {
                Id = "tropicalwagtail", DisplayName = "Tropical Wagtail", HunterLevelReq = 19,
                XpReward = 95, FeatherTone = "stripy", Climate = ClimateFilter.TemperateWet,
                Drops = new[] { new SpeciesDrop("game:bone", 1), new SpeciesDrop("game:feather", 4), new SpeciesDrop("game:poultry-raw", 1) },
            },
        };

        // Pick the highest-XP species whose biome filter accepts this climate AND
        // whose level req <= player Hunter level. Fallback: crimson swift.
        public static HunterSpecies Resolve(float tempC, float rain, int playerHunterLevel)
        {
            HunterSpecies best = null;
            foreach (var s in BirdSnareSpecies)
            {
                if (s.HunterLevelReq > playerHunterLevel) continue;
                if (!s.MatchesBiome(tempC, rain)) continue;
                if (best == null || s.XpReward > best.XpReward) best = s;
            }
            return best ?? BirdSnareSpecies[0];
        }
    }
}
