"""
Generates src/SoleSurvivor.App/ArtExtra.cs: location icons and character figures for
Larkspur Lane, the Kestrel Motel and Ashgrove House, plus an SVG preview of each in /tmp/preview.
    python3 tools/art/gen_art.py
Each drawing is a list of parts:
    ("p", d, fill, stroke, thickness, opacity)   a path (stroke may be None)
    ("glow", cx, cy, r, colour, alpha)           a soft radial glow
    ("legL"/"legR", d, fill)                     a leg that swings while walking (figures only)
    ("eyes", d, fill, glow colour)               glowing eyes/face (figures only)
Icons are 84 x 64 with the ground at y=62; figures give their own size and foot point.
"""
import os, random
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
rng = random.Random(4242)

def p(d, fill, stroke=None, th=1, op=1): return ("p", d, fill, stroke, th, op)
def glow(cx, cy, r, c, a): return ("glow", cx, cy, r, c, a)
def ell(cx, cy, rx, ry): return f"M{cx-rx:g},{cy:g} a{rx:g},{ry:g} 0 1 1 {2*rx:g},0 a{rx:g},{ry:g} 0 1 1 {-2*rx:g},0 Z"
def rect(x, y, w, h): return f"M{x:g},{y:g} h{w:g} v{h:g} h{-w:g} Z"

# ============================================================ Larkspur Lane
def diner(): return [
    p(rect(6, 36, 72, 26), "#C8C2B8", "#7A746A"),
    p("M4,36 L80,36 L80,31 L4,31 Z", "#8E2A3A"),
    p("M4,33.5 h76", "none", "#E8E4D8", 0.8),
    glow(42, 46, 32, "#FFE0A0", 0.4),
    p(rect(10, 40, 16, 12) + " " + rect(30, 40, 16, 12) + " " + rect(58, 40, 16, 12), "#FFE8A8", "#8A6F35", 0.6),
    p(rect(49, 42, 7, 20), "#2A3A40"),
    p("M40,31 L40,22 M44,31 L44,22", "none", "#5A5A5A", 1.4),
    glow(42, 15, 20, "#FF6FB0", 0.55),
    p(rect(22, 8, 40, 14), "#1A0F1E", "#FF6FB0", 1.6),
    p("M27,12 v6 q4,0 4,-3 q0,-3 -4,-3 M34,12 v6 M37,18 v-6 l4,6 v-6 M44,12 v6 h4 M44,15 h3 M44,12 h4 M51,18 v-6 h3 q2,0 2,1.6 q0,1.4 -2,1.4 h-3 M54,15 l3,3", "none", "#FFB8DA", 1.1),
]
def your_house(): return [
    glow(30, 44, 26, "#FFE08A", 0.3),
    p(rect(8, 30, 40, 32), "#6A7A9A", "#3A4660"),
    p("M4,32 L28,12 L52,32 Z", "#3A3048", "#221C2C"),
    p(rect(14, 36, 10, 8) + " " + rect(32, 36, 10, 8) + " " + rect(14, 50, 10, 8), "#FFE08A", "#8A6F35", 0.6),
    p(rect(32, 49, 8, 13), "#4A2A2A"),
    p(rect(50, 40, 28, 22), "#5E6C88", "#3A4660"),
    p("M48,41 L64,32 L80,41 Z", "#3A3048", "#221C2C"),
    p(rect(54, 46, 20, 16), "#8A94AA", "#5E6C88"),
    p("M54,50 h20 M54,54 h20 M54,58 h20", "none", "#6E7890", 0.8),
    p(ell(60, 58.5, 3.8, 3.8) + " " + ell(72, 58.5, 3.8, 3.8), "none", "#E8E8E8", 1.1),
    p("M60,58.5 L65,52.5 L72,58.5 M65,52.5 L70,52.5 L72,58.5 M64,51 h3 M70,52.5 l1,-2.5 h2", "none", "#E8574A", 1.2),
]
def school():
    wins = []
    for row, y in enumerate([40, 50]):
        for x in [6, 14, 22, 56, 64, 72]:
            wins.append(rect(x, y, 5, 6))
    return [
        p(rect(2, 34, 80, 28), "#8A4A3A", "#5A2E24"),
        p("M2,38 h80 M2,46 h80 M2,54 h80", "none", "#7A3E30", 0.6),
        p(" ".join(wins), "#1A1A24"),
        glow(16.5, 43, 8, "#FFE08A", 0.4),
        p(rect(14, 40, 5, 6), "#FFE08A"),
        p(rect(34, 14, 16, 20), "#9A5A48", "#5A2E24"),
        p("M32,14 L42,4 L52,14 Z", "#3A2A2A"),
        p(ell(42, 22, 5, 5), "#F0E8D0", "#3A2A2A", 0.8),
        p("M42,22 L42,18.5 M42,22 L44.5,23.5", "none", "#222222", 0.9),
        p(rect(37, 50, 10, 12), "#2A1A14"),
        p("M78,34 L78,6", "none", "#AAAAAA", 1),
        p("M78,6 L86,8.5 L78,11 Z", "#3A6AB0"),
    ]
