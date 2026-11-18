"""
Synthesises every sound in Sole Survivor (no samples, so nothing to license).
Writes 16-bit mono WAVs to src/SoleSurvivor.App/Sounds/, embedded into the exe.
    python3 tools/sounds/gen_sounds.py
Needs numpy and scipy.
"""
import os, numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 22050
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'src', 'SoleSurvivor.App', 'Sounds')
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(1909)

def t(sec): return np.arange(int(SR * sec)) / SR
def noise(sec): return rng.uniform(-1, 1, int(SR * sec))
def env(n, a, d, curve=4.0):
    """Attack (s), then exponential-ish decay over the rest."""
    na = max(1, int(SR * a)); e = np.ones(n)
    e[:na] = np.linspace(0, 1, na)
    rest = n - na
    if rest > 0: e[na:] = np.exp(-curve * np.linspace(0, 1, rest) * (1 / max(d, 1e-3)) * (rest / SR))
    return e
def adsr(n, a, r):
    e = np.ones(n); na, nr = int(SR * a), int(SR * r)
    e[:na] = np.linspace(0, 1, na); e[n - nr:] = np.linspace(1, 0, nr); return e
def lp(x, hz, order=2): b, a = signal.butter(order, hz / (SR / 2), 'low'); return signal.lfilter(b, a, x)
def hp(x, hz, order=2): b, a = signal.butter(order, hz / (SR / 2), 'high'); return signal.lfilter(b, a, x)
def bp(x, lo, hi, order=2): b, a = signal.butter(order, [lo / (SR / 2), hi / (SR / 2)], 'band'); return signal.lfilter(b, a, x)
def tone(freq, sec, kind='sine'):
    ph = 2 * np.pi * np.cumsum(np.broadcast_to(freq, t(sec).shape)) / SR
    if kind == 'tri': return 2 / np.pi * np.arcsin(np.sin(ph))
    return np.sin(ph)
def reverb(x, wet=0.3, size=1.0):
    """Cheap Schroeder reverb: four feedback combs in parallel, then two allpasses."""
    xx = np.concatenate([x, np.zeros(int(SR * 1.2 * size))])
    acc = np.zeros_like(xx)
    for d, g in [(0.0297, .78), (0.0371, .76), (0.0411, .74), (0.0437, .72)]:
        n = max(1, int(SR * d * size))
        a = np.zeros(n + 1); a[0] = 1; a[n] = -g          # y[k] = x[k] + g*y[k-n]
        acc += signal.lfilter([1.0], a, xx)
    for d, g in [(0.005, .7), (0.0017, .7)]:
        n = max(1, int(SR * d))
        b = np.zeros(n + 1); b[0] = -g; b[n] = 1
        a = np.zeros(n + 1); a[0] = 1; a[n] = -g
        acc = signal.lfilter(b, a, acc)
    acc = lp(acc, 4000) * 0.25
    return xx * (1 - wet) + acc * wet
def norm(x, peak=0.85):
    m = np.max(np.abs(x)) or 1; return x / m * peak
def fade(x, ms=8):
    n = int(SR * ms / 1000); x = x.copy(); x[:n] *= np.linspace(0, 1, n); x[-n:] *= np.linspace(1, 0, n); return x
def save(name, x, peak=0.85, fade_ms=8):
    """Normalise and write. Loops (amb_*) are not faded: they must join end-to-start without a dip."""
    x = norm(x, peak)
    if not name.startswith('amb_'): x = fade(x, fade_ms)
    wavfile.write(os.path.join(OUT, name + '.wav'), SR, (x * 32767).astype(np.int16))
def mix(*parts):
    n = max(len(p) for p in parts); out = np.zeros(n)
    for p in parts: out[:len(p)] += p
    return out
def at(x, sec, total):
    out = np.zeros(int(SR * total)); i = int(SR * sec); out[i:i + len(x)] = x[:len(out) - i]; return out

# ------------------------------------------------------------------ interface
def click():
    n = noise(0.04); x = hp(n, 2500) * env(len(n), 0.001, 0.03, 10)
    save('ui_click', x, 0.35, fade_ms=1)

def card_flip():
    n = noise(0.18); x = bp(n, 1500, 6000) * env(len(n), 0.005, 0.15, 6)
    x += 0.4 * bp(noise(0.18), 300, 900) * env(len(n), 0.02, 0.12, 8)
    save('card_play', x, 0.6, fade_ms=2)

def card_buy():
    n = noise(0.14); swish = bp(n, 1200, 5000) * env(len(n), 0.01, 0.12, 6)
    chime = mix(tone(1318.5, 0.5) * env(int(SR * .5), 0.002, 0.45, 5), at(tone(1760, 0.45) * env(int(SR * .45), 0.002, 0.4, 5), 0.07, 0.6))
    save('card_buy', reverb(mix(swish, 0.35 * chime), 0.25, 0.6), 0.6)

