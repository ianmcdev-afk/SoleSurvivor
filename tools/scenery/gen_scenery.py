"""
Generates src/SoleSurvivor.App/Scenery.cs: the baked static background for every scenario
(Gallowmouth: fields, woods, cliffs, beach, stone circle; Whitepine: snowbound pines, ice,
ridge, tracks; St Agnes: walled grounds, dead trees, graves, hedges, puddles).

Everything is placed with collision checks against the locations, roads, token spots,
sea and walls, then baked into a handful of path-data strings (one per colour layer),
so WPF draws it cheaply.  Re-run after moving locations:
    python3 tools/scenery/gen_scenery.py
"""
import math, random, re, os, json

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SCEN_SRC = open(os.path.join(ROOT, 'src/SoleSurvivor.Core/Scenarios.cs')).read()
W, H = 800, 580
rng = random.Random(1907)
CONSTS = {}

def scenario_map(prop):
    """Locations {id: (x, y)} and edges [(a, b)] of one scenario, read from Scenarios.cs."""
    block = SCEN_SRC.split(f'public static ScenarioDef {prop} {{ get; }} = new()')[1].split('public static ScenarioDef ')[0]
    locs = {int(i): (float(x), float(y)) for i, x, y in re.findall(r'new\((\d+), "[^"]+", "[^"]*", ([\d.]+), ([\d.]+)', block)}
    edges_src = re.search(r'Edges = \[(.*?)\],', block).group(1)
    edges = [(int(a), int(b)) for a, b in re.findall(r'\((\d+), (\d+)\)', edges_src)]
    return locs, edges

LOCS, EDGES, SEA_POLY, SHORE_PTS, WALL = {}, [], None, [], None

SHORE = "M34,0 Q52,60 30,140 Q14,220 34,300 Q56,380 40,440 Q60,512 160,520 Q300,532 420,526 Q560,518 680,524 Q760,528 800,520"

def shore_points():
    pts, cur = [], None
    for cmd, args in re.findall(r'([MQ])([^MQ]+)', SHORE):
        nums = list(map(float, re.findall(r'-?[\d.]+', args)))
        if cmd == 'M':
            cur = (nums[0], nums[1]); pts.append(cur)
        else:
            c, e = (nums[0], nums[1]), (nums[2], nums[3])
            for i in range(1, 25):
                t = i / 24
                x = (1-t)**2*cur[0] + 2*(1-t)*t*c[0] + t*t*e[0]
                y = (1-t)**2*cur[1] + 2*(1-t)*t*c[1] + t*t*e[1]
                pts.append((x, y))
            cur = e
    return pts

def in_poly(x, y, poly):
    inside = False
    for i in range(len(poly)):
        x1, y1 = poly[i]; x2, y2 = poly[(i+1) % len(poly)]
        if (y1 > y) != (y2 > y) and x < (x2-x1)*(y-y1)/(y2-y1)+x1:
            inside = not inside
    return inside

def seg_dist(px, py, a, b):
    (x1, y1), (x2, y2) = a, b
    dx, dy = x2-x1, y2-y1
    t = max(0, min(1, ((px-x1)*dx+(py-y1)*dy)/(dx*dx+dy*dy)))
    return math.hypot(px-(x1+t*dx), py-(y1+t*dy))

def shore_dist(x, y):
    return min(seg_dist(x, y, SHORE_PTS[i], SHORE_PTS[i+1]) for i in range(len(SHORE_PTS)-1))

def on_land(x, y, margin=8):
    if SEA_POLY is None: return True
    return not in_poly(x, y, SEA_POLY) and shore_dist(x, y) > margin

def clear(x, y, r=10):
    """True if a feature of radius r at (x,y) stays off locations, tokens, roads, the sea and the walls."""
    if not (r < x < W - 2 and r < y < H): return False
    if WALL is not None and not (WALL + r + 6 < x < W - WALL - r - 6 and WALL + r + 6 < y < H - WALL - r - 2): return False
    if not on_land(x, y, r + 4): return False
    for (lx, ly) in LOCS.values():
        if lx - 104 - r < x < lx + 106 + r and ly - 66 - r < y < ly + 82 + r: return False
    for a, b in EDGES:
        if seg_dist(x, y, LOCS[a], LOCS[b]) < 9 + r: return False
    return True

def f(v): return f"{v:.1f}".rstrip('0').rstrip('.')

layers = {}
def add(layer, d): layers.setdefault(layer, []).append(d)

def use_map(prop, sea=False, wall=None):
    global LOCS, EDGES, SEA_POLY, SHORE_PTS, WALL, layers
    LOCS, EDGES = scenario_map(prop)
    WALL = wall
    if sea:
        SHORE_PTS = shore_points()
        SEA_POLY = SHORE_PTS + [(800, 580), (0, 580), (0, 0)]
    else:
        SHORE_PTS, SEA_POLY = [], None
    layers = {}
    rng.seed(1907)

