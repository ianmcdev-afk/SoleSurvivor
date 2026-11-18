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


# ------------------------------------------------------------------ new scenarios
def music_box_notes(notes, step, total, detune=1.0, slow=0.0):
    """A wind-up music box: plucked metallic tines, optionally slowing down as it winds out."""
    out = np.zeros(int(SR * total)); tpos = 0.0
    for i, f in enumerate(notes):
        dur = 1.6
        n = int(SR * dur)
        tine = (tone(f * detune, dur) + 0.35 * tone(f * 2.01 * detune, dur) + 0.12 * tone(f * 5.4, dur)) * env(n, 0.002, 1.4, 5)
        out += at(tine * 0.5, tpos, total)
        tpos += step * (1 + slow * i)
        if tpos > total - 0.2: break
    return out

def amb_larkspur():
    dur = 16.0
    crickets = np.zeros(int(SR * dur))
    for k in range(int(dur * 2.2)):
        c = bp(noise(0.05), 4200, 5200) * env(int(SR * .05), 0.002, 0.04, 8)
        burst = mix(*[at(c, j * 0.07, 0.25) for j in range(3)])
        crickets += at(burst * rng.uniform(0.1, 0.25), rng.uniform(0, dur - 0.3), dur)
    hum = 0.25 * lp(noise(dur), 160)
    # A faint lullaby, a long way off, slightly out of tune.
    notes = [659.3, 587.3, 523.3, 587.3, 659.3, 659.3, 659.3, 587.3, 587.3, 587.3, 659.3, 784.0, 784.0]
    box = reverb(music_box_notes(notes, 0.55, dur, detune=0.985), 0.6, 1.4) * 0.18
    save('amb_larkspur', loop(hum + crickets + box[:int(SR * dur)]), 0.3)

def amb_kestrel():
    dur = 14.0
    hiss = bp(noise(dur), 2000, 9000) * 0.18
    hum = 0.25 * (tone(60, dur) + 0.5 * tone(120, dur) + 0.25 * tone(180, dur))
    tt = np.linspace(0, dur, int(SR * dur))
    buzz = 0.08 * np.sign(np.sin(2 * np.pi * 120 * tt)) * (0.6 + 0.4 * (np.sin(2 * np.pi * tt / 3.7) > 0.8))
    truck = lp(noise(dur), 300) * np.exp(-((tt - 9.0) / 2.0) ** 2) * 0.9
    save('amb_kestrel', loop(hiss + lp(hum, 400) + lp(buzz, 2000) + truck), 0.3)

def amb_ashgrove():
    dur = 12.0
    out = 0.3 * lp(noise(dur), 220) * (0.6 + 0.4 * np.sin(np.linspace(0, 2 * np.pi, int(SR * dur))) ** 2)
    for k in range(int(dur)):  # a slow grandfather clock: tick ... tock
        for off, f in ((0.0, 2600), (0.5, 2100)):
            tk = bp(noise(0.03), f * 0.8, f * 1.2) * env(int(SR * .03), 0.001, 0.025, 10) * 0.6
            out += at(tk, k + off, dur)
    creak = tone(np.linspace(180, 140, int(SR * 0.9)), 0.9, 'tri') * env(int(SR * .9), 0.2, 0.6, 3) * 0.12
    out += at(lp(creak, 900), 6.2, dur)
    save('amb_ashgrove', loop(out), 0.32)

def tide_larkspur():
    notes = [784.0, 659.3, 523.3, 587.3, 493.9]
    x = reverb(music_box_notes(notes, 0.32, 3.0, slow=0.25), 0.5, 1.2)
    save('tide_larkspur', x, 0.75)

def tide_kestrel():
    dur = 2.2
    beep = tone(1000, 1.0) * adsr(int(SR * 1.0), 0.01, 0.05) * 0.5
    burst = bp(noise(0.7), 800, 8000) * env(int(SR * .7), 0.01, 0.6, 2)
    x = mix(at(burst, 0.0, dur), at(beep, 0.6, dur), at(burst * 0.6, 1.6, dur))
    save('tide_kestrel', x, 0.7)