def clinic(): return [
    p(rect(6, 26, 72, 36), "#D8DEE4", "#8A949E"),
    p("M4,26 h76 v-4 h-76 Z", "#7A8690"),
    glow(42, 36, 34, "#9ADCE8", 0.35),
    p(rect(10, 31, 64, 9), "#9ADCE8", "#7AB8C4", 0.6),
    p("M10,33.5 h64 M10,36 h64 M10,38.5 h64", "none", "#7AB8C4", 0.5),
    p(rect(10, 46, 14, 8) + " " + rect(60, 46, 14, 8), "#2A3440"),
    p(rect(34, 44, 16, 18), "#9ADCE8", "#5A6A74", 0.8),
    p("M42,44 v18", "none", "#5A6A74", 0.8),
    p("M30,44 h24 v-3 h-24 Z", "#3A6AB0"),
    p(ell(42, 14, 7, 7), "#3A6AB0", "#E8E4D8", 0.8),
    p("M44.5,9.5 A5,5 0 1 0 44.5,18.5 A3.8,4.6 0 1 1 44.5,9.5 Z", "#FFE8A8"),
]
def playground(): return [
    p("M0,64 Q42,50 84,64 Z", "#4A4030"),
    p("M6,62 L14,26 L22,62 M38,62 L46,26 L54,62", "none", "#B8463A", 2),
    p("M14,26 L46,26", "none", "#B8463A", 2.2),
    p("M23,26 L22,46 M29,26 L28,46 M35,26 L38,44 M41,26 L44,44", "none", "#9A9A9A", 0.8),
    p("M20,46 h10 v2 h-10 Z M36,44 h10 v2 h-10 Z", "#3A3A3A"),
    p("M60,62 L60,30 M66,62 L66,30 M60,36 h6 M60,42 h6 M60,48 h6 M60,54 h6", "none", "#C9C2B0", 1.2),
    p("M58,30 h10 v-3 h-10 Z", "#E8C24A"),
    p("M68,30 Q76,44 82,62 L78,62 Q72,46 66,32 Z", "#E8C24A", "#9A7A20", 0.8),
    p(ell(30, 60, 10, 2.4), "#C9B080"),
]
def neighbour(): return [
    p(rect(10, 34, 56, 28), "#8A8A70", "#4E4E3A"),
    p("M4,36 L38,14 L72,36 Z", "#4A3A44", "#2A2028"),
    p("M54,24 L54,14 L60,14 L60,28 Z", "#6A4A3A"),
    glow(24, 47, 14, "#C8A8FF", 0.6),
    p(rect(18, 42, 12, 10), "#C8A8FF", "#5A4A7A", 0.6),
    p("M24,42 v10 M18,47 h12", "none", "#5A4A7A", 0.6),
    p(rect(46, 42, 12, 10), "#1A1A20"),
    p(rect(33, 46, 9, 16), "#3A2A3A"),
    p("M0,62 L0,54 M5,62 L5,54 M70,62 L70,54 M75,62 L75,54 M80,62 L80,54 M0,57 h8 M68,57 h16", "none", "#E8E4D8", 1.2),
    p("M64,36 v4 M62,40 v3 M64,40 v4 M66,40 v3", "none", "#C9A04A", 0.6),
]
def drained_pool(): return [
    p("M2,60 Q4,40 42,38 Q80,40 82,60 Q80,66 42,66 Q4,66 2,60 Z", "#B8B2A4"),
    p("M10,58 L18,44 L66,44 L74,58 Z", "#5A8A9A", "#E8E4D8", 1.4),
    p("M14,58 L20,48 L64,48 L70,58 Z", "#3A6070"),
    p("M24,52 h40 M22,55 h44 M30,48 v10 M42,48 v10 M54,48 v10", "none", "#4E7A88", 0.5),
    p("M30,50 l6,4 l-2,3 M52,48 l-4,6", "none", "#1E2E34", 0.8),
    p(ell(40, 56, 9, 2), "#14242C"),
    p(ell(26, 54, 1.2, 0.8) + " " + ell(48, 51, 1.2, 0.8) + " " + ell(58, 55, 1.2, 0.8), "#8A5A2A"),
    p("M64,44 L64,52 M68,44 L68,52 M64,47 h4 M64,50 h4", "none", "#C8C8C8", 1),
    p("M4,45 L22,41 L22,43 L4,47 Z", "#E8E4D8"),
    p("M7,47 v8", "none", "#8A8A8A", 1.4),
]
def cinema():
    bulbs = " ".join(ell(x, 39, 0.9, 0.9) for x in range(8, 78, 5))
    vbulbs = " ".join(ell(42, y, 0.8, 0.8) for y in range(-3, 15, 4))
    return [
        p(rect(8, 24, 68, 38), "#5A2A3A", "#3A1A24"),
        p(rect(20, 16, 44, 8), "#6A3444"),
        p("M38,16 L38,-6 L46,-6 L46,16 Z", "#2A1A20", "#E8C24A", 1.2),
        p(vbulbs, "#FFF4C0"),
        glow(42, 36, 30, "#E8C24A", 0.4),
        p("M4,40 L80,40 L76,32 L8,32 Z", "#E8C24A", "#9A7A20", 0.8),
        p(bulbs, "#FFF4C0"),
        p("M14,36 h56", "none", "#5A2A3A", 1.4),
        p(rect(30, 46, 24, 16), "#1A0E14"),
        p("M36,46 v16 M42,46 v16 M48,46 v16", "none", "#3A1A24", 0.8),
        p(rect(12, 46, 12, 12), "#C8A8FF", "#3A1A24", 0.6),
        p(rect(60, 46, 12, 12), "#9ADCE8", "#3A1A24", 0.6),
    ]