def blob(cx, cy, rx, ry, layer):
    k = 0.55
    add(layer, f"M{f(cx-rx)},{f(cy)} C{f(cx-rx)},{f(cy-ry*k*1.6)} {f(cx+rx*0.2)},{f(cy-ry*1.2)} {f(cx+rx)},{f(cy-ry*0.3)} "
               f"C{f(cx+rx*1.2)},{f(cy+ry*0.6)} {f(cx-rx*0.3)},{f(cy+ry*1.3)} {f(cx-rx)},{f(cy)} Z")

def scatter(n, r, area, fn, tries=4000):
    """Place up to n things of radius r in area (x0, y0, x1, y1), calling fn(x, y)."""
    placed = []
    for _ in range(tries):
        if len(placed) >= n: break
        x, y = rng.uniform(area[0], area[2]), rng.uniform(area[1], area[3])
        if not clear(x, y, r): continue
        if any(math.hypot(x-px, y-py) < r * 1.6 for px, py in placed): continue
        placed.append((x, y))
    for x, y in sorted(placed, key=lambda p: p[1]):
        fn(x, y)
    return placed

def ellipse(cx, cy, rx, ry):
    return f"M{f(cx-rx)},{f(cy)} a{f(rx)},{f(ry)} 0 1 1 {f(2*rx)},0 a{f(rx)},{f(ry)} 0 1 1 {f(-2*rx)},0 Z"