def tide_ashgrove():
    dur = 3.4; out = np.zeros(int(SR * dur))
    for i, f in enumerate([523.3, 392.0, 440.0, 293.7]):
        n = int(SR * 1.6)
        bell = (tone(f, 1.6) + 0.5 * tone(f * 2.76, 1.6) + 0.25 * tone(f * 5.4, 1.6)) * env(n, 0.003, 1.4, 3)
        out += at(bell * 0.5, i * 0.42, dur)
    save('tide_ashgrove', reverb(out, 0.45, 1.3), 0.7)

def drive_car():
    dur = 1.4; tt = t(dur)
    rpm = 40 + 60 * np.clip(tt / 0.6, 0, 1) - 20 * np.clip((tt - 0.9) / 0.5, 0, 1)
    ph = 2 * np.pi * np.cumsum(rpm) / SR
    engine = np.sign(np.sin(ph)) * 0.5 + 0.3 * np.sin(2 * ph)
    x = lp(engine, 700) * adsr(len(tt), 0.08, 0.4) + 0.2 * lp(noise(dur), 500) * adsr(len(tt), 0.1, 0.4)
    save('drive_car', x, 0.42)

def drive_bike():
    dur = 1.2
    ring = (tone(2350, 0.5) + 0.6 * tone(3100, 0.5)) * env(int(SR * .5), 0.002, 0.45, 5)
    chain = np.zeros(int(SR * dur))
    for k in range(16):
        c = hp(noise(0.012), 3000) * env(int(SR * .012), 0.001, 0.01, 10) * 0.35
        chain += at(c, 0.25 + k * 0.055, dur)
    save('drive_bike', mix(at(ring * 0.5, 0, dur), at(ring * 0.4, 0.12, dur), chain), 0.6)

def unlock():
    dur = 0.6
    clack = bp(noise(0.04), 1500, 5000) * env(int(SR * .04), 0.001, 0.03, 10)
    turn = bp(noise(0.12), 600, 2500) * env(int(SR * .12), 0.01, 0.1, 4) * 0.6
    x = mix(at(turn, 0.0, dur), at(clack, 0.14, dur), at(clack * 0.7, 0.3, dur))
    save('unlock', reverb(x, 0.2, 0.4), 0.6)

# ------------------------------------------------------------------ Cedar Hollow, Holloway, Crowhurst
def amb_cedarhollow():
    dur = 15.0; n = noise(dur); tt = np.linspace(0, dur, len(n))
    gust = 0.35 + 0.65 * (0.5 + 0.5 * np.sin(2 * np.pi * tt / 5.0 + 1.3 * np.sin(tt / 1.7)))
    rustle = bp(n, 1800, 6000) * gust * 0.35 + lp(noise(dur), 250) * 0.3
    crickets = np.zeros(len(n))
    for _ in range(int(dur * 1.5)):
        c = bp(noise(0.05), 4200, 5200) * env(int(SR * .05), 0.002, 0.04, 8)
        crickets += at(mix(*[at(c, j * 0.07, 0.25) for j in range(3)]) * rng.uniform(0.08, 0.18), rng.uniform(0, dur - 0.3), dur)
    dog = np.zeros(len(n))
    for t0 in (4.0, 4.5):
        bark = bp(noise(0.12), 400, 1400) * env(int(SR * .12), 0.005, 0.1, 5) * 0.25
        dog += at(bark, t0, dur)
    save('amb_cedarhollow', loop(rustle + crickets + lp(dog, 2000)), 0.3)