# ============================================================ Kestrel Motel
def gas_station(): return [
    glow(31, 34, 28, "#FFE8A8", 0.45),
    p("M8,26 L8,62 M54,26 L54,62", "none", "#9A9A90", 2),
    p("M2,26 L60,26 L60,19 L2,19 Z", "#E8E4D8", "#8A8A80"),
    p("M2,23.5 h58", "none", "#C8553D", 2),
    p(rect(18, 46, 8, 16) + " " + rect(36, 46, 8, 16), "#C8553D", "#7A2E25"),
    p(rect(19.5, 48, 5, 3) + " " + rect(37.5, 48, 5, 3), "#FFE8A8"),
    p("M26,52 q3,0 3,4 v6 M44,52 q3,0 3,4 v6", "none", "#2A2A2A", 0.8),
    glow(72, 46, 12, "#FFE8A8", 0.45),
    p(rect(66, 36, 12, 26), "#3A6AB0", "#1E3A6A"),
    p(rect(68, 40, 8, 10), "#FFE8A8"),
    p("M66,36 h12 v-3 h-12 Z", "#1E3A6A"),
]
def motel_office(): return [
    p(rect(6, 36, 58, 26), "#C8A878", "#7A6440"),
    p("M2,36 h66 v-4 h-66 Z", "#5A4A3A"),
    glow(23, 48, 16, "#9FE8FF", 0.55),
    p(rect(12, 42, 22, 12), "#9FE8FF", "#4A6A74", 0.6),
    p("M14,45 h3 M20,48 h4 M26,44 h3 M16,51 h5 M28,50 h3", "none", "#E8FFFF", 0.6),
    p(rect(44, 42, 10, 20), "#3A2A20"),
    p("M74,62 L74,10", "none", "#6A6A6A", 2),
    glow(70, 16, 18, "#FF5A6A", 0.5),
    p(rect(58, 9, 26, 13), "#1A1214", "#FF5A6A", 1.4),
    p("M60.5,18.5 v-5 l1.6,2.6 l1.6,-2.6 v5 M66,16 a1.6,2.5 0 1 1 0.1,0 Z M70,13.5 h3.2 M71.6,13.5 v5 M78,13.5 h-3 v5 h3 M75,16 h2.4 M79.6,13.5 v5 h2.6", "none", "#FF8A96", 0.9),
    glow(72, 29, 10, "#FFD24A", 0.5),
    p("M60,26 L76,26 L81,29 L76,32 L60,32 Z", "none", "#FFD24A", 1.2),
]
def motel_room(dark):
    parts = [
        p(rect(4, 30, 76, 32), "#B89870", "#6A5438"),
        p("M2,30 h80 v-5 h-80 Z", "#5A4A3A"),
    ]
    if not dark:
        parts += [
            p(rect(34, 38, 14, 24), "#4A6A5A", "#2A3A30"),
            p(rect(39, 42, 4, 4), "#E8C24A"),
            glow(19, 46, 14, "#9FE8FF", 0.6),
            p(rect(10, 40, 18, 12), "#9FE8FF", "#4A6A74", 0.6),
            p("M12,43 h2 M18,46 h3 M22,43 h2 M14,49 h4", "none", "#E8FFFF", 0.6),
            p(rect(56, 44, 14, 8), "#8A8A8A", "#5A5A5A"),
            p("M58,46 h10 M58,48 h10 M58,50 h10", "none", "#5A5A5A", 0.6),
        ]
    else:
        parts += [
            p(rect(34, 38, 14, 24), "#0E0E0E"),
            p("M48,62 L48,38 L52,40 L52,62 Z", "#4A6A5A"),
            p(rect(39, 42, 4, 4), "#E8C24A"),
            glow(65, 46, 10, "#9FE8FF", 0.25),
            p(rect(56, 40, 18, 12), "#1A2A34", "#4A6A74", 0.6),
            p(rect(8, 42, 16, 20), "#D8DEE4", "#8A949E"),
            p(rect(11, 45, 10, 4), "#3A6AB0"),
        ]
    return parts
def swimming_pool(): return [
    p("M2,60 Q4,40 42,38 Q80,40 82,60 Q80,66 42,66 Q4,66 2,60 Z", "#C8BCA4"),
    p("M10,58 L18,44 L66,44 L74,58 Z", "#3A6A4A", "#E8E4D8", 1.4),
    p("M24,52 q6,-3 12,0 M44,50 q6,3 12,0 M30,56 q5,-2 10,0", "none", "#5A8A5A", 0.8),
    p("M4,45 L22,41 L22,43 L4,47 Z", "#E8E4D8"),
    p("M7,47 v8", "none", "#8A8A8A", 1.4),
    p("M66,44 L66,52 M70,44 L70,52 M66,47 h4 M66,50 h4", "none", "#C8C8C8", 1),
    p("M72,62 L77,52 L83,52 M73,58 L83,58 L81,62", "none", "#E8E4D8", 1.2),
    p("M75,52 L80,52 L79,58 L74,58 Z", "#C8553D"),
]
def laundromat():
    washers = " ".join(ell(x, 45, 5, 5) for x in [16, 30, 44, 58, 72])
    inner = " ".join(ell(x, 45, 3.4, 3.4) for x in [16, 30, 44, 58, 72])
    return [
        p(rect(4, 28, 76, 34), "#6A8A9A", "#3A5A6A"),
        p("M4,28 h76 v-7 h-76 Z", "#E8E4D8"),
        p("M12,24.5 h20 M36,24.5 h12 M52,24.5 h20", "none", "#3A6AB0", 1.5),
        p(rect(8, 34, 68, 22), "#1A2228", "#9AB0BA"),
        glow(44, 45, 30, "#E8FFFF", 0.3),
        p(washers, "#D8DEE4", "#8A949E", 0.6),
        p(inner, "#C8E8F0"),
        p("M14,44 h2 M28,46 h3 M43,43 h2 M57,46 h2 M70,44 h3", "none", "#5A6A74", 0.6),
        p(rect(36, 56, 12, 6), "#3A5A6A"),
    ]
def tower(): return [
    p("M28,62 L40,-6 L52,62", "none", "#9A9AA4", 1.6),
    p("M31,50 L49,40 M49,50 L31,40 M33,34 L47,24 M47,34 L33,24 M35,16 L45,8 M45,16 L35,8", "none", "#8A8A94", 0.8),
    p("M30,52 h20 M32,38 h16 M35,22 h10 M37,10 h6", "none", "#8A8A94", 0.8),
    p("M40,-6 L40,-14", "none", "#9A9AA4", 1.2),
    glow(40, -14, 7, "#FF3A3A", 0.8), glow(36, 10, 6, "#FF3A3A", 0.7), glow(45, 32, 6, "#FF3A3A", 0.7),
    p(ell(40, -14, 1.6, 1.6) + " " + ell(36.5, 10, 1.4, 1.4) + " " + ell(44.5, 32, 1.4, 1.4), "#FF6A6A"),
    p("M52,28 a6,6 0 0 1 10,6 Z", "#D8D8D8", "#8A8A8A", 0.6),
    p(rect(58, 48, 18, 14), "#6A6A60", "#3A3A34"),
    p("M56,48 h22 v-3 h-22 Z", "#4A4A44"),
    glow(66, 54, 8, "#9FE8FF", 0.5),
    p(rect(62, 51, 7, 5), "#9FE8FF"),
]
def drive_in():
    cols = ["#C0C0C0", "#C0C000", "#00C0C0", "#00C000", "#C000C0", "#C00000", "#0000C0"]
    bars = [p(rect(6 + i * 68 / 7, 6, 68 / 7 + 0.2, 18), c) for i, c in enumerate(cols)]
    return [
        p("M10,62 L12,30 M70,62 L68,30", "none", "#5A5A5A", 2),
        glow(40, 18, 40, "#E8FFFF", 0.25),
        p(rect(4, 4, 72, 28), "#E8E8E8", "#5A5A5A"),
        *bars,
        p(rect(6, 24, 68, 6), "#1A1A1A"),
        p("M10,27 h10 M24,27 h6 M60,27 h10", "none", "#E8E8E8", 1),
        p("M22,62 v-8 M58,62 v-8", "none", "#6A6A6A", 1),
        p(rect(20, 52, 4, 3) + " " + rect(56, 52, 4, 3), "#3A3A3A"),
        p("M28,62 L28,54 L34,50 L56,50 L60,54 L68,54 L68,62 Z", "#7A5A3A", "#3A2A1A"),
        p("M29,58 h38", "none", "#C8A878", 1.6),
        p(rect(35, 51, 8, 3) + " " + rect(45, 51, 8, 3), "#9FE8FF"),
        p(ell(35, 62, 3, 3) + " " + ell(61, 62, 3, 3), "#1A1A1A"),
    ]