# ====================================================================== Gallowmouth
def gallowmouth():
    use_map('Gallowmouth', sea=True)
    trees = []
    # ------------------------------------------------------------------ fields + walls
    def field(poly, colour):
        if not all(clear(x, y, 2) for x, y in poly): raise SystemExit(f"field off-limits: {poly}")
        d = "M" + " L".join(f"{f(x)},{f(y)}" for x, y in poly) + " Z"
        add(f"field_{colour}", d)
        add("walls", d)

    field([(214, 16), (272, 12), (276, 62), (218, 66)], "#2F4A2C")
    field([(280, 14), (334, 18), (330, 70), (282, 64)], "#3F3F27")
    field([(218, 74), (276, 70), (274, 126), (222, 124)], "#354A30")
    field([(282, 72), (330, 76), (326, 122), (280, 128)], "#45422A")
    field([(206, 322), (252, 320), (254, 362), (208, 364)], "#3F3F27")
    field([(258, 320), (300, 322), (298, 362), (258, 362)], "#2F4A2C")
    field([(210, 370), (254, 368), (252, 402), (212, 402)], "#354A30")
    field([(258, 368), (298, 370), (296, 402), (256, 402)], "#3F3F27")
    # furrows in two of the fields
    for (x0, y0, x1, y1, n) in [(280, 14, 330, 70, 7), (258, 368, 296, 402, 5)]:
        for i in range(1, n):
            t = i / n
            add("furrows", f"M{f(x0+3)},{f(y0+(y1-y0)*t)} L{f(x1-3)},{f(y0+(y1-y0)*t+2)}")

    # ------------------------------------------------------------------ heather + gorse patches
    def blob(cx, cy, rx, ry, layer):
        k = 0.55
        add(layer, f"M{f(cx-rx)},{f(cy)} C{f(cx-rx)},{f(cy-ry*k*1.6)} {f(cx+rx*0.2)},{f(cy-ry*1.2)} {f(cx+rx)},{f(cy-ry*0.3)} "
                   f"C{f(cx+rx*1.2)},{f(cy+ry*0.6)} {f(cx-rx*0.3)},{f(cy+ry*1.3)} {f(cx-rx)},{f(cy)} Z")

    for _ in range(400):
        if len(layers.get("heather", [])) >= 16: break
        x, y = rng.uniform(60, 790), rng.uniform(10, 520)
        if clear(x, y, 14): blob(x, y, rng.uniform(10, 18), rng.uniform(5, 8), "heather")
    for _ in range(400):
        if len(layers.get("gorse_dots", [])) >= 24: break
        x, y = rng.uniform(60, 790), rng.uniform(10, 520)
        if clear(x, y, 4): add("gorse_dots", f"M{f(x)},{f(y)} a1.4,1.4 0 1 1 2.8,0 a1.4,1.4 0 1 1 -2.8,0 Z")

    # ------------------------------------------------------------------ beach + cliffs (drawn under / over the sea edge)
    beach = "M" + " L".join(f"{f(x)},{f(y)}" for x, y in SHORE_PTS if y > 300)
    cliff = "M" + " L".join(f"{f(x)},{f(y)}" for x, y in SHORE_PTS if y <= 310)
    add("beach", beach)
    add("cliff_edge", cliff)
    # jagged rock teeth along the cliff top
    for (x, y) in SHORE_PTS[1:-1]:
        if y > 300 or rng.random() < 0.35: continue
        s = rng.uniform(4, 8)
        add("cliff_rocks", f"M{f(x-1)},{f(y-s)} l{f(s*1.2)},{f(s*0.5)} l{f(-0.4*s)},{f(s*1.1)} l{f(-s*1.3)},{f(-0.2*s)} Z")

    # ------------------------------------------------------------------ woods, hedgerow trees, bushes
    trees = []
    def try_tree(x, y, kind, scale):
        r = 9 * scale
        if not clear(x, y, r): return False
        if any(math.hypot(x-tx, y-ty) < (r + tr) * 0.75 for tx, ty, _, tr in trees): return False
        trees.append((x, y, kind, r)); return True

    for _ in range(3000):
        if sum(1 for t in trees if t[2] == 'pine') >= 34: break
        x, y = rng.uniform(548, 792), rng.uniform(8, 150)
        try_tree(x, y, 'pine', rng.uniform(0.85, 1.25))
    for _ in range(600):
        x, y = rng.uniform(770, 796), rng.uniform(170, 470)
        try_tree(x, y, 'pine', rng.uniform(0.8, 1.05))
    for _ in range(3000):
        if sum(1 for t in trees if t[2] == 'bush') >= 26: break
        x, y = rng.uniform(60, 790), rng.uniform(10, 510)
        try_tree(x, y, 'bush', rng.uniform(0.6, 0.9))
    # hedgerow trees along the top fields
    for (x, y) in [(210, 140), (338, 132), (200, 412), (310, 412), (346, 20)]:
        try_tree(x, y, 'oak', 1.0)

    trees.sort(key=lambda t: t[1])   # paint back to front
    for x, y, kind, r in trees:
        s = r / 9
        add("tree_shadow", f"M{f(x-8*s)},{f(y+2)} a{f(9*s)},{f(3*s)} 0 1 1 {f(18*s)},0 a{f(9*s)},{f(3*s)} 0 1 1 {f(-18*s)},0 Z")
        if kind == 'pine':
            add("trunk", f"M{f(x-1.2*s)},{f(y)} L{f(x-1.2*s)},{f(y-5*s)} L{f(x+1.2*s)},{f(y-5*s)} L{f(x+1.2*s)},{f(y)} Z")
            add("pine_dark", f"M{f(x)},{f(y-26*s)} L{f(x+6*s)},{f(y-16*s)} L{f(x+3.5*s)},{f(y-16*s)} L{f(x+9*s)},{f(y-5*s)} "
                             f"L{f(x-9*s)},{f(y-5*s)} L{f(x-3.5*s)},{f(y-16*s)} L{f(x-6*s)},{f(y-16*s)} Z")
            add("pine_light", f"M{f(x)},{f(y-26*s)} L{f(x+6*s)},{f(y-16*s)} L{f(x+3.5*s)},{f(y-16*s)} L{f(x+9*s)},{f(y-5*s)} L{f(x+1*s)},{f(y-5*s)} Z")
        elif kind == 'oak':
            add("trunk", f"M{f(x-1.6*s)},{f(y)} L{f(x-1.2*s)},{f(y-9*s)} L{f(x+1.2*s)},{f(y-9*s)} L{f(x+1.6*s)},{f(y)} Z")
            add("oak_dark", f"M{f(x-10*s)},{f(y-12*s)} a{f(10*s)},{f(8*s)} 0 1 1 {f(20*s)},0 a{f(10*s)},{f(8*s)} 0 1 1 {f(-20*s)},0 Z")
            add("oak_light", f"M{f(x-2*s)},{f(y-17*s)} a{f(6*s)},{f(4.5*s)} 0 1 1 {f(12*s)},0 a{f(6*s)},{f(4.5*s)} 0 1 1 {f(-12*s)},0 Z")
        else:
            add("bush_dark", f"M{f(x-9*s)},{f(y-4*s)} a{f(9*s)},{f(6*s)} 0 1 1 {f(18*s)},0 a{f(9*s)},{f(6*s)} 0 1 1 {f(-18*s)},0 Z")
            add("bush_light", f"M{f(x-3*s)},{f(y-7*s)} a{f(4.5*s)},{f(3*s)} 0 1 1 {f(9*s)},0 a{f(4.5*s)},{f(3*s)} 0 1 1 {f(-9*s)},0 Z")

    # ------------------------------------------------------------------ the hanging tree (dead, crows) between Cliff Path, Chapel and the Inn
    def dead_tree(x, y):
        assert clear(x, y, 12), "dead tree blocked"
        add("tree_shadow", f"M{f(x-10)},{f(y+2)} a10,3 0 1 1 20,0 a10,3 0 1 1 -20,0 Z")
        add("dead_tree", f"M{f(x)},{f(y)} L{f(x)},{f(y-22)} M{f(x)},{f(y-12)} L{f(x-9)},{f(y-20)} L{f(x-12)},{f(y-27)} "
                         f"M{f(x-9)},{f(y-20)} L{f(x-4)},{f(y-28)} M{f(x)},{f(y-17)} L{f(x+10)},{f(y-24)} L{f(x+14)},{f(y-22)} "
                         f"M{f(x+10)},{f(y-24)} L{f(x+8)},{f(y-31)} M{f(x)},{f(y-22)} L{f(x+3)},{f(y-32)}")
        for (cx, cy) in [(x-12, y-28), (x+8, y-32), (x+14, y-23)]:
            add("crows", f"M{f(cx-3)},{f(cy)} q1.5,-2 3,0 q1.5,-2 3,0 l-1.2,1.5 l-3.6,0 Z")
    dead_tree(338, 222)

    # ------------------------------------------------------------------ the stone circle (folk horror), centre-right moor
    SC = (586, 358)
    assert clear(SC[0], SC[1], 22), "stone circle blocked"
    add("circle_ground", f"M{f(SC[0]-34)},{f(SC[1])} a34,19 0 1 1 68,0 a34,19 0 1 1 -68,0 Z")
    stones = []
    for i in range(9):
        a = math.tau * i / 9 + 0.2
        sx, sy = SC[0] + 30 * math.cos(a), SC[1] + 16 * math.sin(a)
        stones.append((sy, sx, rng.uniform(9, 14)))
    for sy, sx, h in sorted(stones):
        add("stones", f"M{f(sx-3.2)},{f(sy)} L{f(sx-3.6)},{f(sy-h+3)} Q{f(sx)},{f(sy-h-1)} {f(sx+3.6)},{f(sy-h+3)} L{f(sx+3.2)},{f(sy)} Z")
        add("stones_lit", f"M{f(sx+0.5)},{f(sy-1)} L{f(sx+0.6)},{f(sy-h+2.4)} Q{f(sx+2.6)},{f(sy-h+1.6)} {f(sx+3.4)},{f(sy-h+3)} L{f(sx+3)},{f(sy-1)} Z")
    add("altar", f"M{f(SC[0]-8)},{f(SC[1]+2)} L{f(SC[0]-7)},{f(SC[1]-3)} L{f(SC[0]+7)},{f(SC[1]-3)} L{f(SC[0]+8)},{f(SC[1]+2)} Z")


    # ------------------------------------------------------------------ mill pond between the Inn and the Cannery
    PX, PY = 528, 412
    assert clear(PX, PY, 16), "pond blocked"
    add("pond_bank", f"M{PX-26},{PY} C{PX-26},{PY-14} {PX+4},{PY-17} {PX+24},{PY-8} C{PX+34},{PY+2} {PX+12},{PY+15} {PX-6},{PY+13} C{PX-20},{PY+12} {PX-26},{PY+7} {PX-26},{PY} Z")
    add("pond", f"M{PX-22},{PY} C{PX-22},{PY-11} {PX+3},{PY-14} {PX+20},{PY-7} C{PX+29},{PY+1} {PX+11},{PY+11} {PX-5},{PY+10} C{PX-17},{PY+9} {PX-22},{PY+5} {PX-22},{PY} Z")
    add("pond_glint", f"M{PX-6},{PY-3} l12,-1 M{PX-2},{PY+2} l8,0")
    for i, (rx, ry) in enumerate([(PX-24, PY-2), (PX-21, PY+6), (PX+22, PY-6), (PX+26, PY-1)]):
        add("reeds", f"M{rx},{ry} l-2,-9 M{rx+2},{ry} l1,-11 M{rx+4},{ry} l3,-8")

    # ------------------------------------------------------------------ sea detail: rocks with foam, a wreck
    rocks = [(14, 34), (22, 96), (10, 250), (26, 330), (16, 420), (70, 540), (330, 552), (470, 548), (640, 556), (790, 548)]
    for x, y in rocks:
        assert in_poly(x, y, SEA_POLY), f"rock on land {x},{y}"
        s = rng.uniform(0.8, 1.3)
        add("sea_rocks", f"M{f(x-7*s)},{f(y+2)} L{f(x-4*s)},{f(y-5*s)} L{f(x+2*s)},{f(y-6*s)} L{f(x+7*s)},{f(y)} L{f(x+3*s)},{f(y+3)} Z")
        add("foam", f"M{f(x-10*s)},{f(y+3)} q{f(5*s)},-2 {f(10*s)},0 q{f(5*s)},2 {f(10*s)},0")
    # wreck: broken mast and ribs poking out of the water, bottom right
    add("wreck", "M742,566 L748,540 L750,540 L745,566 Z M733,566 q4,-10 9,-12 M756,566 q-3,-9 -8,-12 M738,554 L760,548")

    CONSTS['StoneCircle'] = SC
    return layers, [
        # (layer, fill, stroke, thickness, opacity, group)
        ("beach", "none", "#5E5641", 16, 1, "under_sea"),
        ("field_#2F4A2C", "#2F4A2C", None, 1, 0.9, "ground"),
        ("field_#3F3F27", "#3F3F27", None, 1, 0.9, "ground"),
        ("field_#354A30", "#354A30", None, 1, 0.9, "ground"),
        ("field_#45422A", "#45422A", None, 1, 0.9, "ground"),
        ("furrows", "none", "#2A2A1A", 1.2, 0.8, "ground"),
        ("walls", "none", "#6A6E62", 2.4, 0.9, "ground"),
        ("heather", "#3E2F3C", None, 1, 0.85, "ground"),
        ("gorse_dots", "#C9A83A", None, 1, 0.7, "ground"),
        ("circle_ground", "#1A2620", "#2C3A31", 1, 1, "ground"),
        ("pond_bank", "#2E3A2C", None, 1, 1, "ground"),
        ("pond", "#103038", "#1E4A50", 1, 1, "ground"),
        ("pond_glint", "none", "#BFE6DE", 1.2, 0.5, "ground"),
        ("reeds", "none", "#5C6B3A", 1.3, 1, "ground"),
        ("cliff_edge", "none", "#2B2F2C", 7, 1, "over_sea"),
        ("cliff_rocks", "#4A504C", "#2B2F2C", 0.8, 1, "over_sea"),
        ("sea_rocks", "#2E3532", "#1C211F", 0.8, 1, "over_sea"),
        ("foam", "none", "#BFE6DE", 1.2, 0.45, "over_sea"),
        ("wreck", "#2A221A", "#2A221A", 1.6, 0.9, "over_sea"),
        ("tree_shadow", "#000000", None, 1, 0.35, "top"),
        ("trunk", "#3A2C1E", None, 1, 1, "top"),
        ("pine_dark", "#16301F", "#0F2216", 0.6, 1, "top"),
        ("pine_light", "#21452D", None, 1, 1, "top"),
        ("oak_dark", "#1E3A24", "#132617", 0.6, 1, "top"),
        ("oak_light", "#2C5232", None, 1, 1, "top"),
        ("bush_dark", "#1F3A26", "#142719", 0.6, 1, "top"),
        ("bush_light", "#2B4E33", None, 1, 1, "top"),
        ("dead_tree", "none", "#2B2420", 2, 1, "top"),
        ("crows", "#0B0B0B", None, 1, 1, "top"),
        ("stones", "#5E625C", "#33362F", 0.8, 1, "top"),
        ("stones_lit", "#7D827A", None, 1, 1, "top"),
        ("altar", "#4C4F4A", "#2E302B", 0.8, 1, "top"),
    ]
