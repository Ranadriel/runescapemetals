# OSRS Hunter — Design-Grounding Reference

*Compiled 2026-06-21 via OSRS wiki fan-out research. For runescapemetals / runestory family design.*

## 1. Core mechanic taxonomy

| Family | Trap object / gear | Player action |
|---|---|---|
| **Tracking** | Noose wand | Inspect scenery (burrows, broken twigs); follow chained tracks across tiles; attack with noose wand. |
| **Bird snare** | Single-tile spring snare on grass | Lay, walk off; bird approaches, snare snaps regardless of success; reset on fail. |
| **Box trap** | Baited wooden box | Lay; chinchompa/ferret within 2-tile radius (5×5 square) attracted; passive. **Requires partial Eagles' Peak.** |
| **Net trap** | Rope + small fishing net on sapling | Use rope+net on young tree; lizard/salamander walks past; tree snaps back on fail. |
| **Deadfall** | Boulder + propping log at fixed scenery site | Knife + log on boulder; passive — prey walks under, boulder drops. |
| **Pitfall (active lure)** | Spiked pit + covering logs at fixed scenery | Knife + log on pit; equip Hunter's spear or hold teasing stick; click creature to tease; jump pit; creature follows. |
| **Magic box** | Magical box on tile (optional bead bait) | Drop; imp wanders in → Imp-in-a-box. Cannot be smoked. |
| **Rabbit snare** | Snare at burrow + ferret consumable | Place snare at hole, use ferret on hole; ferret flushes rabbit into snare. |
| **Butterfly net** | Held net + jar | Click flying creature; auto-walk and swing. Without jar: butterflies released, implings auto-looted. |
| **Falconry** | Rented gyr falcon + falconer's glove (zone-locked) | Click kebbit; falcon launches; walk to falcon to retrieve fur + bird. Lose-on-timeout. |
| **Aerial fishing** | Cormorant's glove + bait | At Lake Molch, click fishing spot; cormorant always returns with a fish (species rolled). |
| **Drift net** | Drift net strung between two underwater anchors | Use net on anchor; chase shoals into net; each shoal +10%; net destroyed on harvest. |
| **Birdhouse trap** | Crafted birdhouse + 10 hop seeds, on Fossil Island sites | Place, return ~50 min later; collect feathers/seeds/nests. AFK passive. |
| **Crab trap** | Permanent fixed crab-trap site | Click site; cannot fail. |
| **Marasamaw plant** | RS3-only. No live OSRS wiki page. Treat as invented if used. |

## 2. Trap success math

No closed-form formula on the wiki for standard traps — only modifier values.

| Variable | Effect |
|---|---|
| Player level vs creature required level | Below req → always fails. Above req → rate scales with gap (Net trap page graph: ~55% at min req → 99%+ at L99). |
| Creature-specific bait | **+3%** catch rate |
| Smoking trap with lit/bruma torch | **+2%** catch rate |
| Hunter's spear equipped (pitfall only) | **+5%** tease success (not catch roll) |
| Guild Hunter outfit (full) | **+2.5%** catch rate (Hunter creatures) |
| Magic butterfly net (vs regular) | **+~7.8%** capture chance on butterflies/implings/bats |
| Magic box bead bait | **+3%** (wiki: "generally not worth baiting") |
| Magic box smoking | **N/A — cannot be smoked** |

**Variables that do NOT affect catch rate (explicit wiki statement):**
- **Camouflage clothing (larupia/graahk/kyatt)** — *"Wearing camouflage gear does not improve the chance of successfully capturing creatures."* Their only effect is **damage reduction** from creatures attacking out of pitfalls (~20% per piece up to 60% full kyatt). OSRS-specific quirk; RS3 reversed it to catch-rate. Design call for the mod.
- Time of day / weather — not mentioned anywhere as catch-rate factors.

**Always-success cases (no roll):** Crab trapping, aerial fishing (species varies; catch never fails).

**The only published catch formula — Aerial fishing:**
Roll `X = (Fishing × 2 + Hunter) / 3`, then bucket:

| X | Fish | Req F | Req H |
|---|---|---|---|
| 82+ | Greater siren | 91 | 87 |
| 67–81 | Mottled eel | 73 | 68 |
| 52–66 | Common tench | 56 | 51 |
| 0–51 | Bluegill | 43 | 35 |

Molch pearl rate: `1 / (100 − ((X − 40) × 25 / 59))` ≈ 1/100 → 1/75.

## 3. Multi-trap limits by level

Universal across box, net, bird snare, rabbit snare, magic box, pitfall.

| Hunter level | Simultaneous traps |
|---|---|
| 1–19 | 1 |
| 20–39 | 2 |
| 40–59 | 3 |
| 60–79 | 4 |
| 80–99 | 5 |

**Exceptions:**
- **Deadfall**: hard cap **1**, regardless of level.
- **Wilderness +1**: black salamanders and black chinchompas in Wildy = +1 extra trap (L80 → 6 in Wildy).
- Falconry / tracking / butterfly net / aerial fishing / crab / drift net: no per-tile cap.

## 4. Equipment

### 4a. Camo clothing sets

| Set | Slots | Hunter req | Magnitude | Biome / target | Source |
|---|---|---|---|---|---|
| Larupia | hat, top, legs | 28 | -20% dmg from Hunter creatures (full) | Jungle / Spined larupia | Fancy Dress (Varrock), Pellem (Guild). 100–500 gp + furs |
| Graahk | headdress, top, legs | 38 | -40% dmg (full) | Tropical / Horned graahk | Fancy Clothes, Pellem, Prifddinas Seamstress. 150–750 gp + furs |
| Kyatt | hat, top, legs | 52 | -20%/piece, -60% full | Snow / Sabre-toothed kyatt | Fancy Clothes, Pellem. 200–1000 gp + furs |
| Spotted cape | cape | 40 | -2.267 kg weight | Falconry tier | Fancy Clothes. 400 gp + 2 spotted furs |
| Spottier cape | cape | 66 (unboostable) | -4.535 kg weight | Endgame falconry | Fancy Clothes. 800 gp + 2 dashing furs |
| Gloves of silence | hands | 54 | +5% Thieving (NOT Hunter) | Cross-skill, decays in 62 fails | NPC tailor, 2 dark kebbit furs + 600 gp |
| Guild Hunter outfit | full set | — | **+2.5% catch rate** (only OSRS Hunter-catch outfit) | Hunter Guild | Hunter Guild shop |

**Load-bearing design note:** Outside the Guild Hunter outfit, **no OSRS camo clothing buffs catch rate.** The big three fur sets only reduce damage from the prey itself.

### 4b. Tools