# ============================================================ Ashgrove House
def foyer():
    stars = " ".join(ell(x, y, 0.5, 0.5) for x, y in [(32, 26), (40, 20), (50, 28), (45, 34), (36, 38)])
    return [
        p(rect(8, 10, 68, 42), "#6A3A30", "#3A1E18"),
        p("M0,64 L12,52 L72,52 L84,64 Z", "#D8D0C0"),
        p("M18,52 L10,64 M30,52 L26,64 M42,52 L42,64 M54,52 L58,64 M66,52 L74,64 M6,58 h72", "none", "#3A3030", 0.8),
        p("M26,52 L26,22 Q42,8 58,22 L58,52 Z", "#1A2A44"),
        p(stars, "#FFFFFF"),
        p("M26,52 L20,50 L20,24 L26,22 Z M58,52 L64,50 L64,24 L58,22 Z", "#5A2A1A", "#2A140E", 0.8),
        p(rect(12, 14, 6, 38) + " " + rect(66, 14, 6, 38), "#D8D0C0", "#8A8070", 0.8),
        p("M42,10 v5 M33,17 Q42,23 51,17", "none", "#C9A04A", 1.2),
        glow(42, 15, 12, "#FFD27A", 0.6),
        p(ell(33, 15.5, 0.9, 1.4) + " " + ell(42, 18.5, 0.9, 1.4) + " " + ell(51, 15.5, 0.9, 1.4), "#FFE8A8"),
    ]
def grand_hall(): return [
    p(rect(2, 8, 80, 54), "#4A2A24", "#2A1612"),
    p("M2,44 h80", "none", "#6A3A2E", 1.4),
    p(rect(10, 38, 28, 24), "#8A8070", "#4A4238"),
    p(rect(16, 46, 16, 16), "#120A08"),
    glow(24, 58, 8, "#E8743A", 0.35),
    p("M8,38 h32 v-3 h-32 Z", "#6A6050"),
    p(rect(14, 13, 20, 19), "#C9A04A"),
    p(rect(16, 15, 16, 15), "#2A1E2A"),
    p(ell(24, 22, 2.6, 3.4), "#D8C8B0"),
    p("M20,30 Q24,24 28,30 Z", "#3A2A3A"),
    p("M44,62 L44,56 L50,56 L50,50 L56,50 L56,44 L62,44 L62,38 L68,38 L68,32 L82,32 L82,62 Z", "#5A2A1E", "#2A140E", 0.8),
    p("M46,58 h4 M52,52 h4 M58,46 h4 M64,40 h4", "none", "#8A1E24", 2),
    p("M44,50 L74,22", "none", "#8A5A3A", 1.4),
    p("M50,46 v6 M56,40 v6 M62,34 v6 M68,28 v6", "none", "#8A5A3A", 0.8),
]
def library():
    books = []
    palette = ["#7A1E24", "#2A4A3A", "#1E2A5A", "#8A6A2A", "#4A2A4A", "#5A3A1E", "#2A2A2A"]
    for x0, x1 in [(8, 36), (48, 76)]:
        for shelf_y in [20, 32, 44, 56]:
            x = x0
            while x < x1 - 1:
                w = rng.choice([2, 2, 3])
                h = rng.choice([8, 9, 10])
                if rng.random() < 0.1:
                    x += 2; continue
                books.append((rect(x, shelf_y - h, w - 0.4, h), rng.choice(palette)))
                x += w
    by_colour = {}
    for d, c in books: by_colour.setdefault(c, []).append(d)
    return [
        p(rect(2, 6, 80, 56), "#3A2418"),
        glow(42, 34, 34, "#FFD27A", 0.18),
        p(rect(6, 8, 32, 54) + " " + rect(46, 8, 32, 54), "#5A3A24", "#2A1A10"),
        *[p(" ".join(ds), c) for c, ds in by_colour.items()],
        p("M6,20 h32 M6,32 h32 M6,44 h32 M6,56 h32 M46,20 h32 M46,32 h32 M46,44 h32 M46,56 h32", "none", "#2A1A10", 1.4),
        p("M34,62 L40,8 M40,62 L46,8 M35,52 h6 M36.2,42 h6 M37.3,32 h6 M38.4,22 h6", "none", "#B08A50", 1.2),
    ]