# ====================================================================== Whitepine
def whitepine():
    use_map('Whitepine')
    # Ice sheet around the frozen lake and a frozen stream running off the south edge.
    lx, ly = LOCS[6]
    add("ice", f"M{f(lx-120)},{f(ly+6)} C{f(lx-118)},{f(ly-60)} {f(lx+40)},{f(ly-72)} {f(lx+120)},{f(ly-20)} "
               f"C{f(lx+160)},{f(ly+20)} {f(lx+120)},{f(ly+92)} {f(lx+10)},{f(ly+96)} C{f(lx-80)},{f(ly+98)} {f(lx-122)},{f(ly+60)} {f(lx-120)},{f(ly+6)} Z")
    add("ice_cracks", f"M{f(lx-90)},{f(ly+40)} l20,-8 l14,10 l18,-12 M{f(lx+60)},{f(ly+70)} l16,-14 l20,6 M{f(lx-40)},{f(ly-40)} l10,12 l18,-4")
    add("stream", f"M{f(lx+60)},{f(ly+88)} C{f(lx+70)},{f(ly+130)} {f(lx+40)},{f(ly+160)} {f(lx+70)},{f(H+10)}")
    # Mountain ridge across the north-east.
    add("ridge", "M430,0 L470,26 L505,10 L540,34 L580,6 L612,24 L640,4 L676,30 L712,8 L748,28 L780,10 L800,18 L800,0 Z")
    add("ridge_snow", "M470,26 L505,10 L518,20 L505,16 Z M580,6 L612,24 L598,18 Z M640,4 L676,30 L660,20 Z M712,8 L748,28 L730,20 Z")
    # Snow drifts.
    for _ in range(3000):
        if len(layers.get("drifts", [])) >= 26: break
        x, y = rng.uniform(20, 790), rng.uniform(20, 570)
        if clear(x, y, 16): blob(x, y, rng.uniform(16, 30), rng.uniform(6, 10), "drifts")
    # A collapsed tent and a dead campfire.
    def tent(x, y):
        add("tent", f"M{f(x-14)},{f(y)} L{f(x-2)},{f(y-14)} L{f(x+16)},{f(y-4)} L{f(x+12)},{f(y+2)} Z")
        add("tent_snow", f"M{f(x-2)},{f(y-14)} L{f(x+16)},{f(y-4)} L{f(x+6)},{f(y-6)} Z")
    def campfire(x, y):
        add("ash", ellipse(x, y, 7, 3.5))
        for i in range(7):
            a = math.tau * i / 7
            add("rocks_dark", ellipse(x + 10*math.cos(a), y + 5*math.sin(a), 2.4, 1.6))
    scatter(1, 20, (180, 60, 420, 300), tent)
    scatter(1, 14, (40, 260, 420, 560), campfire)
    # Hoofprints wandering across the snow: something walked here.
    for (x0, y0, x1, y1) in [(560, 470, 760, 560), (330, 150, 420, 300)]:
        pts = [(x0 + (x1-x0)*t + rng.uniform(-8, 8), y0 + (y1-y0)*t) for t in [i/9 for i in range(10)]]
        for i, (x, y) in enumerate(pts):
            if clear(x, y, 3):
                side = 3 if i % 2 else -3
                add("prints", ellipse(x + side, y, 1.6, 1.1))
    # Snowy boulders.
    def boulder(x, y):
        s = rng.uniform(0.8, 1.3)
        add("boulder", f"M{f(x-9*s)},{f(y)} L{f(x-7*s)},{f(y-6*s)} L{f(x)},{f(y-9*s)} L{f(x+8*s)},{f(y-5*s)} L{f(x+9*s)},{f(y)} Z")
        add("boulder_snow", f"M{f(x-7*s)},{f(y-6*s)} L{f(x)},{f(y-9*s)} L{f(x+8*s)},{f(y-5*s)} L{f(x+2*s)},{f(y-6.5*s)} Z")
    scatter(14, 10, (20, 20, 790, 570), boulder)
    # Snow-laden pines everywhere there's room.
    trees = []
    for _ in range(6000):
        if len(trees) >= 120: break
        x, y = rng.uniform(10, 796), rng.uniform(10, 575)
        s = rng.uniform(0.8, 1.3)
        r = 9 * s
        if not clear(x, y, r): continue
        if in_poly(x, y, [(lx-125, ly), (lx-60, ly-70), (lx+130, ly-25), (lx+130, ly+95), (lx-90, ly+98)]): continue
        if any(math.hypot(x-tx, y-ty) < (r + tr) * 0.8 for tx, ty, tr in trees): continue
        trees.append((x, y, r))
    for x, y, r in sorted(trees, key=lambda t: t[1]):
        s = r / 9
        add("tree_shadow", f"M{f(x-8*s)},{f(y+2)} a{f(9*s)},{f(3*s)} 0 1 1 {f(18*s)},0 a{f(9*s)},{f(3*s)} 0 1 1 {f(-18*s)},0 Z")
        add("trunk", f"M{f(x-1.2*s)},{f(y)} L{f(x-1.2*s)},{f(y-5*s)} L{f(x+1.2*s)},{f(y-5*s)} L{f(x+1.2*s)},{f(y)} Z")
        add("pine_dark", f"M{f(x)},{f(y-27*s)} L{f(x+6*s)},{f(y-17*s)} L{f(x+3.5*s)},{f(y-17*s)} L{f(x+9.5*s)},{f(y-5*s)} "
                         f"L{f(x-9.5*s)},{f(y-5*s)} L{f(x-3.5*s)},{f(y-17*s)} L{f(x-6*s)},{f(y-17*s)} Z")
        add("pine_snow", f"M{f(x)},{f(y-27*s)} L{f(x+4*s)},{f(y-20.5*s)} L{f(x-4*s)},{f(y-20.5*s)} Z "
                         f"M{f(x+3.5*s)},{f(y-17*s)} L{f(x+6.5*s)},{f(y-12*s)} L{f(x+1*s)},{f(y-14*s)} Z "
                         f"M{f(x-3.5*s)},{f(y-17*s)} L{f(x-6.5*s)},{f(y-12*s)} L{f(x-1*s)},{f(y-14*s)} Z "
                         f"M{f(x+9.5*s)},{f(y-5*s)} L{f(x+4*s)},{f(y-9*s)} L{f(x+2*s)},{f(y-5*s)} Z")
    return layers, [
        ("ice", "#8FB2C6", "#C9E2EE", 1.5, 0.9, "ground"),
        ("ice_cracks", "none", "#E8F6FC", 1, 0.7, "ground"),
        ("stream", "none", "#9FC2D4", 9, 0.8, "ground"),
        ("ridge", "#5E6876", "#3E4652", 1, 1, "ground"),
        ("ridge_snow", "#E8F0F6", None, 1, 1, "ground"),
        ("drifts", "#D6E2EC", None, 1, 0.55, "ground"),
        ("ash", "#2A2A2A", None, 1, 0.9, "ground"),
        ("rocks_dark", "#4A4F56", None, 1, 1, "ground"),
        ("prints", "#5A6878", None, 1, 0.7, "ground"),
        ("tent", "#B8463A", "#6E2A22", 0.8, 1, "top"),
        ("tent_snow", "#EEF4F8", None, 1, 1, "top"),
        ("boulder", "#5E646C", "#3A3F46", 0.8, 1, "top"),
        ("boulder_snow", "#EEF4F8", None, 1, 1, "top"),
        ("tree_shadow", "#1A2430", None, 1, 0.35, "top"),
        ("trunk", "#3A2C1E", None, 1, 1, "top"),
        ("pine_dark", "#1B3A2A", "#10261A", 0.6, 1, "top"),
        ("pine_snow", "#EEF4F8", None, 1, 0.95, "top"),
    ]