| Tool | Slot | Min Hunter req | Buy / source |
|---|---|---|---|
| Rope | inv | — | Ned (Draynor) 18 gp; or 30 Crafting from yak hair |
| Teasing stick | inv | 31 (gated by larupia) | Aleck's/Nardah 60 gp; Imia (Guild) 65 gp |
| Hunter's spear | weapon | — (Varlamore) | Doubles as teasing stick + pitfall +5% tease |
| Noose wand | weapon | gated by target | Nardah/Aleck's 4 gp; GE 13 gp |
| Box trap | inv | 27 (Eagles' Peak) | Aleck's/Nardah/Guild 38–46 gp |
| Bird snare | inv | 1 | Aleck's/Nardah; GE 29 gp (stackable since Aug 2025) |
| Butterfly net | weapon | — | shops |
| Butterfly jar | inv | — | shops 1 gp |
| Magic box | inv | 71 | shops 720 gp; GE 430 gp; bead bait +3% |
| Falconer's glove + gyr falcon | hands (rented) | 43 | Matthias @ Piscatoris falconry zone, 500 gp/session OR 500,000 gp lifetime |
| Cormorant's glove | hands | 35 | Aerial fishing zone, Lake Molch |
| Drift net | inv | 44 H + 47 F | Fossil Island underwater shop |
| Hunter kit (8-pack) | inv | — | Lunar spell (71 Magic, Dream Mentor) or NPC freebie. Untradeable. |

### 4c. Hunter potions

| Potion | Boost | Notes |
|---|---|---|
| Hunter potion (1–4) | **+3** visible | 53 Herblore, 120 xp/dose. Clean avantoe + kebbit teeth dust. |
| Hunting mix (Barbarian) | +3 + heals 6 HP | 58 Herblore. Hunter pot(2) + caviar. |
| Super hunter potion | **+6** visible | Recipe not confirmed this pass; verify before port. |
| Spicy stew (yellow spice) | ±0 to ±5 random per dose | From Evil Dave's Hell-Rat Behemoths. Heals 11 HP. |
| Horn of plenty (empty / charged) | +2 / +4 invisible | Stat-panel-silent but mechanics apply. |
| Hunter cape special | +1 | Skill cape activated. |

**Do not exist in OSRS** (kill from any porting assumption):
- "Hunter's sense" prayer — not in OSRS.
- Summer pie Hunter boost — pie is +5 Agility, not Hunter.

## 5. Creature ladder

Sorted by Hunter level. "Post-launch" column = added after 2013 OSRS launch.

| L | Creature | Method | Primary drop(s) | XP | Location | Post-launch |
|---|---|---|---|---|---|---|
| 1 | Crimson swift | Bird snare | Bones, raw bird meat, red feathers | 34 | Feldip; Kebos; Isle of Souls; Tlati | — |
| 1 | Polar kebbit | Deadfall (track) | Polar fur, raw beast meat | 30 | Rellekka | — |
| 3 | Common kebbit | Deadfall (track) | Common fur, beast meat | 36 | Piscatoris | — |
| 5 | Golden warbler | Bird snare | Yellow feathers | 47 | Uzer | — |
| 5 | Birdhouse (regular) | Birdhouse | Feathers, nests, seeds | 280/4 houses | Fossil Island | **2017-09-07** |
| 7 | Feldip weasel | Deadfall (track) | Feldip fur | 48 | Feldip | — |
| 9 | Copper longtail | Bird snare | Orange feathers | 61.2 | Piscatoris/Kourend/Aldarin | — |
| 11 | Cerulean twitch | Bird snare | Blue feathers | 64.5 | Rellekka | — |
| 13 | Desert devil | Noose (track) | Devil fur | 66 | Uzer | — |
| 14 | Birdhouse (oak) | Birdhouse | — | 420/4 | Fossil Is. | **2017-09-07** |
| 15 | Ruby harvest | Butterfly net | Ruby harvest jar | 24 | Piscatoris/Kourend/Farming Guild | — |
| 17 | Baby impling | Net + Magic box | Chisel, thread, anchovies | 18/20 | Puro-Puro + overworld | — |
| 19 | Tropical wagtail | Bird snare | Stripy feathers | 95.2 | Feldip; Tlati | — |
| 21 | Red crab | Crab trap | (Hunter Guild content) | ~109 | Pandemonium (Guild) | **2024-03-20** |
| 22 | Young impling | Net + Magic box | Steel nails, pure essence, bow string | 20/22 | Puro-Puro + overworld | — |
| 23 | Wild kebbit | Deadfall (track) | Kebbit claws | 128 | Piscatoris; Auburnvale | — |
| 24 | Birdhouse (willow) | Birdhouse | — | 560/4 | Fossil Is. | **2017-09-07** |
| 25 | Sapphire glacialis | Butterfly net | Sapphire glacialis jar | 34 | Rellekka; Farming Guild | — |
| 27 | Ferret | Box trap | Ferret | 115.2 | Piscatoris (Eagles' Peak) | — |
| 27 | White rabbit | Rabbit snare + ferret | Rabbit foot, raw rabbit | 144 | Piscatoris | — |
| 28 | Gourmet impling | Net + Magic box | Tuna, curry, easy clue | 22/24 | Puro-Puro + overworld | — |
| 29 | Swamp lizard | Net trap | Swamp lizard (weapon) | 152 | E of Canifis | — |
| 31 | Spined larupia | Pitfall | Larupia fur, big bones | 180 | Feldip | — |
| 33 | Barb-tailed kebbit | Deadfall | Barb-tail harpoon, fur | 168 | Feldip | — |
| 34 | Birdhouse (teak) | Birdhouse | — | 700/4 | Fossil Is. | **2017-09-07** |
| 35 | Snowy knight | Butterfly net | Snowy knight jar | 44 | Rellekka; Weiss; Farming Guild | — |
| 35 | Bluegill | Aerial fishing (43 F) | Bluegill | 16.5 H + Fishing XP | Lake Molch | **2019-01-10** |
| 36 | Earth impling | Net + Magic box | Earth talisman, earth runes | 25/27 | Puro-Puro + overworld | — |
| 37 | Prickly kebbit | Deadfall | Kebbit spike | 204 | Piscatoris | — |
| 39 | Embertailed jerboa | Box trap | Jerboa tail | 137 | Avium Savannah | **2024-03-20** |
| 41 | Horned graahk | Pitfall | Graahk fur, big bones | 240 | Karamja | — |
| 42 | Essence impling | Net + Magic box | Law/blood/nature runes | 27/29 | Puro-Puro + overworld | — |
| 43 | Spotted kebbit | Falconry | Spotted fur (113 gp) | 104 | Piscatoris Falconry zone | — |
| 44 | Birdhouse (maple) | Birdhouse | — | 820/4 | Fossil Is. | **2017-09-07** |
| 44 / 47F | Drift net shoals | Drift net | Anchovies → manta ray (Fishing-scaled) | scales | Fossil Is. underwater | **2018-03-14** |
| 45 | Black warlock | Butterfly net | Black warlock jar | 54 | Feldip; Crypt of Tonali | — |
| 47 | Orange salamander | Net trap | Orange salamander (weapon) | 224 | Uzer; Necropolis | — |
| 49 | Razor-backed kebbit | Deadfall (track) | Long kebbit spike | 348.5 | Piscatoris | — |
| 49 | Birdhouse (mahogany) | Birdhouse | — | 960/4 | Fossil Is. | **2017-09-07** |
| 50 | Eclectic impling | Net + Magic box | Med clue, mith pickaxe, snape grass | 30/32 | Puro-Puro + overworld | — |
| 51 | Sabre-toothed kebbit | Deadfall | Kebbit teeth | 200 | Rellekka | — |
| 51 | Common tench | Aerial fishing (56 F) | Common tench | 45 H + F | Lake Molch | **2019-01-10** |
| 53 | Chinchompa (grey) | Box trap | Grey chinchompa | 198.4 | Piscatoris; Kourend | — |
| 55 | Sabre-toothed kyatt | Pitfall | Kyatt fur, big bones | 300 | Rellekka | — |
| 57 | Dark kebbit | Falconry | Dark fur (68 gp) | 132 | Piscatoris Falconry zone | — |
| 57 | Pyre fox | Deadfall | Fox fur, raw pyre fox | 222 | Avium Savannah | **2024-03-20** |
| 58 | Nature impling | Net + Magic box | Magic logs, ranarr/torstol seed | 34/36 | Puro-Puro + overworld | — |
| 59 | Red salamander | Net trap | Red salamander (weapon) | ~272 | Ourania | — |
| 59 | Birdhouse (yew) | Birdhouse | — | 1020/4 | Fossil Is. | **2017-09-07** |
| 60 | Maniacal monkey | Deadfall (banana) | Monkey tail | 1000 | Kruk's Dungeon (needs MM2) | **2016-05-26** |
| 63 | Carnivorous chinchompa | Box trap | Red chinchompa | 265 | Feldip; Gwenith; Tlati | — |
| 65 | Magpie impling | Net + Magic box | Hard clue, noted rune bars, dragon dagger | 44/216 | Puro-Puro + overworld | — |
| 65 | Sunlight moth | Butterfly net | Sunlight moth (jar) | — | Avium | **2024-03-20** |
| 67 | Black salamander | Net trap | Black salamander (weapon) | ~319 | NE of Chaos Temple (Wildy) | — |
| 68 | Mottled eel | Aerial fishing (73 F) | Mottled eel | 90 H + F | Lake Molch | **2019-01-10** |
| 69 | Dashing kebbit | Falconry | Dashing fur (155 gp) + raw dashing kebbit (2031 gp) | 156 | Piscatoris Falconry zone | — |
| 71 | Imp | Magic box | Fiendish ashes, beads | 450 | E of Ardougne Monastery; Kourend | — |
| 72 | Sunlight antelope | Pitfall | Antler, big bones, fur | 380 | Avium Savannah | **2024-03-20** |
| 73 | Black chinchompa | Box trap (Wildy +1) | Black chinchompa | 315 | Wilderness | **2014-03-27** |
| 74 | Birdhouse (magic) | Birdhouse | — | 1140/4 | Fossil Is. | **2017-09-07** |
| 74 | Ninja impling | Net + Magic box | Rune chainbody, dragon dagger(p+), onyx bolts | 50/240 | Puro-Puro + overworld | — |
| 75 | Moonlight moth | Butterfly net | Moonlight moth (jar) | — | Avium/Neypotzli/Guild | **2024-03-20** |
| 77 | Rainbow crab | Crab trap | High gp/hr | — | Crown Jewel Is. (Guild) | **2024-03-20** |
| 79 | Tecu salamander | Net trap | Tecu salamander | 344 | Avium (SE of Ralos' Rise) | **2024-03-20** |
| 80 | Herbiboar | Tracking (kick burrow) | Grimy herbs (marrentill → torstol; Herblore-scaled) | ~1950 (→2461) | Mushroom Meadow, Fossil Is. | **2017-09-07** |
| 80 (90 bare) | Crystal impling | Net + Magic box | Crystal shards, acorn, dragonstones | 280 | Prifddinas | **2019-07-25** |
| 83 (93 bare) | Dragon impling | Net + Magic box | Dragon arrows/darts/longsword, noted dragon bones | 65/300 | Puro-Puro + overworld | — |
| 87 | Greater siren | Aerial fishing (91 F) | Greater siren | 130 H + F | Lake Molch | **2019-01-10** |
| 89 | Birdhouse (redwood) | Birdhouse | — | 1200/4 | Fossil Is. | **2017-09-07** |
| 89 (99 bare) | Lucky impling | Net + Magic box | One clue-table roll (any tier) | 80/380 | Puro-Puro + overworld | **2016-07-06** |
| 91 | Moonlight antelope | Pitfall | Antler, big bones, fur | 450 | Hunter Guild underground | **2024-03-20** |

**Verify before port (common assumption errors):**
- **Razor-backed kebbit = L49, not L77.**
- **Sabre-toothed kebbit (L51, deadfall) ≠ Sabre-toothed kyatt (L55, pitfall).** Two distinct creatures.
- **Cerulean twitch = L11, not L29.**
- **Spined larupia = L31, not L43.**
- **Pawya = RS3 only. Not in OSRS.**
- **Pirate / Zombie impling = RS3 only. Not in OSRS.** Canonical OSRS impling roster is 12: Baby, Young, Gourmet, Earth, Essence, Eclectic, Nature, Magpie, Ninja, Crystal, Dragon, Lucky.
- **Marasamaw plant = no live OSRS wiki page.** RS3 mechanic. Invent or skip.

## 6. XP & progression

### Best XP/hr by bracket

- **1–20** — Birdhouses (5+): 50k → 216k/hr lifetime. Ruby Harvests (15): ~20k/hr.
- **20–40** — Red Crabs (21): 31k/hr. Sapphire Glacialis (25): 28.5k/hr. Embertailed Jerboas (39): 50k/hr. Aerial fishing (35): 26k H / 18k F.
- **40–60** — Drift Net (44): 113k combined H+F, AFK, fish banked. Falconry Spotted (43): 60–95k/hr. Razor-backed kebbit (49): 100–140k/hr + spike/bone profit. Orange salamander (47): 40–100k/hr. Rumours Novice (46): 160k/hr.
- **60–80** — Carnivorous chinchompas (63): 85–220k/hr, 0.43–1.1M gp/hr. Maniacal monkeys (60): 51–110k/hr. Black chinchompas (73): 145–225k/hr, 1.3–2.4M gp/hr (Wildy risk). Rumours Expert (72): 200k+/hr.
- **80–99** — Black chins: 145–265k/hr. Rumours Master (91): 250k/hr. Herbiboar (80): 137–171k/hr + herb profit, AFK. Rainbow crabs (77): 155k/hr + 690k gp/hr. Moonlight moths (75): 100k+/hr. Aerial Greater siren (87): up to 82k H / 62k F @99.

### Profit-focused alternatives
- **Herbiboar** (80): AFK, herb stack profit, scales with Herblore.
- **Black chinchompas** (73): top-tier XP + gp, Wildy risk.
- **Carnivorous chinchompas** (63): safe, near-top gp/hr.
- **Razor-backed kebbit** (49): spikes + bones, profitable at sub-50.
- **Drift net** (44 H / 47 F): banked fish, dual-skill XP.
- **Dashing kebbit raw meat** (69, falconry): 2031 gp/each side-income.

## 7. Locations → creatures

| Area | Biome | Creatures (lvl) |
|---|---|---|
| Piscatoris Hunter area | Woodland | Common kebbit 3, Copper longtail 9, Ruby harvest 15, Wild kebbit 23, Ferret/White rabbit 27, Prickly kebbit 37, Razor-backed kebbit 49, Sabre-tooth kebbit 51, Chinchompa 53 |
| Piscatoris Falconry zone | Woodland | Spotted 43, Dark 57, Dashing 69 |
| Feldip Hills | Jungle | Crimson swift 1, Feldip weasel 7, Tropical wagtail 19, Spined larupia 31, Barb-tail 33, Black warlock 45, Carnivorous chin 63 |
| Karamja Hunter area | Jungle | Horned graahk 41 |
| Uzer Hunter area | Desert | Golden warbler 5, Desert devil 13, Orange salamander 47 |
| Necropolis Hunter area | Desert | Orange salamander 47 |
| Rellekka Hunter area | Snow | Polar kebbit 1, Cerulean twitch 11, Sapphire glacialis 25, Snowy knight 35, Sabre-tooth kebbit 51, Sabre-tooth kyatt 55 |
| Canifis / Slepe | Swamp | Swamp lizard 29 |
| Ourania | Lava | Red salamander 59 |
| Boneyard (Wilderness) | Lava | Black salamander 67 |
| Wilderness | — | Black chinchompa 73 |
| Anywhere | — | Imp 71 |
| Kruk's Dungeon (Ape Atoll) | — | Maniacal monkey 60 (MM2 + banana bait) |
| Fossil Island | — | Birdhouses 5+, Drift net 44, Herbiboar 80 (Mushroom Meadow) |
| Lake Molch (Kebos) | — | Aerial fishing 35/51/68/87 |
| Kourend Woodland | — | Copper longtail 9, Ruby harvest 15, Chinchompa 53 |
| Gwenith (Prifddinas) | — | Carnivorous chinchompa 63, Crystal impling 80 |
| **Hunter Guild — Avium Savannah (2024)** | Savannah | Embertailed jerboa 39, Pyre fox 57, Sunlight moth 65, Sunlight antelope 72, Moonlight moth 75, Tecu salamander 79, Moonlight antelope 91 |
| Tlati Rainforest (Varlamore) | Rainforest | Carnivorous chinchompa 63/80 |
| Hunter Guild (Pandemonium / Crown Jewel Is.) | — | Red crab 21, Rainbow crab 77, Rumours 46/72/91 |

## 8. Falconry deep-dive

**Mechanic:** Rented gyr falcon + falconer's glove (hand slot, blocks weapon + shield). Click kebbit → falcon launched → on hit, walk to falcon and right-click to retrieve fur. **Catch forfeit on timeout** — wiki message: *"Your falcon has left its prey."* On miss, falcon returns "much faster" than on hit. Wiki does not quantify throw range, fatigue ticks, or retrieval window — empirical/feel choices for the mod.

**Kebbit varieties:**

| Kebbit | Lvl | XP | Fur (alch / GE) | Bonus drop | Success @ L1 → L99 |
|---|---|---|---|---|---|
| Spotted | 43 | 104 | 113 gp | Kebbity tuft 1/10 Rumour | 10% → 121% |
| Dark | 57 | 132 | 68 gp (alch 126) | Kebbity tuft | 0% → 99% |
| Dashing | 69 | 156 | 155 gp + **raw dashing kebbit 2031 gp** | Kebbity tuft | 0% @L1 → 80% @L99; ~55% at L69 |

**Falcon rental:** 500 gp/session or 500,000 gp lifetime. Falcon and glove are **zone-locked** to Piscatoris falconry area — auto-disappears on teleport out; glove cannot be removed by player (must talk to Matthias); teleport bypasses glove restriction (engine quirk, ignore for mod).

**Competitive analysis:** Falconry is **never the explicit XP optimum** in any band. Spotted (43–57) is solid before salamanders unlock; salamanders out-scale dark/dashing from L60 onward. Wiki: *"Since dashing kebbits do not become available until level 69, training with them is not popular since salamanders offer better experience rates from level 60 onwards."* Real role:
- Early-band gap filler (43–57)
- Fur revenue
- Cape unlocks + Rumour collection

**Quest gating:** *Falconry has NO quest requirement.* Eagles' Peak gates **box traps + eagle transport + ferret only**, not falconry. Falconry is pure L43 + geography.

## 9. Quests & lock-walls

| Quest | Hunter req | Unlocks | Hunter XP reward |
|---|---|---|---|
| **Eagles' Peak** | 27 (boostable) | Box traps + eagle transport + ferret | 2500 + box trap |
| Cold War | 10 | — | — |
| Ascent of Arceuus | 12 | — | 1500 |
| Perilous Moons | 20 | — | 5000 |
| Shadows of Custodia | 36 | — | 4000 |
| Troubled Tortugans | 45 | — | — |
| At First Light | 46 | — | 4500 |
| Defender of Varrock | 52 | — | 15000 |
| Secrets of the North | 56 | — | 40000 |
| **Monkey Madness II** | 60 | Maniacal monkeys | 50000 |
| While Guthix Sleeps | 62 | — | 50000 |
| **Song of the Elves** | 70 | Crystal impling (Prifddinas) | 40000 |
| Natural History Quiz (miniquest) | — | — | 1000 (14 displays, 28 kudos) |

Only Eagles' Peak, MM2, and Song of the Elves materially gate content. Everything else is XP-reward only.

## 10. Post-launch additions (date-stamped era gates)

```
2014-03-27  Black chinchompa                          Wilderness Feedback        L73
2016-05-26  Maniacal monkey                           Monkey Madness II          L60
2016-07-06  Lucky impling                             Treasure Trail Expansion   L89/99
2017-09-07  Birdhouse trapping (9 tiers)              Fossil Island              L5–89
2017-09-07  Herbiboar                                 Fossil Island              L80
2018-03-14  Drift Net Fishing                         Fossil Is. underwater      L44/47F
2019-01-10  Aerial Fishing (4 fish)                   Kebos Lowlands             L35–87
2019-07-25  Crystal impling                           Song of the Elves          L80
2024-03-20  Hunter Guild + Rumours                    Varlamore: Part One        L46+
2024-03-20  Embertailed jerboa                        Varlamore                  L39
2024-03-20  Pyre fox                                  Varlamore                  L57
2024-03-20  Sunlight moth                             Varlamore                  L65
2024-03-20  Sunlight antelope                         Varlamore                  L72
2024-03-20  Moonlight moth                            Varlamore                  L75
2024-03-20  Rainbow crab                              Varlamore                  L77
2024-03-20  Tecu salamander                           Varlamore                  L79
2024-03-20  Moonlight antelope                        Varlamore                  L91
2024-03-20  Sunlight Hunter's Crossbow + Hunter's Spear  Varlamore               weapons
2024-03-20  Quetzal Whistle + pet                     Varlamore                  Rumour reward
```

Varlamore: Part One (2024-03-20) was the largest single Hunter expansion in OSRS history — 7 creatures, the Guild, the Rumours assignment system, and 2 new weapons in one drop.

## 11. Design pressure-points — most transposable into Vintage Story

1. **Trap-tile occupancy is a perfect grid-block match.** Bird snare / box trap / magic box are single-tile placeable objects that block player standing. VS blocks already work this way — these become block entities with tick handlers. The 5×5 attraction radius around box traps maps cleanly to a chunk-local AABB scan.

2. **Multi-trap limit by skill level is your XP curve.** The 1/2/3/4/5-trap ladder at L1/20/40/60/80 gives a clean progression without inventing arbitrary numbers. Tie to a Hunter skill stat OR to a tool tier (bronze snare → iron snare → steel snare unlocks +1 simultaneous).

3. **Catch-then-retrieve two-step (Falconry pattern).** Throwable entity drops a temporary loot pile that despawns on a timer if the player doesn't physically collect it. No new system needed — VS already has dropped-item despawn. Maps onto VS's existing throw/projectile + item-drop systems.

4. **Bait + smoke modifier stack (catch-rate +3% bait, +2% smoke).** Trivial to port. Bait = consumable inv item; smoke = use torch on placed trap, sets a 30-min state flag on the block entity. Two small dials that meaningfully change the success curve without touching base rates.

5. **Zone-locked rental tool (Falconry).** The falcon/glove can't leave Matthias's area. In VS this becomes an NPC-bound tool that auto-returns on biome exit, or a tool with `lockedToBiome` metadata. Forces site-specific play loops without requiring permissions/locks/UI tutorials.

6. **Camo-by-biome (RS3 interpretation, NOT OSRS).** OSRS doesn't buff catch-rate from camo, but RS3 does — and the RS3 interpretation is more legible for a VS audience. Larupia = jungle camo, graahk = tropical camo, kyatt = snow camo. Maps onto VS biomes one-to-one. **Design call: pick RS3 interpretation (catch-rate buff) over OSRS (damage reduction) — reads better as a survival mechanic.**

7. **Tracking → footprint chain.** The herbiboar/kebbit mechanic — inspect scenery → follow chained spawned-track entities across tiles → final reveal → harvest. In VS this is a sequence of temporary marker block entities tied to a seed, with the chain ending at a spawned creature or a harvestable. Excellent fit for VS's deterministic worldgen seeds (you can re-derive tracks from chunk hash).

**Honorable mentions:**
- **Birdhouse passive-collection cycle** (~50 min real-time → loot). Maps cleanly to VS's BlockEntity tick + game-time scheduler. AFK-friendly without being trivial.
- **Drift net dual-skill XP.** If runescapemetals ever spans Hunter + Fishing, drift net is the bridge mechanic.

**Do NOT port:**
- Wilderness +1 trap (no Wildy in VS).
- Magic box "Imp-in-a-box" banking item (depends on a Bank system that VS doesn't have).
- Aerial fishing's "always succeeds, only species varies" — too gentle for a survival sandbox; players will expect failure states.

---

## Sources

Primary: https://oldschool.runescape.wiki/w/Hunter and linked sub-pages for each trap, creature, equipment piece, quest, and region.

Open gaps:
- No closed-form catch-rate formula on the wiki for standard traps (only modifier values).
- Falconry throw range, fatigue ticks, retrieval timeout not quantified anywhere.
- Marasamaw plant page returns 404 — likely RS3-only.
- Super hunter potion recipe not fully confirmed this pass.