def dining(): return [
    p(rect(2, 8, 80, 42), "#3A1A20"),
    p("M0,64 L4,50 L80,50 L84,64 Z", "#4A2A1A"),
    p(rect(30, 12, 24, 22), "#1A2A44"),
    p("M42,12 v22 M30,23 h24", "none", "#3A1A20", 1),
    p("M26,10 L32,10 L30,36 L24,36 Z M52,10 L58,10 L60,36 L54,36 Z", "#7A1E24", "#4A0E14", 0.6),
    p("M16,46 L16,36 L22,36 L22,46 M30,46 L30,36 L36,36 L36,46 M48,46 L48,36 L54,36 L54,46 M62,46 L62,36 L68,36 L68,46", "none", "#5A2E18", 1.6),
    p("M8,52 L76,52 L72,46 L12,46 Z", "#6A3A20", "#3A1E10", 0.8),
    p("M12,46 h60 v2 h-60 Z", "#E8E0D0"),
    p("M14,52 v10 M70,52 v10", "none", "#3A1E10", 2),
    p(ell(22, 47, 2.2, 0.8) + " " + ell(32, 47, 2.2, 0.8) + " " + ell(52, 47, 2.2, 0.8) + " " + ell(62, 47, 2.2, 0.8), "#F4F0E8"),
    p("M42,46 v-8 M37,40 q5,4 10,0 M37,40 v-3 M47,40 v-3 M42,38 v-3", "none", "#C9A04A", 1),
    glow(42, 35, 12, "#FFD27A", 0.6),
    p(ell(37, 36, 0.8, 1.3) + " " + ell(47, 36, 0.8, 1.3) + " " + ell(42, 34, 0.8, 1.3), "#FFE8A8"),
]
def conservatory(): return [
    p("M4,62 L4,30 Q42,0 80,30 L80,62 Z", "#1E2C28", "#C8D0C8"),
    glow(42, 30, 30, "#BFE6DE", 0.15),
    p("M22,62 L22,16 M42,62 L42,8 M62,62 L62,16 M4,44 h76 M10,24 Q42,4 74,24", "none", "#9AA8A0", 0.8),
    p("M14,62 L16,44 M16,44 q-8,-2 -10,4 M16,44 q6,-6 12,-2 M16,44 q2,-8 -2,-12 M16,44 q-6,-6 -4,-12", "none", "#6A5A3A", 1.4),
    p("M68,62 L68,48 M68,50 q-5,-4 -8,0 M68,52 q5,-5 9,-1 M68,48 q-1,-6 3,-8", "none", "#5A6A3A", 1.2),
    p("M30,62 Q30,52 42,52 Q54,52 54,62 Z", "#6A6A64", "#3A3A34", 0.6),
    p("M33,56 Q42,54 51,56 Q42,58 33,56 Z", "#0E1418"),
    p("M42,52 v-8", "none", "#6A6A64", 1.6),
    p("M36,44 q6,4 12,0 Z", "#6A6A64"),
]
def gallery():
    frames = [(6, 12, 20, 24), (30, 9, 24, 30), (58, 12, 20, 24)]
    gold = " ".join(rect(x, y, w, h) for x, y, w, h in frames)
    inner = " ".join(rect(x + 2, y + 2, w - 4, h - 4) for x, y, w, h in frames)
    return [
        p(rect(2, 6, 80, 56), "#5A1E24", "#2A0E12"),
        p(rect(2, 54, 80, 8), "#3A2418"),
        p(gold, "#C9A04A", "#7A5A20", 0.6),
        p(inner, "#2A1E2A"),
        p(ell(16, 22, 3, 4) + " " + ell(68, 22, 3, 4), "#D8C8B0"),
        p("M12,34 Q16,27 20,34 Z M64,34 Q68,27 72,34 Z", "#3A2A3A"),
        p(rect(32, 11, 20, 26), "#0E0A0E"),
        glow(42, 24, 14, "#E8B060", 0.25),
        p("M8,50 Q22,56 36,50 Q50,56 64,50 Q71,54 78,50", "none", "#8A1E24", 1.6),
        p("M8,50 v10 M36,50 v10 M64,50 v10 M78,50 v10", "none", "#C9A04A", 1.4),
    ]
def attic(): return [
    p("M0,62 L42,6 L84,62 Z", "#3A2C24", "#1E1610"),
    p("M42,6 L42,62 M21,34 L63,34 M10,48 h64", "none", "#2A1E16", 1.4),
    glow(42, 22, 14, "#C8D8F0", 0.5),
    p(ell(42, 22, 6, 6), "#C8D8F0", "#2A1E16", 1),
    p("M42,16 v12 M36,22 h12", "none", "#2A1E16", 1),
    p("M8,62 Q10,44 21,42 Q32,44 34,62 Z", "#C8C0B0", "#8A8270", 0.6),
    p("M52,62 Q53,40 63,37 Q73,40 74,62 Z", "#B8B0A0", "#8A8270", 0.6),
    p(ell(63, 36, 4, 4.5), "#B8B0A0", "#8A8270", 0.6),
    p(rect(36, 52, 12, 10), "#5A3A20", "#2A1A10"),
    p("M36,55 h12", "none", "#C9A04A", 1),
]
def cellar():
    racks = " ".join(ell(x, y, 1.4, 1.4) for x in range(20, 38, 4) for y in range(30, 60, 4))
    return [
        p("M2,62 L2,14 Q42,-2 82,14 L82,62 Z", "#4A4440", "#2A2624"),
        p("M2,24 h12 M70,24 h12 M2,36 h12 M70,36 h12 M2,48 h12 M70,48 h12 M8,14 v10 M76,14 v10 M8,36 v12 M76,36 v12", "none", "#2A2624", 0.8),
        p("M14,62 L14,24 Q42,8 70,24 L70,62 Z", "#1A1614"),
        p(rect(17, 27, 22, 34), "#2A1E16"),
        p(racks, "#3A0E14", "#5A4A3A", 0.5),
        glow(57, 52, 14, "#E8743A", 0.55),
        p(rect(48, 38, 18, 24), "#3A3634", "#0E0C0A"),
        p(rect(52, 46, 10, 10), "#E8743A"),
        p("M52,49 h10 M52,52 h10", "none", "#7A3A1E", 0.6),
        p("M57,38 L57,20", "none", "#3A3634", 3),
    ]

ICONS = {
    "larkspur": [diner, your_house, school, clinic, playground, neighbour, drained_pool, cinema],
    "kestrel": [gas_station, motel_office, lambda: motel_room(False), lambda: motel_room(True), swimming_pool, laundromat, tower, drive_in],
    "ashgrove": [foyer, grand_hall, library, dining, conservatory, gallery, attic, cellar],
}

# ============================================================ figures
def static_specks(x, y, w, h, n=30):
    light, dark = [], []
    for _ in range(n):
        px, py = x + rng.uniform(0, w - 0.8), y + rng.uniform(0, h - 0.8)
        (light if rng.random() < 0.5 else dark).append(rect(round(px, 1), round(py, 1), 0.8, 0.8))
    return " ".join(light), " ".join(dark)

