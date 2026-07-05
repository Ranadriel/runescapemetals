#!/usr/bin/env python3
from PIL import Image
import colorsys
import math
import os
import shutil

VANILLA_TEX    = "/home/rana/Desktop/vintagestory/assets/survival/textures"
VANILLA_SHAPES = "/home/rana/Desktop/vintagestory/assets/survival/shapes"
GAME_TEX       = "/home/rana/.config/VintagestoryData/Mods/runescapemetals/assets/game/textures"
GAME_SHAPES    = "/home/rana/.config/VintagestoryData/Mods/runescapemetals/assets/game/shapes"
# Mirror destinations: VS resolves textures per-domain, and the forge contents
# renderer specifically looks up survival:textures/... (because the forge shape
# lives in the survival mod's domain). Mirroring every texture into all three
# domains (game/survival/runescape) defends against domain-specific lookups
# failing silently with the pink-and-white missing-texture render.
SURV_TEX       = "/home/rana/.config/VintagestoryData/Mods/runescapemetals/assets/survival/textures"
MOD_TEX        = "/home/rana/.config/VintagestoryData/Mods/runescapemetals/assets/runescape/textures"

METAL_HUE = {
    "mithril":    220.0,
    "adamantite": 140.0,
    "runite":     190.0,
    "dragon":       0.0,
}

# Distinct vanilla flecks pattern per metal so the four ores look different.
ORE_PATTERN_SOURCE = {
    "mithril":    "cassiterite",
    "adamantite": "magnetite",
    "runite":     "chromite",
    "dragon":     "hematite",
}


def hsv_recolor_pixel(r, g, b, hue_deg):
    R, G, B = r / 255.0, g / 255.0, b / 255.0
    h, s, v = colorsys.rgb_to_hsv(R, G, B)
    new_h = (hue_deg / 360.0) % 1.0
    new_s = min(1.0, max(s, 0.55) * 1.4)
    nR, nG, nB = colorsys.hsv_to_rgb(new_h, new_s, v)
    return int(round(nR * 255)), int(round(nG * 255)), int(round(nB * 255))


def recolor(src_path, dst_path, hue_deg):
    src = Image.open(src_path).convert("RGBA")
    out = Image.new("RGBA", src.size)
    px_in = src.load()
    px_out = out.load()
    w, h = src.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px_in[x, y]
            if a == 0:
                px_out[x, y] = (0, 0, 0, 0)
                continue
            nr, ng, nb = hsv_recolor_pixel(r, g, b, hue_deg)
            px_out[x, y] = (nr, ng, nb, a)
    os.makedirs(os.path.dirname(dst_path), exist_ok=True)
    out.save(dst_path)
    # Mirror into survival: and runescape: domains so domain-specific lookups
    # (forge contents renderer in survival:, mod block refs in runescape:)
    # always find the texture wherever they search.
    rel = os.path.relpath(dst_path, GAME_TEX)
    for base in (SURV_TEX, MOD_TEX):
        mirror = os.path.join(base, rel)
        os.makedirs(os.path.dirname(mirror), exist_ok=True)
        out.save(mirror)


def hue_check(path, expected_hue, tol_deg=15.0, min_sat=0.5):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    w, h = im.size
    n = 0
    sum_sin = 0.0
    sum_cos = 0.0
    sum_s = 0.0
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            R, G, B = r / 255.0, g / 255.0, b / 255.0
            hh, ss, vv = colorsys.rgb_to_hsv(R, G, B)
            if ss < 0.05:
                continue
            angle = hh * 2.0 * math.pi
            sum_sin += ss * math.sin(angle)
            sum_cos += ss * math.cos(angle)
            sum_s += ss
            n += 1
    if n == 0:
        return False, 0.0, 0.0
    mean_angle = math.atan2(sum_sin, sum_cos)
    if mean_angle < 0:
        mean_angle += 2 * math.pi
    mean_hue_deg = mean_angle * 180.0 / math.pi
    mean_sat = sum_s / n
    diff = abs(mean_hue_deg - expected_hue)
    diff = min(diff, 360.0 - diff)
    return (diff <= tol_deg and mean_sat >= min_sat), mean_hue_deg, mean_sat


def emit_shape(src_path, dst_path, src_token, dst_token):
    with open(src_path, "r") as f:
        content = f.read()
    content = content.replace(src_token, dst_token)
    os.makedirs(os.path.dirname(dst_path), exist_ok=True)
    with open(dst_path, "w") as f:
        f.write(content)


