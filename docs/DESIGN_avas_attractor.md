# DESIGN — Ava's Attractor (v1)

**STATUS: BUILT 2026-06-11.** Shape modeled from the OSRS wiki detail/equipped renders
(slate pack, grey flap, rotated diamond panel, black pocket, red magnet, pale pouch,
forked dowsing rod over the right shoulder). Worn placement copies the vanilla
worn-backpack root frame (stepParent UpperTorso) verbatim, so it sits exactly where
vanilla packs sit. Compile + load smoke-tested clean on a throwaway dataPath
(18 sources compiled, system started, zero class-registration errors). Remaining
human-eye checks: worn look in-game, GUI transform taste, recipe craft test.
One compiler landmine documented inline: never touch IServerWorldAccessor.LoadedEntities
from script-mod code — ConcurrentDictionary is off the reference list (CS0012, and the
error voids EVERY class in the assembly while still printing "Successfully compiled").

**CRASH + FIX 2026-06-11 (second landmine, client-side):** `wearableInvShape` may NOT
carry a domain prefix. The engine builds the path as literally
`new AssetLocation("shapes/" + attrValue + ".json")` (decompiled
CollectibleBehaviorWearableAttachment.genFullBodyMesh). With the first-draft value
`runescape:item/avasattractor-inv` the colon made AssetLocation parse domain =
`shapes/runescape` → Shape.TryGet returned null → the engine tesselates the null shape
with NO null-guard → hard NRE crash-to-desktop the moment any GUI slot renders the item
(stack: ShapeTesselator.TesselateShape ← genFullBodyMesh_Patch1, Harmony id
attributerenderinglibrary was in the chain but innocent — vanilla has the same hole).
No vanilla item uses wearableInvShape, so there was no example to copy; decompile is the
only ground truth. Fix: the attribute is domainless and resolves in the `game` domain,
so the inv shape now ships at `assets/game/shapes/runescape/avasattractor-inv.json` and
the attribute reads `runescape/avasattractor-inv`. Texture refs inside the shape stay
domain-qualified (`runescape:item/avas/...`) and resolve fine from the game domain.

requested 2026-06-11 ("HARD PIVOT"). source mechanics: OSRS wiki, Ava's attractor
(Animal Magnetism reward). RS → VS translation below.

## what it is in RS (verified on wiki)

- cape-slot device, quest reward, for sub-50 Ranged.
- fired ammo: 60% returned automatically, 20% breaks, 20% drops to ground.
- every ~3.5 min (if the wearer moved ≥3 tiles) it attracts random iron junk —
  mostly iron arrows; rarer: darts, knives, ore, nails, bars, broken arrows, med helms.
- metal TORSO armor interferes — device stops working.
- upgrade path: attractor → accumulator (72%, needs 75 steel arrows) → assembler.

## VS translation

**Item**: `runescape:avas-attractor`, wearable, **clothing category: shoulder**
(VS's cape slot — RHO's correction 2026-06-11, supersedes the neck-slot draft).
Lives in existing `itemtypes/wearable/`. Texture: small copper-coil-on-leather device.

**Mechanic 1 — ammo recovery (the point of the item):**
- server-side: hook projectile impact (EntityProjectile / arrow entity death+stick).
- if shooter wears attractor AND no metal body armor: roll.
  - **60% → arrow teleports back into shooter's inventory** (quiver/offhand/backpack,
    standard TryGiveItemstack; overflow drops at feet).
  - remaining 40%: vanilla behavior untouched (vanilla already breaks/drops by its
    own breakChanceOnImpact — we only intercept the survivors; tune so net feel
    matches 60/20/20).

**Mechanic 2 — junk attraction (the charm):**
- accumulate wearer's traveled distance; every ~3.5 real minutes AND ≥3 blocks moved
  since last pull: spawn one junk item directly into inventory.
- junk table (RS-flavored, VS-native): iron arrow (heavily weighted, the wiki's
  1975/2000), nails-and-strips, metal bits (iron), rusty gear, rare: iron arrowhead,
  poor iron nugget. NO net-positive economy break — junk value stays trivial.
- a player-toggle ("Commune" in RS): sneak+rightclick the worn item toggles junk
  attraction off, recovery stays on.

**Mechanic 3 — interference (the constraint):**
- if body/torso armor slot item code matches chain/lamellar/plate/scale metal armor →
  device fully inert (both mechanics). Leather/cloth/gambeson fine.
- tooltip states it plainly: "Inert while metal torso armor is worn."

**Acquisition (no quests in VS):**
- grid recipe: 1 magnetite nugget (VS has magnetite!) + 2 iron strips (nails-and-strips)
  + 1 leather + 1 resin → avas-attractor. Magnetite IS the animal magnetism.

**Upgrade path (later, mirrors metal tiers):**
- attractor (iron-era, 60%) → accumulator (steel-era, 72%, steel junk table) →
  assembler (mithril+, 80%, no junk, no interference). v1 ships attractor only.

## implementation shape

- new C# class `ItemAvasAttractor` (wearable) + one ModSystem server listener for
  projectile impacts and the junk timer. ~1 source file.
- no new entity, no patches to vanilla projectiles — listener-only, compat-safe.
- BUT: respect [[feedback_vs_mod_compile_classpath]] — bundled-lib references stay out.
- arrows from ANY mod work (we intercept by entity class, not item list).