def maya(): return dict(w=32, h=48, fx=16, fy=47, legs=((13, 36), (19, 36)), parts=[
    p("M5,47 A11,2.5 0 1 1 27,47 A11,2.5 0 1 1 5,47 Z", "#000000", None, 1, 0.45),
    glow(28, 31, 15, "#FFE8A8", 0.55),
    ("legR", "M17,36 L21,36 L21,47 L17,47 Z", "#6A7AAA"),
    ("legL", "M11,36 L15,36 L15,47 L11,47 Z", "#5A6A9A"),
    p("M8,18 L24,18 L26,38 L6,38 Z", "#8A6AB0", "#4E3A6A"),
    p("M11,30 h10 v4 h-10 Z", "none", "#4E3A6A", 0.7),
    p("M16,18 L16,29", "none", "#4E3A6A", 0.7),
    p("M23,21 L28,28", "none", "#8A6AB0", 2.6),
    p("M26.5,26.5 L31.5,26.5 L31.5,30.5 L26.5,30.5 Z", "#3A3A3A"),
    p("M9.5,11 A6.5,7 0 1 1 22.5,11 A6.5,7 0 1 1 9.5,11 Z", "#1E140E"),
    p("M12.5,12.5 A3.5,4 0 1 1 19.5,12.5 A3.5,4 0 1 1 12.5,12.5 Z", "#B07A58", "#7A5038", 0.5),
    p("M14.2,12.6 L15.2,12.6 M16.9,12.6 L17.9,12.6", "none", "#1E140E", 1.1),
    p("M13.8,14.6 q1,0.5 2,0 M16.6,14.6 q1,0.5 2,0", "none", "#6A4A6A", 0.5),
])
def lullaby_man(): return dict(w=36, h=66, fx=18, fy=65, parts=[
    p("M2,65 A16,3.2 0 1 1 34,65 A16,3.2 0 1 1 2,65 Z", "#000000", None, 1, 0.4),
    glow(18, 32, 28, "#C8A8FF", 0.16),
    p("M11,20 L25,20 L29,63 L26,59 L22,64 L18,59 L14,64 L10,59 L7,63 Z", "#9A9488", "#5E5A50", 0.7),
    p("M14,24 L13,58 M18,22 L18,58 M22,24 L23,58", "none", "#868074", 0.6),
    p("M11,22 Q4,34 5,50 M25,22 Q32,30 31,42", "none", "#9A9488", 2.4),
    p("M5,50 l-1.2,5.5 M5,50 l0.4,6 M5,50 l2,5 M31,42 l2.4,3.6 M31,42 l0.6,4.4", "none", "#E8E0D0", 0.7),
    p("M32.5,44 L35.5,41 M35.5,41 a1.6,1.6 0 1 1 1.2,-2.6 M35.5,41 a1.6,1.6 0 1 0 -2.6,-1.2", "none", "#C9A04A", 1),
    p("M11,10.5 a7,7.5 0 1 1 14,0 a7,7.5 0 1 1 -14,0 Z", "#F0ECE2", "#B8B0A0", 0.5),
    p("M18,3 A7,7.5 0 0 1 18,18 A4.6,7.5 0 0 0 18,3 Z", "#D2CCC0"),
    ("eyes", "M14,9.6 q1.5,1.3 3,0 M19,9.6 q1.5,1.3 3,0", "#5A4A7A", "#C8A8FF"),
    p("M14,13.6 q4,3.2 8,0", "none", "#3A3440", 0.8),
    p("M10,7.5 Q13,0 21,0.5 Q30,1.5 34,12 L31.5,13 Q28,6 24,6.5 Q16,4.5 10,7.5 Z", "#6A6A8A", "#3A3A5A", 0.6),
    p(ell(33, 14, 2.2, 2.2), "#E8E4D8"),
    p("M12,20 h12 v2 h-12 Z", "#E8E4D8"),
])
def ruth(): return dict(w=32, h=48, fx=16, fy=47, legs=((13, 37), (19, 37)), parts=[
    p("M5,47 A11,2.5 0 1 1 27,47 A11,2.5 0 1 1 5,47 Z", "#000000", None, 1, 0.45),
    glow(29, 30, 15, "#E8FFFF", 0.55),
    ("legR", "M17,37 L21,37 L21,47 L17,47 Z", "#2A2A34"),
    ("legL", "M11,37 L15,37 L15,47 L11,47 Z", "#34343E"),
    p("M8,18 L24,18 L27,40 L5,40 Z", "#B89A6A", "#6A5434"),
    p("M6.6,30 h18.8", "none", "#6A5434", 1.4),
    p("M12,18 L16,24 L20,18", "none", "#6A5434", 0.8),
    p("M10,18 L14,26 M22,18 L18,26", "none", "#1E1E1E", 0.6),
    p(rect(13, 25.5, 6, 4.5), "#1E1E1E"),
    p(ell(16, 27.7, 1.3, 1.3), "#4A6A8A"),
    p("M23,21 L28,28", "none", "#B89A6A", 2.6),
    p("M26.5,26.5 L31.5,26.5 L31.5,30.5 L26.5,30.5 Z", "#2A2A2A"),
    p("M10,11.5 A6,6.8 0 1 1 22,11.5 A6,6.8 0 1 1 10,11.5 Z", "#2A1A14"),
    p("M12.6,12.6 A3.4,4 0 1 1 19.4,12.6 A3.4,4 0 1 1 12.6,12.6 Z", "#E2C3A0", "#9C7E63", 0.5),
    p("M14.3,12.8 L15.1,12.8 M17,12.8 L17.8,12.8", "none", "#2B1F18", 1.1),
    p("M10,12 L12.4,12 L12.4,17 L10,16 Z M22,12 L19.6,12 L19.6,17 L22,16 Z", "#2A1A14"),
])
def presenter():
    light, dark = static_specks(13.6, 7.6, 8.8, 7.8, 40)
    return dict(w=36, h=66, fx=18, fy=65, parts=[
        p("M2,65 A16,3.2 0 1 1 34,65 A16,3.2 0 1 1 2,65 Z", "#000000", None, 1, 0.4),
        glow(18, 14, 18, "#9FE8FF", 0.25),
        p("M11,44 L17,44 L16.4,63 L12,63 Z M19,44 L25,44 L24,63 L19.6,63 Z", "#4A4E58"),
        p("M11,63 h6 v2 h-6 Z M19,63 h6 v2 h-6 Z", "#1A1A1A"),
        p("M9,20 L27,20 L29,45 L7,45 Z", "#6A6E78", "#3A3E46", 0.7),
        p("M13,20 L17.5,31 M23,20 L18.5,31", "none", "#3A3E46", 0.9),
        p("M15.5,20 L18,27 L20.5,20 Z", "#E8E8E0"),
        p("M17.3,21.5 L18.7,21.5 L19.6,32 L18,34.5 L16.4,32 Z", "#B8463A"),
        p("M9,22 Q4,34 7,44 M27,22 Q32,32 30,40", "none", "#6A6E78", 3),
        p(ell(7, 44.5, 1.6, 1.6) + " " + ell(30, 40.5, 1.6, 1.6), "#D8D0C8"),
        p("M30,40 L31.5,32", "none", "#2A2A2A", 1.4),
        p(ell(31.8, 30.6, 1.9, 2.4), "#9A9A9A", "#4A4A4A", 0.5),
        p("M11.6,10.5 a6.4,7.2 0 1 1 12.8,0 a6.4,7.2 0 1 1 -12.8,0 Z", "#D8D0C8", "#8A8278", 0.5),
        p("M11.6,8.6 Q18,0.6 24.4,8.6 Q18,4 11.6,8.6 Z", "#2A2A2A"),
        ("eyes", rect(13.4, 7.4, 9.2, 8.2), "#B8C8CC", "#9FE8FF"),
        p(light, "#FFFFFF"),
        p(dark, "#3A4448"),
        p("M24,11 q2,1 1.6,4 L25,17", "none", "#2A2A2A", 0.6),
    ])