# ====================================================================== St Agnes
def stagnes():
    use_map('StAgnes', wall=14)
    # Perimeter wall with a gap at the gatehouse.
    gx = LOCS[0][0]
    add("wall", f"M{gx-70},{H-10} L10,{H-10} L10,10 L790,10 L790,{H-10} L{gx+70},{H-10}")
    piers = [(10, 10), (790, 10), (10, H-10), (790, H-10)] + [(x, 10) for x in range(70, 790, 60)] + [(10, y) for y in range(70, H-10, 60)] + [(790, y) for y in range(70, H-10, 60)] \
            + [(x, H-10) for x in range(70, int(gx-70), 60)] + [(x, H-10) for x in range(int(gx+130), 790, 60)]
    for x, y in piers:
        add("piers", f"M{x-4},{y-4} h8 v8 h-8 Z")
    # Lawns: darker mown patches.
    for _ in range(3000):
        if len(layers.get("lawn", [])) >= 18: break
        x, y = rng.uniform(40, 760), rng.uniform(40, 540)
        if clear(x, y, 18): blob(x, y, rng.uniform(22, 40), rng.uniform(10, 16), "lawn")
    # Graves by the chapel.
    cx, cy = LOCS[2]
    graves = 0
    for gy in range(int(cy) + 92, H - 22, 16):
        for gx2 in range(int(cx) - 120, int(cx) + 10, 15):
            if clear(gx2, gy, 5):
                add("graves", f"M{gx2-3},{gy} L{gx2-3},{gy-7} Q{gx2},{gy-10} {gx2+3},{gy-7} L{gx2+3},{gy} Z")
                graves += 1
    # A weeping angel on a plinth, and benches.
    def angel(x, y):
        add("plinth", f"M{f(x-7)},{f(y)} h14 v-6 h-14 Z")
        add("statue", f"M{f(x-4)},{f(y-6)} L{f(x-3)},{f(y-18)} L{f(x+3)},{f(y-18)} L{f(x+4)},{f(y-6)} Z "
                      f"M{f(x-3)},{f(y-18)} a3,3 0 1 1 6,0 a3,3 0 1 1 -6,0 Z "
                      f"M{f(x-3)},{f(y-16)} L{f(x-11)},{f(y-24)} L{f(x-6)},{f(y-10)} Z M{f(x+3)},{f(y-16)} L{f(x+11)},{f(y-24)} L{f(x+6)},{f(y-10)} Z")
    def bench(x, y):
        add("bench", f"M{f(x-9)},{f(y-4)} h18 v2.5 h-18 Z M{f(x-8)},{f(y-1.5)} v3 M{f(x+8)},{f(y-1.5)} v3")
    def lamp(x, y):
        add("lamppost", f"M{f(x)},{f(y)} L{f(x)},{f(y-20)} M{f(x)},{f(y-20)} l5,0")
        add("lamp_glass", ellipse(x + 5, y - 18, 1.8, 1.8))
    scatter(1, 14, (40, 40, 760, 540), angel)
    scatter(5, 10, (40, 40, 760, 540), bench)
    scatter(6, 8, (40, 40, 760, 540), lamp)
    # Puddles (rain will glint on them).
    def puddle(x, y):
        rx = rng.uniform(8, 16)
        add("puddles", ellipse(x, y, rx, rx * 0.35))
        CONSTS.setdefault('Puddles', []).append((round(x), round(y)))
    scatter(9, 12, (30, 30, 770, 550), puddle)
    # Clipped hedges.
    def hedge(x, y):
        w = rng.uniform(22, 40)
        add("hedge", f"M{f(x-w/2)},{f(y)} L{f(x-w/2)},{f(y-7)} Q{f(x-w/2)},{f(y-11)} {f(x-w/2+4)},{f(y-11)} L{f(x+w/2-4)},{f(y-11)} Q{f(x+w/2)},{f(y-11)} {f(x+w/2)},{f(y-7)} L{f(x+w/2)},{f(y)} Z")
        add("hedge_light", f"M{f(x-w/2+3)},{f(y-9)} L{f(x+w/2-3)},{f(y-9)}")
    scatter(14, 18, (40, 40, 760, 540), hedge)
    # Bare, dead trees.
    def dead(x, y):
        s = rng.uniform(0.8, 1.2)
        add("tree_shadow", ellipse(x, y + 1, 9 * s, 2.6 * s))
        add("dead_tree", f"M{f(x)},{f(y)} L{f(x)},{f(y-22*s)} M{f(x)},{f(y-12*s)} L{f(x-9*s)},{f(y-20*s)} L{f(x-12*s)},{f(y-27*s)} "
                         f"M{f(x-9*s)},{f(y-20*s)} L{f(x-4*s)},{f(y-28*s)} M{f(x)},{f(y-17*s)} L{f(x+10*s)},{f(y-24*s)} L{f(x+14*s)},{f(y-22*s)} "
                         f"M{f(x+10*s)},{f(y-24*s)} L{f(x+8*s)},{f(y-31*s)} M{f(x)},{f(y-22*s)} L{f(x+3*s)},{f(y-32*s)}")
    scatter(32, 13, (24, 24, 776, 556), dead)
    return layers, [
        ("lawn", "#22302A", None, 1, 0.9, "ground"),
        ("puddles", "#1A2A30", "#3A5058", 0.8, 0.95, "ground"),
        ("graves", "#5E5C56", "#3A3834", 0.6, 1, "top"),
        ("wall", "none", "#4A4740", 7, 1, "top"),
        ("piers", "#5E5A52", "#36332E", 0.8, 1, "top"),
        ("hedge", "#1C3424", "#122218", 0.6, 1, "top"),
        ("hedge_light", "none", "#2A4A32", 1.2, 1, "top"),
        ("tree_shadow", "#000000", None, 1, 0.35, "top"),
        ("dead_tree", "none", "#2E2824", 2, 1, "top"),
        ("plinth", "#6A6760", "#45423C", 0.6, 1, "top"),
        ("statue", "#8C8A84", "#55534E", 0.6, 1, "top"),
        ("bench", "#5A4030", "#3A2A20", 1, 1, "top"),
        ("lamppost", "none", "#2A2A2A", 1.6, 1, "top"),
        ("lamp_glass", "#E8C26A", None, 1, 0.9, "top"),
    ]