def amb_holloway():
    dur = 16.0; out = np.zeros(int(SR * dur))
    # A wheezy, slightly detuned calliope playing an oom-pah waltz, far off.
    melody = [523.3, 659.3, 784.0, 659.3, 587.3, 698.5, 880.0, 698.5, 523.3, 659.3, 784.0, 1046.5, 987.8, 784.0, 659.3, 587.3]
    beat = 0.45
    for i, f in enumerate(melody * 3):
        tpos = i * beat
        if tpos > dur - 0.5: break
        nn = int(SR * beat * 0.9)
        pipe = (tone(f * 0.992, beat * 0.9) + 0.5 * tone(f * 2.01, beat * 0.9) + 0.3 * tone(f * 1.5, beat * 0.9)) * adsr(nn, 0.02, 0.1)
        out += at(pipe * 0.18, tpos, dur)
        if i % 3 == 0: out += at(tone(f / 4, beat * 0.4, 'tri') * adsr(int(SR * beat * 0.4), 0.01, 0.1) * 0.25, tpos, dur)
    out = lp(reverb(out, 0.5, 1.4)[:int(SR * dur)], 2500)
    hum = 0.3 * lp(noise(dur), 180) + 0.06 * tone(50, dur)
    save('amb_holloway', loop(out + hum), 0.3)

def amb_crowhurst():
    dur = 14.0; n = noise(dur); tt = np.linspace(0, dur, len(n))
    howl = np.zeros(len(n)); centre = 650 + 400 * np.sin(2 * np.pi * tt / 6.0) ** 2
    for i in range(0, len(n), 1024):
        seg = slice(i, i + 1024); c = centre[i]
        howl[seg] = bp(n, c * 0.75, c * 1.3)[seg]
    howl *= 0.5 + 0.5 * np.sin(2 * np.pi * tt / 6.0) ** 2
    crackle = np.zeros(len(n))
    for _ in range(140):
        c = hp(noise(0.006), 2500) * env(int(SR * .006), 0.0005, 0.005, 10) * rng.uniform(0.1, 0.4)
        crackle += at(c, rng.uniform(0, dur - 0.02), dur)
    fire = lp(noise(dur), 300) * 0.25
    save('amb_crowhurst', loop(howl * 0.6 + crackle + fire), 0.32)

def tide_cedarhollow():
    # An old two-tone telephone bell: ring-ring, pause, ring-ring.
    dur = 2.6; out = np.zeros(int(SR * dur))
    for t0 in (0.0, 1.4):
        nn = int(SR * 0.9); tt = np.arange(nn) / SR
        trill = np.sign(np.sin(2 * np.pi * 20 * tt))
        bell = (tone(1350, 0.9) + 0.7 * tone(1650, 0.9) + 0.4 * tone(2700, 0.9)) * (0.6 + 0.4 * trill) * adsr(nn, 0.005, 0.05)
        out += at(lp(bell, 4500), t0, dur)
    save('tide_cedarhollow', out, 0.6)

def tide_holloway():
    dur = 1.9
    honk_n = int(SR * 0.35)
    honk = (np.sign(np.sin(2 * np.pi * np.cumsum(np.full(honk_n, 340.0)) / SR)) * 0.6 + tone(680, 0.35)) * adsr(honk_n, 0.01, 0.06)
    slide = tone(np.linspace(1400, 300, int(SR * 1.0)), 1.0) * adsr(int(SR * 1.0), 0.02, 0.2) * 0.6
    x = mix(at(lp(honk, 2500), 0.0, dur), at(lp(honk, 2500), 0.42, dur), at(slide, 0.85, dur))
    save('tide_holloway', reverb(x, 0.25, 0.8), 0.65)

def tide_crowhurst():
    dur = 2.4; out = np.zeros(int(SR * dur))
    t0 = 0.0
    for k in range(11):
        key = bp(noise(0.03), 1200, 4500) * env(int(SR * .03), 0.001, 0.025, 10) + 0.4 * lp(noise(0.03), 300) * env(int(SR * .03), 0.001, 0.02, 10)
        out += at(key * 0.8, t0, dur); t0 += rng.uniform(0.08, 0.16)
    ding = (tone(2093, 1.0) + 0.4 * tone(4186, 1.0)) * env(int(SR * 1.0), 0.002, 0.9, 4) * 0.6
    out += at(ding, t0 + 0.05, dur)
    save('tide_crowhurst', reverb(out, 0.2, 0.5), 0.65)

def drive_cart():
    dur = 1.2; tt = t(dur)
    whine = tone(380 + 260 * np.clip(tt / 0.5, 0, 1), dur) * 0.4 + 0.15 * tone(760 + 520 * np.clip(tt / 0.5, 0, 1), dur)
    rumble = lp(noise(dur), 400) * 0.3
    save('drive_cart', (whine + rumble) * adsr(len(tt), 0.1, 0.35), 0.4)