def clara(): return dict(w=32, h=48, fx=16, fy=47, legs=((13, 37), (19, 37)), parts=[
    p("M5,47 A11,2.5 0 1 1 27,47 A11,2.5 0 1 1 5,47 Z", "#000000", None, 1, 0.45),
    glow(28, 26, 15, "#FFD27A", 0.6),
    ("legR", "M17,37 L21,37 L21,47 L17,47 Z", "#2A2A2A"),
    ("legL", "M11,37 L15,37 L15,47 L11,47 Z", "#343434"),
    p("M8,18 L24,18 L26,39 L6,39 Z", "#2E5A4A", "#173328"),
    p("M13,18 L16,26 L19,18", "none", "#173328", 0.9),
    p("M15,18 L16,22 L17,18 Z", "#E8E0D0"),
    p(ell(11.5, 32, 0.8, 0.8) + " " + ell(11.5, 28, 0.8, 0.8), "#C9A04A"),
    p("M23,21 L27.5,26", "none", "#2E5A4A", 2.6),
    p("M27,26 h3 v5 h-3 Z", "#C9A04A"),
    p("M28.5,26 v-3", "none", "#F4F0E8", 1.4),
    p(ell(28.5, 21.5, 0.9, 1.5), "#FFE8A8"),
    p("M10,11.5 A6,6.8 0 1 1 22,11.5 A6,6.8 0 1 1 10,11.5 Z", "#8A3A1E"),
    p(ell(16, 4.5, 3, 2.4), "#8A3A1E"),
    p("M12.6,12.6 A3.4,4 0 1 1 19.4,12.6 A3.4,4 0 1 1 12.6,12.6 Z", "#E9C9A8", "#B8957A", 0.5),
    p("M14.3,12.8 L15.1,12.8 M17,12.8 L17.8,12.8", "none", "#2B1F18", 1.1),
    p(ell(17.5, 12.8, 1.6, 1.6), "none", "#C9A04A", 0.5),
])
def curator(): return dict(w=36, h=66, fx=18, fy=65, parts=[
    p("M2,65 A16,3.2 0 1 1 34,65 A16,3.2 0 1 1 2,65 Z", "#000000", None, 1, 0.4),
    glow(18, 32, 28, "#E8B060", 0.14),
    p("M12,56 L16,56 L16,63 L12,63 Z M20,56 L24,56 L24,63 L20,63 Z", "#1A1A1E"),
    p("M11,63 h6 v2 h-6 Z M19,63 h6 v2 h-6 Z", "#0E0E10"),
    p("M10,20 L26,20 L30,57 L24,55 L18,58 L12,55 L6,57 Z", "#5A1A24", "#2E0A10", 0.7),
    p("M18,22 L18,50", "none", "#2E0A10", 0.7),
    p(ell(16.6, 26, 0.7, 0.7) + " " + ell(16.6, 31, 0.7, 0.7) + " " + ell(16.6, 36, 0.7, 0.7), "#C9A04A"),
    p("M11,20 L13.5,13 L18,18 L22.5,13 L25,20 Z", "#3A0E14", "#1E060A", 0.5),
    p("M16,18 L20,18 L19,24 L17,24 Z", "#E8E0D0"),
    p("M10,22 Q5,34 8,46 M26,22 Q31,34 28,46", "none", "#5A1A24", 3),
    p(ell(8, 46.8, 1.7, 1.7) + " " + ell(28, 46.8, 1.7, 1.7), "#E8E0D0"),
    p("M28,47 Q30,51 26.5,52.5", "none", "#C9A04A", 0.7),
    p(ell(26, 54, 2, 2), "#E8C26A", "#8A6A20", 0.5),
    p("M12,9.5 a6,7 0 1 1 12,0 a6,7 0 1 1 -12,0 Z", "#F4F0E8", "#B8B0A0", 0.5),
    p("M12,6.5 Q18,-1 24,6.5 Q18,2.5 12,6.5 Z", "#B8BCC4"),
    p("M15,4.5 L17,8 L16,11 L19,13 L18,16.5", "none", "#E8B634", 0.8),
    p(ell(15.4, 9.8, 1.4, 1) + " " + ell(20.6, 9.8, 1.4, 1), "#1A1012"),
    ("eyes", ell(15.4, 9.8, 0.5, 0.5) + " " + ell(20.6, 9.8, 0.5, 0.5), "#F0C070", "#E8B060"),
    p("M15.5,14 q2.5,1 5,0", "none", "#8A8070", 0.6),
])

FIGURES = {"Maya": maya, "LullabyMan": lullaby_man, "Ruth": ruth, "Presenter": presenter, "Clara": clara, "Curator": curator}

