"""Procedural chiptune sound effects (sfxr-style), written as 16-bit mono WAVs.

Run: .venv/bin/python sfx.py <output_dir>
"""

import sys
import wave
from pathlib import Path

import numpy as np

SR = 44100


def osc(freq, dur, shape="square", duty=0.5):
    """freq may be a scalar or an array (per-sample frequency for sweeps)."""
    n = int(SR * dur)
    f = np.broadcast_to(np.asarray(freq, np.float64), (n,))
    phase = np.cumsum(f / SR) % 1.0
    if shape == "square":
        return np.where(phase < duty, 1.0, -1.0)
    if shape == "triangle":
        return 4 * np.abs(phase - 0.5) - 1
    if shape == "saw":
        return 2 * phase - 1
    if shape == "noise":
        # Sample-and-hold noise clocked at `freq`, like the NES noise channel.
        steps = np.floor(np.cumsum(f / SR)).astype(int)
        rng = np.random.default_rng(7)
        vals = rng.uniform(-1, 1, steps.max() + 2)
        return vals[steps]
    raise ValueError(shape)


def env(n, attack=0.005, release=0.05, sustain=1.0):
    e = np.full(n, sustain)
    a = min(n, int(SR * attack))
    r = min(n, int(SR * release))
    if a:
        e[:a] = np.linspace(0, sustain, a)
    if r:
        e[-r:] *= np.linspace(1, 0, r)
    return e


def sweep(f0, f1, dur, curve=1.0):
    t = np.linspace(0, 1, int(SR * dur)) ** curve
    return f0 * (f1 / f0) ** t


def tone(freq, dur, shape="square", duty=0.5, vol=0.5, release=0.04):
    x = osc(freq, dur, shape, duty)
    return x * env(len(x), release=release) * vol


def seq(*parts):
    return np.concatenate(parts)


def note(n):
    """MIDI note number -> Hz."""
    return 440.0 * 2 ** ((n - 69) / 12)


def lowpass(x, alpha=0.3):
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc += alpha * (v - acc)
        y[i] = acc
    return y


def make_all():
    s = {}
    s["jump"] = tone(sweep(280, 720, 0.16, 0.7), 0.16, duty=0.25, vol=0.35)
    s["jump_big"] = tone(sweep(200, 560, 0.2, 0.7), 0.2, duty=0.25, vol=0.38)
    s["coin"] = seq(tone(note(83), 0.07, duty=0.5, vol=0.3, release=0.005), tone(note(88), 0.35, duty=0.5, vol=0.3, release=0.3))
    s["stomp"] = seq(tone(sweep(600, 150, 0.1), 0.1, duty=0.5, vol=0.45), tone(sweep(300, 90, 0.08), 0.08, "triangle", vol=0.5))
    s["bump"] = tone(sweep(180, 90, 0.12), 0.12, "triangle", vol=0.7) + tone(160, 0.12, "noise", vol=0.15)
    brk = tone(sweep(1800, 300, 0.35), 0.35, "noise", vol=0.5, release=0.25)
    s["brick_break"] = lowpass(brk, 0.35)
    s["powerup_appear"] = seq(*[tone(note(n), 0.05, duty=0.25, vol=0.3, release=0.01) for n in [60, 67, 72, 64, 71, 76, 67, 74, 79]])
    s["powerup"] = seq(*[tone(note(n), 0.06, duty=0.5, vol=0.3, release=0.01) for n in [60, 64, 67, 72, 76, 79, 84, 79, 84, 88]])
    s["shrink"] = seq(*[tone(note(n), 0.07, duty=0.25, vol=0.3, release=0.01) for n in [79, 72, 74, 67, 69, 60]])
    s["one_up"] = seq(*[tone(note(n), 0.1, duty=0.5, vol=0.3, release=0.02) for n in [76, 79, 88, 84, 86, 91]])
    s["kick"] = tone(sweep(900, 200, 0.12), 0.12, duty=0.5, vol=0.35)
    s["flagpole"] = tone(sweep(400, 1600, 1.0, 1.0) * (1 + 0.03 * np.sin(np.linspace(0, 60, int(SR * 1.0)))), 1.0, duty=0.5, vol=0.25, release=0.1)
    s["pause"] = seq(tone(note(76), 0.08, vol=0.3), tone(note(72), 0.08, vol=0.3), tone(note(76), 0.08, vol=0.3), tone(note(72), 0.12, vol=0.3))
    s["hurry"] = seq(*[tone(note(n), 0.09, duty=0.5, vol=0.3) for n in [84, 0 + 84, 88, 84, 88, 91]])
    s["ui_tap"] = tone(sweep(900, 1300, 0.05), 0.05, duty=0.5, vol=0.25, release=0.02)
    s["fireworks"] = lowpass(tone(sweep(2500, 400, 0.5), 0.5, "noise", vol=0.6, release=0.45), 0.25)
    return s


def write_wav(path: Path, x: np.ndarray):
    x = np.clip(x, -1, 1)
    pcm = (x * 32767 * 0.9).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


if __name__ == "__main__":
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    for name, data in make_all().items():
        write_wav(out / f"sfx_{name}.wav", data)
        print(f"sfx_{name}.wav {len(data) / SR:.2f}s")