def main():
    targets = []

    src = os.path.join(VANILLA_TEX, "block/metal/ingot/iron.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"block/metal/ingot/{metal}.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    for i in (1, 2, 3, 4, 5):
        src = os.path.join(VANILLA_TEX, f"block/metal/sheet/iron{i}.png")
        for metal, hue in METAL_HUE.items():
            dst = os.path.join(GAME_TEX, f"block/metal/sheet/{metal}{i}.png")
            recolor(src, dst, hue)
            targets.append((dst, hue))

    for i in (1, 2, 3, 4):
        src = os.path.join(VANILLA_TEX, f"block/metal/sheet-plain/iron{i}.png")
        for metal, hue in METAL_HUE.items():
            dst = os.path.join(GAME_TEX, f"block/metal/sheet-plain/{metal}{i}.png")
            recolor(src, dst, hue)
            targets.append((dst, hue))

    src = os.path.join(VANILLA_TEX, "block/metal/tarnished/iron.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"block/metal/tarnished/{metal}.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    # No vanilla block/stone/rock/iron1.png exists; sheet/iron1 is a
    # texture-validator placeholder that never renders for metal tools.
    src = os.path.join(VANILLA_TEX, "block/metal/sheet/iron1.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"block/stone/rock/{metal}1.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    for kind in ("brigandine", "chain", "scale", "plate"):
        src = os.path.join(VANILLA_TEX, f"entity/humanoid/serapharmor/{kind}/iron.png")
        for metal, hue in METAL_HUE.items():
            dst = os.path.join(GAME_TEX, f"entity/humanoid/serapharmor/{kind}/{metal}.png")
            recolor(src, dst, hue)
            targets.append((dst, hue))

    src = os.path.join(VANILLA_TEX, "entity/humanoid/serapharmor/lamellar/tinbronze.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"entity/humanoid/serapharmor/lamellar/{metal}.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    # Hematite is the iron-bearing nugget; closest visual match.
    nugget_src = os.path.join(VANILLA_TEX, "item/resource/nugget/hematite.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"item/resource/nugget/{metal}.png")
        recolor(nugget_src, dst, hue)
        targets.append((dst, hue))

    for metal, hue in METAL_HUE.items():
        pattern = ORE_PATTERN_SOURCE[metal]
        for i in (1, 2, 3):
            src = os.path.join(VANILLA_TEX, f"block/stone/ore/{pattern}{i}.png")
            dst = os.path.join(GAME_TEX, f"block/stone/ore/{metal}{i}.png")
            recolor(src, dst, hue)
            targets.append((dst, hue))

    # Lantern: base body, decorative variant, and grid lining — three
    # texture refs the lantern blocktype expects per material.
    src = os.path.join(VANILLA_TEX, "block/metal/lantern/iron.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"block/metal/lantern/{metal}.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    src = os.path.join(VANILLA_TEX, "block/metal/lantern/iron-deco.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"block/metal/lantern/{metal}-deco.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    src = os.path.join(VANILLA_TEX, "block/metal/lantern/grid/iron.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"block/metal/lantern/grid/{metal}.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    # Shield plate face (used by full-metal shield-metal construction).
    src = os.path.join(VANILLA_TEX, "block/metal/plate/iron.png")
    for metal, hue in METAL_HUE.items():
        dst = os.path.join(GAME_TEX, f"block/metal/plate/{metal}.png")
        recolor(src, dst, hue)
        targets.append((dst, hue))

    print(f"\nGenerated {len(targets)} textures.")

    passed = failed = 0
    for path, hue in targets:
        ok, mh, ms = hue_check(path, hue)
        if ok:
            passed += 1
        else:
            failed += 1
            print(f"  FAIL {os.path.relpath(path, GAME_TEX)} mean_hue={mh:.1f} expect={hue} sat={ms:.2f}")
    print(f"Hue check: {passed} pass / {failed} fail")

    shapes_emitted = 0

    for kind in ("brigandine", "chain", "scale", "plate"):
        src = os.path.join(VANILLA_SHAPES, f"entity/humanoid/seraph/armor/{kind}/head-iron.json")
        for metal in METAL_HUE:
            dst = os.path.join(GAME_SHAPES, f"entity/humanoid/seraph/armor/{kind}/head-{metal}.json")
            emit_shape(src, dst, "iron", metal)
            shapes_emitted += 1

    src = os.path.join(VANILLA_SHAPES, "item/tool/blade/falx/iron.json")
    for metal in METAL_HUE:
        dst = os.path.join(GAME_SHAPES, f"item/tool/blade/falx/{metal}.json")
        emit_shape(src, dst, "iron", metal)
        shapes_emitted += 1

    # Limonite is the iron-equivalent vanilla ore (no graded iron shape).
    ore_tier_metals = {
        "poor":      ["mithril", "adamantite", "runite", "dragon"],
        "medium":    ["mithril", "adamantite", "runite", "dragon"],
        "rich":      ["mithril", "adamantite", "runite"],
        "bountiful": ["mithril"],
    }
    for tier, metals in ore_tier_metals.items():
        src = os.path.join(VANILLA_SHAPES, f"item/ore/{tier}/limonite.json")
        for metal in metals:
            dst = os.path.join(GAME_SHAPES, f"item/ore/{tier}/{metal}.json")
            emit_shape(src, dst, "limonite", metal)
            shapes_emitted += 1

    print(f"Emitted {shapes_emitted} shapes.")


if __name__ == "__main__":
    main()