def card_discard():
    n = noise(0.3); sweep = signal.chirp(t(0.3), 3500, 0.3, 600)
    x = bp(n, 500, 5000) * env(len(n), 0.01, 0.25, 4) * (0.6 + 0.4 * sweep)
    coin = tone(880, 0.25) * env(int(SR * .25), 0.002, 0.2, 6) * 0.25
    save('card_discard', mix(x, at(coin, 0.12, 0.4)), 0.55)

def dice():
    total = 0.55; out = np.zeros(int(SR * total))
    for i in range(9):
        tt = 0.03 + i * 0.05 + rng.uniform(-0.01, 0.01)
        c = bp(noise(0.03), 1800, 7000) * env(int(SR * .03), 0.0005, 0.02, 12) * (1 - i / 12)
        body = tone(rng.uniform(450, 700), 0.03) * env(int(SR * .03), 0.0005, 0.02, 12) * 0.5
        out += at(c + body, tt, total)
    save('dice', reverb(out, 0.15, 0.4), 0.6)

def chime_up():
    notes = [523.25, 659.25, 783.99, 1046.5]; total = 1.4; out = np.zeros(int(SR * total))
    for i, f in enumerate(notes):
        out += at((tone(f, 1.0) + 0.3 * tone(f * 2, 1.0)) * env(int(SR * 1.0), 0.003, 0.9, 4), i * 0.09, total)
    save('saved', reverb(out, 0.35, 1.0), 0.6)

def found():
    total = 0.7; out = np.zeros(int(SR * total))
    for i, f in enumerate([1568, 2093, 2637]):
        out += at(tone(f, 0.4) * env(int(SR * .4), 0.002, 0.35, 6), i * 0.06, total)
    save('found', reverb(out, 0.3, 0.6), 0.45)

def nothing():
    x = lp(noise(0.25), 400) * env(int(SR * .25), 0.005, 0.2, 6) + 0.6 * tone(110, 0.25) * env(int(SR * .25), 0.005, 0.2, 7)
    save('nothing', x, 0.5, fade_ms=2)

def heal():
    f = np.linspace(392, 784, int(SR * 0.6))
    x = tone(f, 0.6) * adsr(int(SR * .6), 0.05, 0.3) * 0.6 + 0.3 * tone(f * 1.5, 0.6) * adsr(int(SR * .6), 0.1, 0.3)
    save('heal', reverb(x, 0.35, 0.8), 0.45)

def bell():
    dur = 2.4; base = 220
    x = sum(a * tone(base * r, dur) * env(int(SR * dur), 0.002, dur * d, 3)
            for r, a, d in [(1, 1, .9), (2.76, .5, .5), (5.4, .25, .3), (8.93, .12, .2), (0.5, .4, 1)])
    save('round', reverb(x, 0.35, 1.2), 0.5)

# ------------------------------------------------------------------ survivor
def step():
    x = lp(noise(0.12), 600) * env(int(SR * .12), 0.003, 0.08, 8) + 0.5 * tone(70, 0.12) * env(int(SR * .12), 0.002, 0.06, 10)
    x += 0.25 * bp(noise(0.12), 2000, 5000) * env(int(SR * .12), 0.001, 0.04, 14)
    save('step', x, 0.5, fade_ms=1)

def hit_killer():
    n = noise(0.35)
    x = lp(n, 1800) * env(len(n), 0.001, 0.2, 7) + 0.9 * tone(np.linspace(140, 60, len(n)), 0.35) * env(len(n), 0.001, 0.25, 6)
    save('hit', reverb(x, 0.2, 0.5), 0.8)

def miss():
    n = noise(0.35); x = bp(n, 600, 3000) * np.sin(np.linspace(0, np.pi, len(n))) ** 2
    save('miss', x, 0.45)

# ------------------------------------------------------------------ killer
def killer_move():
    dur = 1.1; n = noise(dur)
    sweep = np.linspace(300, 1200, len(n))
    x = np.zeros(len(n))
    for i in range(0, len(n), 512):          # moving band-pass = breathy whoosh
        seg = slice(i, i + 512); f = sweep[i]
        x[seg] = bp(n, f * 0.7, f * 1.3)[seg]
    x *= np.sin(np.linspace(0, np.pi, len(n))) ** 1.5
    drone = 0.5 * tone(55, dur) * np.sin(np.linspace(0, np.pi, len(n)))
    save('killer_move', reverb(x + drone, 0.4, 1.0), 0.5)

def killer_strike():
    n = noise(0.6)
    thud = 1.2 * tone(np.linspace(90, 40, len(n)), 0.6) * env(len(n), 0.001, 0.4, 5)
    crack = hp(n, 1500) * env(len(n), 0.001, 0.08, 14)
    save('strike', reverb(thud + 0.6 * crack, 0.25, 0.7), 0.9)

def taken():
    dur = 1.6
    x = sum(tone(f, dur, 'tri') * env(int(SR * dur), 0.01, 1.3, 3) for f in [233.08, 246.94, 349.23])
    x = lp(x, 2000) + 0.4 * lp(noise(dur), 300) * env(int(SR * dur), 0.05, 1.0, 4)
    save('taken', reverb(x, 0.45, 1.2), 0.55)