# ============================================================ emit C#
def cs_str(s): return '"' + s.replace('"', '\\"') + '"'
def cs_part(part, legs_vars):
    kind = part[0]
    if kind == "p":
        _, d, fill, stroke, th, op = part
        e = f"P({cs_str(d)}, {cs_str(fill)}" + (f", {cs_str(stroke)}, {th}" if stroke else (f", null, {th}" if th != 1 else "")) + ")"
        if op != 1: e += f".Also(e => e.Opacity = {op})"
        return e
    if kind == "glow":
        _, cx, cy, r, c, a = part
        return f"Glow({cx}, {cy}, {r}, C({cs_str(c)}), {a})"
    if kind in ("legL", "legR"):
        return f"P({cs_str(part[1])}, {cs_str(part[2])}).Also(e => e.RenderTransform = {legs_vars[kind]})"
    if kind == "eyes":
        _, d, fill, gc = part
        stroke_like = " q" in d  # curved slits are strokes
        core = f"P({cs_str(d)}, \"none\", {cs_str(fill)}, 1)" if stroke_like else f"P({cs_str(d)}, {cs_str(fill)})"
        return core + f".Also(e => e.Effect = new DropShadowEffect {{ Color = C({cs_str(gc)}), BlurRadius = 8, ShadowDepth = 0, Opacity = 1 }})"
    raise ValueError(kind)

out = ["// <auto-generated> by tools/art/gen_art.py. Edit the generator, not this file. </auto-generated>",
       "using System.Windows.Controls;", "using System.Windows.Media;", "using System.Windows.Media.Effects;", "",
       "namespace SoleSurvivor.App;", "",
       "/// <summary>Location icons and figures for Larkspur Lane, the Kestrel Motel and Ashgrove House.</summary>",
       "internal static partial class Art", "{"]
out.append("    private static Canvas ExtraIcon(string scenarioId, int id) => (scenarioId, id) switch")
out.append("    {")
for sid, fns in ICONS.items():
    for i, _ in enumerate(fns):
        out.append(f'        ("{sid}", {i}) => {sid.capitalize()}Icon{i}(),')
out.append("        _ => new Canvas()")
out.append("    };")
out.append("")
for sid, fns in ICONS.items():
    for i, fn in enumerate(fns):
        parts = fn()
        out.append(f"    private static Canvas {sid.capitalize()}Icon{i}() => Group(IconWidth, IconHeight,")
        out.append(",\n".join("        " + cs_part(pt, {}) for pt in parts) + ");")
        out.append("")
for name, fn in FIGURES.items():
    fig = fn()
    out.append(f"    private static Figure {name}()")
    out.append("    {")
    vars_ = {}
    if "legs" in fig:
        (lx, ly), (rx, ry) = fig["legs"]
        out.append(f"        var legL = new RotateTransform(0, {lx}, {ly});")
        out.append(f"        var legR = new RotateTransform(0, {rx}, {ry});")
        vars_ = {"legL": "legL", "legR": "legR"}
    out.append(f"        var body = Group({fig['w']}, {fig['h']},")
    out.append(",\n".join("            " + cs_part(pt, vars_) for pt in fig["parts"]) + ");")
    legs = ", legL, legR" if "legs" in fig else ""
    out.append(f"        return Wrap(body, {fig['w']}, {fig['h']}, {fig['fx']}, {fig['fy']}{legs});")
    out.append("    }")
    out.append("")
out.append("}")
open(os.path.join(ROOT, "src/SoleSurvivor.App/ArtExtra.cs"), "w").write("\n".join(out) + "\n")

# ============================================================ SVG previews
def svg_part(part, gid):
    kind = part[0]
    if kind == "p":
        _, d, fill, stroke, th, op = part
        return f'<path d="{d}" fill="{fill}" stroke="{stroke or "none"}" stroke-width="{th}" opacity="{op}" stroke-linejoin="round" stroke-linecap="round"/>'
    if kind == "glow":
        _, cx, cy, r, c, a = part
        return (f'<defs><radialGradient id="{gid}"><stop offset="0" stop-color="{c}" stop-opacity="{a}"/>'
                f'<stop offset="1" stop-color="{c}" stop-opacity="0"/></radialGradient></defs><circle cx="{cx}" cy="{cy}" r="{r}" fill="url(#{gid})"/>')
    if kind in ("legL", "legR"):
        return f'<path d="{part[1]}" fill="{part[2]}"/>'
    if kind == "eyes":
        _, d, fill, gc = part
        if " q" in d:
            return f'<path d="{d}" fill="none" stroke="{fill}" stroke-width="1"/><path d="{d}" fill="none" stroke="{gc}" stroke-width="2.5" opacity="0.5"/>'
        return f'<path d="{d}" fill="{gc}" opacity="0.5" transform="translate(0,0)"/><path d="{d}" fill="{fill}"/>'

def sheet(title, items, w, h, scale, cols, bg):
    cw, ch = w * scale + 20, h * scale + 40
    rows = (len(items) + cols - 1) // cols
    s = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{cols*cw}" height="{rows*ch+30}" font-family="DejaVu Sans"><rect width="100%" height="100%" fill="{bg}"/>',
         f'<text x="10" y="20" fill="#ddd" font-size="14">{title}</text>']
    for k, (label, parts) in enumerate(items):
        x, y = (k % cols) * cw + 10, (k // cols) * ch + 30
        s.append(f'<g transform="translate({x},{y+20}) scale({scale})">' + "".join(svg_part(pt, f"g{k}_{j}") for j, pt in enumerate(parts)) + "</g>")
        s.append(f'<text x="{x}" y="{y+12}" fill="#aaa" font-size="12">{label}</text>')
    s.append("</svg>")
    return "".join(s)

os.makedirs("/tmp/preview", exist_ok=True)
for sid, fns in ICONS.items():
    open(f"/tmp/preview/icons_{sid}.svg", "w").write(sheet(sid, [(str(i), fn()) for i, fn in enumerate(fns)], 84, 84, 2.2, 4, "#222"))
open("/tmp/preview/figures_new.svg", "w").write(sheet("figures", [(n, fn()["parts"]) for n, fn in FIGURES.items()], 36, 66, 4, 6, "#2a2f33"))
print("ok", sum(len(f) for f in ICONS.values()), "icons,", len(FIGURES), "figures")
