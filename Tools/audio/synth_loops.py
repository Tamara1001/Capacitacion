"""Sintetiza los loops de ambiente y efectos continuos del simulador (sin dependencias de licencia).

Uso: python Tools/audio/synth_loops.py <carpeta_salida>
Genera WAV mono 44.1 kHz 16 bit. Los loops son continuos (fundido cruzado entre el final y el inicio).
"""
import os
import sys
import wave

import numpy as np

SR = 44100
RNG = np.random.default_rng(7)


def lowpass(x, cutoff):
    """Pasa bajos de un polo (suficiente para ruidos de ambiente)."""
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def seamless(x, fade_s=0.5):
    """Hace el loop continuo: mezcla el final sobre el inicio y recorta."""
    n = int(fade_s * SR)
    head, tail = x[:n], x[-n:]
    ramp = np.linspace(0, 1, n)
    x = x[:-n].copy()
    x[:n] = head * ramp + tail * (1 - ramp)
    return x


def normalize(x, peak=0.8):
    return x / (np.max(np.abs(x)) + 1e-9) * peak


def save(path, x):
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print(f"[audio] {os.path.basename(path)}  {len(data) / SR:.2f}s")


def motor_hum(seconds=4.0):
    t = np.arange(int(SR * (seconds + 0.5))) / SR
    base = 100.0  # 2x la frecuencia de red: zumbido magnético típico
    x = (0.6 * np.sin(2 * np.pi * base * t) + 0.3 * np.sin(2 * np.pi * base * 2 * t) +
         0.15 * np.sin(2 * np.pi * base * 3 * t) + 0.08 * np.sin(2 * np.pi * 1480 * t))
    # Rodamientos y ventilador: ruido filtrado con leve modulación.
    noise = lowpass(RNG.standard_normal(len(t)), 900) * 3.0
    x += noise * (0.8 + 0.2 * np.sin(2 * np.pi * 12.5 * t))
    return normalize(seamless(x), 0.7)


def hvac(seconds=8.0):
    n = int(SR * (seconds + 0.5))
    x = lowpass(RNG.standard_normal(n), 300) * 4 + lowpass(RNG.standard_normal(n), 1800) * 0.6
    t = np.arange(n) / SR
    x += 0.05 * np.sin(2 * np.pi * 60 * t)
    return normalize(seamless(x), 0.5)


def water_bleed(seconds=4.0):
    n = int(SR * (seconds + 0.5))
    noise = highpass(lowpass(RNG.standard_normal(n), 2500), 250)
    # Gorgoteo: ráfagas aleatorias suavizadas.
    bursts = lowpass(np.abs(RNG.standard_normal(n)) ** 3, 6)
    x = noise * (0.5 + 2.5 * bursts / (bursts.max() + 1e-9))
    return normalize(seamless(x), 0.6)


def water_spray(seconds=3.0):
    n = int(SR * (seconds + 0.5))
    x = highpass(lowpass(RNG.standard_normal(n), 7000), 900)
    return normalize(seamless(x, 0.3), 0.7)


def spark(seconds=0.6):
    n = int(SR * seconds)
    x = np.zeros(n)
    for _ in range(40):
        start = RNG.integers(0, int(n * 0.7))
        length = RNG.integers(80, 900)
        seg = RNG.standard_normal(length) * np.exp(-np.linspace(0, 6, length))
        x[start:start + length] += seg[: n - start]
    x = highpass(x, 1200)
    x *= np.exp(-np.linspace(0, 3, n))
    return normalize(x, 0.9)


def main():
    out = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else ".")
    os.makedirs(os.path.join(out, "Ambience"), exist_ok=True)
    os.makedirs(os.path.join(out, "SFX"), exist_ok=True)
    save(os.path.join(out, "Ambience", "AMB_MotorHum.wav"), motor_hum())
    save(os.path.join(out, "Ambience", "AMB_RoomHVAC.wav"), hvac())
    save(os.path.join(out, "SFX", "SFX_WaterBleed.wav"), water_bleed())
    save(os.path.join(out, "SFX", "SFX_WaterSpray.wav"), water_spray())
    save(os.path.join(out, "SFX", "SFX_Spark.wav"), spark())


main()