def tide_sting():
    dur = 2.2
    f = 55 * (1 + 0.02 * np.sin(np.linspace(0, 12, int(SR * dur))))
    x = (tone(f, dur) + 0.5 * tone(f * 1.5, dur) + 0.3 * tone(f * 2.01, dur)) * adsr(int(SR * dur), 0.4, 1.0)
    x += 0.5 * bp(noise(dur), 200, 800) * adsr(int(SR * dur), 0.8, 0.8)
    save('tide', reverb(x, 0.4, 1.4), 0.6)

def finale():
    dur = 4.0
    boom = 1.4 * tone(np.linspace(70, 30, int(SR * 1.5)), 1.5) * env(int(SR * 1.5), 0.002, 1.2, 4)
    drone = sum(tone(f, dur, 'tri') for f in [41.2, 61.7, 82.4, 87.3]) * adsr(int(SR * dur), 0.6, 1.5) * 0.4
    rumble = lp(noise(dur), 150) * adsr(int(SR * dur), 0.3, 1.5)
    save('finale', reverb(mix(boom, drone, rumble), 0.4, 1.6), 0.9)

def win():
    dur = 3.0
    chord = sum(tone(f, dur) * adsr(int(SR * dur), 0.3, 1.6) for f in [261.6, 329.6, 392, 523.3]) * 0.4
    shimmer = sum(tone(f, dur) * adsr(int(SR * dur), 0.8, 1.6) for f in [1046.5, 1318.5]) * 0.12
    save('win', reverb(chord + shimmer, 0.45, 1.5), 0.7)

def lose():
    dur = 3.2; out = np.zeros(int(SR * dur))
    for i, f in enumerate([196, 174.6, 155.6, 130.8]):
        out += at(tone(f, 1.4, 'tri') * env(int(SR * 1.4), 0.02, 1.2, 3), i * 0.45, dur)
    out = lp(out, 1500) + 0.3 * lp(noise(dur), 120) * adsr(int(SR * dur), 0.5, 1.5)
    save('lose', reverb(out, 0.45, 1.5), 0.7)

# ------------------------------------------------------------------ ambient loops (seamless)
def loop(x, xfade=1.5):
    """Make x loop seamlessly by crossfading its tail into its head."""
    n = int(SR * xfade); head, tail = x[:n], x[-n:]
    ramp = np.linspace(0, 1, n)
    x = x[:-n].copy(); x[:n] = head * ramp + tail * (1 - ramp)
    return x

def sea():
    dur = 13.5; n = noise(dur)
    tt = np.linspace(0, dur, len(n))
    swell = 0.55 + 0.45 * np.sin(2 * np.pi * tt / 6.0) ** 2           # waves every ~6 s
    x = lp(n, 700) * swell + 0.25 * bp(noise(dur), 1500, 4000) * swell ** 3
    gulls = np.zeros(len(n))
    for s0 in [3.2, 9.1]:
        g = tone(np.linspace(1400, 1000, int(SR * .35)), .35) * np.sin(np.linspace(0, np.pi, int(SR * .35))) * 0.06
        gulls += at(g, s0, dur)
    save('amb_gallowmouth', loop(x + gulls), 0.35)

def wind():
    dur = 14.0; n = noise(dur); tt = np.linspace(0, dur, len(n))
    gust = 0.4 + 0.6 * (0.5 + 0.5 * np.sin(2 * np.pi * tt / 7.0 + np.sin(tt)))
    x = np.zeros(len(n)); centre = 500 + 350 * np.sin(2 * np.pi * tt / 7.0)
    for i in range(0, len(n), 1024):
        seg = slice(i, i + 1024); c = centre[i]
        x[seg] = bp(n, c * 0.6, c * 1.4)[seg]
    x = x * gust + 0.3 * lp(noise(dur), 200)
    save('amb_whitepine', loop(x), 0.32)

def rain():
    dur = 12.0; n = noise(dur)
    hiss = bp(n, 1500, 8000) * 0.5 + lp(noise(dur), 400) * 0.4
    drops = np.zeros(len(n))
    for _ in range(500):
        d = bp(noise(0.01), 2000, 6000) * env(int(SR * .01), 0.0005, 0.008, 12) * rng.uniform(0.2, 0.7)
        drops += at(d, rng.uniform(0, dur - 0.02), dur)
    thunder = lp(noise(4.0), 120) * env(int(SR * 4.0), 0.6, 3.0, 2) * 1.6
    x = hiss + drops + at(thunder, 6.0, dur)
    save('amb_stagnes', loop(x), 0.32)

for fn in [click, card_flip, card_buy, card_discard, dice, chime_up, found, nothing, heal, bell,
           step, hit_killer, miss, killer_move, killer_strike, taken, tide_sting, finale, win, lose,
           sea, wind, rain]:
    fn()
files = sorted(f for f in os.listdir(OUT) if f.endswith('.wav'))
print(len(files), 'sounds,', sum(os.path.getsize(os.path.join(OUT, f)) for f in files) // 1024, 'KB')
print(', '.join(f[:-4] for f in files))