# ------------------------------------------------------------------ Merriwick
def amb_merriwick():
    # Soft wind through a snowy town, a church peal far off, and now and then a jingle of bells.
    dur = 16.0; n = noise(dur); tt = np.linspace(0, dur, len(n))
    wind = bp(n, 300, 1400) * (0.4 + 0.3 * np.sin(2 * np.pi * tt / 7.0) ** 2) * 0.5
    peal = np.zeros(len(n))
    notes = [784.0, 698.5, 659.3, 587.3, 523.3, 493.9]   # a ring of six, rung down the scale
    for r in range(2):
        for k, f in enumerate(notes):
            nn = int(SR * 1.6)
            b = (tone(f, 1.6) + 0.5 * tone(f * 2.76, 1.6) + 0.3 * tone(f * 5.4, 1.6)) * env(nn, 0.003, 1.4, 3)
            peal += at(b * 0.12, 1.0 + r * 7.0 + k * 0.42, dur)
    peal = lp(reverb(peal, 0.6, 1.6)[:len(n)], 1800)
    jingle = np.zeros(len(n))
    for t0 in (5.0, 12.5):
        for j in range(9):
            c = bp(noise(0.06), 5000, 9000) * env(int(SR * .06), 0.001, 0.05, 8) * rng.uniform(0.05, 0.12)
            jingle += at(c, t0 + j * rng.uniform(0.05, 0.11), dur)
    save('amb_merriwick', loop(wind + peal + jingle), 0.3)

def tide_merriwick():
    # The town clock strikes, and something small chitters back.
    dur = 2.6; out = np.zeros(int(SR * dur))
    for t0 in (0.0, 0.9):
        nn = int(SR * 1.6)
        bong = (tone(196, 1.6) + 0.6 * tone(392 * 1.01, 1.6) + 0.35 * tone(196 * 2.92, 1.6)) * env(nn, 0.004, 1.5, 3)
        out += at(bong * 0.55, t0, dur)
    out = reverb(out, 0.35, 1.2)[:len(out)]
    for k in range(14):
        c = bp(noise(0.02), 2500, 6500) * env(int(SR * .02), 0.001, 0.015, 10) * rng.uniform(0.2, 0.45)
        out += at(c, 1.7 + k * rng.uniform(0.035, 0.06), dur)
    save('tide_merriwick', out, 0.65)

def swarm():
    # Many tiny claws on cobbles, and a squeak or two.
    dur = 0.8; out = np.zeros(int(SR * dur))
    for k in range(30):
        c = bp(noise(0.012), 2000, 7000) * env(int(SR * .012), 0.0005, 0.01, 10) * rng.uniform(0.2, 0.6)
        out += at(c, rng.uniform(0, dur - 0.05), dur)
    for t0 in (0.15, 0.45):
        nn = int(SR * 0.09)
        sq = tone(np.linspace(2600, 3400, nn) + 300 * np.sin(np.linspace(0, 20, nn)), 0.09) * adsr(nn, 0.01, 0.04) * 0.3
        out += at(sq, t0, dur)
    save('swarm', out, 0.55)

for fn in [click, card_flip, card_buy, card_discard, dice, chime_up, found, nothing, heal, bell,
           step, hit_killer, miss, killer_move, killer_strike, taken, tide_sting, finale, win, lose,
           sea, wind, rain, amb_larkspur, amb_kestrel, amb_ashgrove, tide_larkspur, tide_kestrel, tide_ashgrove,
           drive_car, drive_bike, unlock,
           amb_cedarhollow, amb_holloway, amb_crowhurst, tide_cedarhollow, tide_holloway, tide_crowhurst, drive_cart,
           amb_merriwick, tide_merriwick, swarm]:
    fn()
files = sorted(f for f in os.listdir(OUT) if f.endswith('.wav'))
print(len(files), 'sounds,', sum(os.path.getsize(os.path.join(OUT, f)) for f in files) // 1024, 'KB')
print(', '.join(f[:-4] for f in files))