# ====================================================================== write C#
def emit(name, result):
    lay, order = result
    out = [f"    public static readonly Layer[] {name} =", "    ["]
    preview = {"under_sea": [], "ground": [], "over_sea": [], "top": []}
    for row in order:
        layer, fill, stroke, th, op, group = row
        if layer not in lay: continue
        d = " ".join(lay[layer])
        dashed = ", Dashed: true" if layer == "walls" else ""
        out.append(f'        new("{group}", "{d}", "{fill}", {("null" if stroke is None else chr(34)+stroke+chr(34))}, {th}, {op}{dashed}),')
        dash = ' stroke-dasharray="4.8 2.4"' if layer == "walls" else ""
        preview[group].append(f'<path d="{d}" fill="{fill}" stroke="{stroke or "none"}" stroke-width="{th}" opacity="{op}" stroke-linejoin="round" stroke-linecap="round"{dash}/>')
    out.append("    ];")
    return out, preview

results = {"Gallowmouth": gallowmouth()}
sc = CONSTS['StoneCircle']
results["Whitepine"] = whitepine()
results["StAgnes"] = stagnes()
puddles = CONSTS.get('Puddles', [])

cs = ["// <auto-generated> by tools/scenery/gen_scenery.py. Edit the generator, not this file. </auto-generated>",
      "#nullable enable", "",
      "namespace SoleSurvivor.App;", "",
      "/// <summary>Baked background scenery for each scenario's map. One path per colour layer.</summary>",
      "internal static class Scenery", "{",
      "    public sealed record Layer(string Group, string Data, string Fill, string? Stroke, double Thickness, double Opacity, bool Dashed = false);", "",
      f"    public const double StoneCircleX = {sc[0]}, StoneCircleY = {sc[1]};", "",
      "    /// <summary>St Agnes puddle centres, for rain glints.</summary>",
      "    public static readonly (double X, double Y)[] Puddles = [" + ", ".join(f"({x}, {y})" for x, y in puddles) + "];", "",
      "    public static Layer[] For(string scenarioId) => scenarioId switch",
      "    {", '        "whitepine" => Whitepine,', '        "stagnes" => StAgnes,', "        _ => Gallowmouth", "    };", ""]
previews = {}
for name, res in results.items():
    lines, prev = emit(name, res)
    cs += lines + [""]
    previews[name] = prev
cs += ["}", ""]
open(os.path.join(ROOT, 'src/SoleSurvivor.App/Scenery.cs'), 'w').write("\n".join(cs))
os.makedirs('/tmp/preview', exist_ok=True)
json.dump(previews, open('/tmp/preview/scenery_all.json', 'w'))
print({k: sum(len(v) for v in r[0].values()) for k, r in results.items()})
