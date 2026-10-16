"""Genera todos los modelos del Simulador LOTO y los exporta como FBX.

Uso:
  blender -b -P Blender/scripts/build_props.py -- <carpeta_salida> [asset1 asset2 ...]

Sin lista de assets genera todos. Ver convenciones en loto_lib.py.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import loto_lib as L  # noqa: E402

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = os.path.abspath(ARGS[0] if ARGS else ".")
ONLY = set(ARGS[1:])
BUILDERS = {}


def asset(fn):
    BUILDERS[fn.__name__] = fn
    return fn


# ------------------------------------------------------------------ Bomba P-101

@asset
def PumpSkid():
    root = L.empty("PumpSkid")
    parts = [
        L.box("Base", (2.6, 0.9, 0.14), (0, 0, 0.07), "Graphite", 0.01),
        L.box("RailFront", (2.6, 0.05, 0.05), (0, -0.43, 0.165), "Graphite", 0.005),
        L.box("RailBack", (2.6, 0.05, 0.05), (0, 0.43, 0.165), "Graphite", 0.005),
    ]
    for i, x in enumerate((-1.2, 1.2)):
        for j, y in enumerate((-0.38, 0.38)):
            parts.append(L.cyl(f"Anchor{i}{j}", 0.018, 0.05, (x, y, 0.16), "Steel", verts=8))
    L.join("Skid", parts, root)

    motor = [
        L.cyl("MotorBody", 0.25, 0.9, (0.8, 0, 0.6), "PumpBlue", rot=(0, 90, 0), verts=32, bevel=0.01),
        L.cyl("MotorFace", 0.2, 0.06, (0.32, 0, 0.6), "PumpBlue", rot=(0, 90, 0), verts=32),
        L.cyl("MotorShaft", 0.03, 0.12, (0.25, 0, 0.6), "Steel", rot=(0, 90, 0), verts=12),
        L.cyl("FanCover", 0.23, 0.12, (1.31, 0, 0.6), "Graphite", rot=(0, 90, 0), verts=32, bevel=0.008),
        L.box("TerminalBox", (0.2, 0.18, 0.14), (0.72, 0, 0.9), "PumpBlue", 0.01),
        L.cyl("Conduit", 0.02, 0.4, (0.72, 0.2, 0.75), "Steel", rot=(90, 0, 0), verts=10),
        L.box("NamePlate", (0.12, 0.005, 0.07), (0.95, -0.252, 0.66), "White", 0.001),
        L.box("FootA", (0.7, 0.12, 0.2), (0.8, -0.16, 0.25), "PumpBlue", 0.01),
        L.box("FootB", (0.7, 0.12, 0.2), (0.8, 0.16, 0.25), "PumpBlue", 0.01),
    ]
    for k in range(7):
        motor.append(L.cyl(f"Fin{k}", 0.265, 0.02, (0.45 + k * 0.11, 0, 0.6), "PumpBlue", rot=(0, 90, 0), verts=32, bevel=0))
    for k in range(4):
        motor.append(L.torus(f"Grille{k}", 0.05 + k * 0.045, 0.006, (1.375, 0, 0.6), "Dark", rot=(0, 90, 0), major_seg=24, minor_seg=6))
    L.join("Motor", motor, root)

    bracket = [
        L.cyl("BearingHousing", 0.1, 0.32, (-0.06, 0, 0.6), "PumpBlue", rot=(0, 90, 0), verts=24),
        L.box("Pedestal", (0.34, 0.42, 0.3), (-0.4, 0, 0.29), "PumpBlue", 0.01),
        L.box("BracketFoot", (0.4, 0.5, 0.04), (-0.4, 0, 0.16), "PumpBlue", 0.005),
        # Guarda del acople abierta al frente para ver el giro.
        L.box("GuardBack", (0.34, 0.02, 0.36), (0.22, 0.17, 0.6), "Yellow", 0.004),
        L.box("GuardTop", (0.34, 0.36, 0.02), (0.22, 0.0, 0.79), "Yellow", 0.004),
    ]
    L.join("Bracket", bracket, root)


@asset
def PumpCasing():
    root = L.empty("PumpCasing")
    parts = [
        L.cyl("Volute", 0.3, 0.3, (0, 0, 0), "PumpBlue", rot=(0, 90, 0), verts=40, bevel=0.015),
        L.cyl("VoluteRim", 0.31, 0.04, (-0.13, 0, 0), "PumpBlue", rot=(0, 90, 0), verts=40, bevel=0.004),
        L.cyl("BackCover", 0.27, 0.05, (0.17, 0, 0), "PumpBlue", rot=(0, 90, 0), verts=40, bevel=0.006),
        L.cyl("SealHousing", 0.1, 0.12, (0.25, 0, 0), "Graphite", rot=(0, 90, 0), verts=24),
        L.cyl("SuctionNozzle", 0.1, 0.16, (-0.22, 0, 0), "PumpBlue", rot=(0, 90, 0), verts=24),
        L.cyl("SuctionFlange", 0.15, 0.035, (-0.3, 0, 0), "PumpBlue", rot=(0, 90, 0), verts=32),
        L.cyl("DischargeNozzle", 0.09, 0.2, (0, 0, 0.33), "PumpBlue", verts=24),
        L.cyl("DischargeFlange", 0.14, 0.035, (0, 0, 0.445), "PumpBlue", verts=32),
        L.cyl("DrainBoss", 0.035, 0.06, (0, -0.18, -0.22), "PumpBlue", rot=(90, 0, 0), verts=12),
    ]
    parts += L.ring_of("BoltS", 6, 0.125, -0.32, (0.02, 0.02, 0.02), "Steel", axis="X")
    parts += L.ring_of("BoltD", 6, 0.115, 0.465, (0.02, 0.02, 0.02), "Steel")
    parts += L.ring_of("BoltC", 8, 0.24, 0.2, (0.02, 0.02, 0.02), "Steel", axis="X")
    L.join("Casing", parts, root)


@asset
def Coupling():
    root = L.empty("Coupling")
    parts = [
        L.cyl("HubA", 0.085, 0.08, (-0.045, 0, 0), "Steel", rot=(0, 90, 0), verts=24),
        L.cyl("HubB", 0.085, 0.08, (0.045, 0, 0), "Steel", rot=(0, 90, 0), verts=24),
        L.cyl("Spider", 0.07, 0.012, (0, 0, 0), "Dark", rot=(0, 90, 0), verts=24),
        L.box("Marker", (0.16, 0.02, 0.03), (0, 0, 0.085), "Yellow", 0.003),
    ]
    L.join("CouplingMesh", parts, root)


# ------------------------------------------------------------------ Válvulas y manómetro

@asset
def BallValve():
    """Válvula esférica bridada. Eje de cañería en X; vástago hacia +Z (pivot de la palanca en z=0.16)."""
    root = L.empty("BallValve")
    parts = [
        L.sphere("Body", 0.1, (0, 0, 0), "Steel", scale=(1.25, 1, 1)),
        L.cyl("FlangeA", 0.13, 0.03, (-0.135, 0, 0), "Steel", rot=(0, 90, 0), verts=32),
        L.cyl("FlangeB", 0.13, 0.03, (0.135, 0, 0), "Steel", rot=(0, 90, 0), verts=32),
        L.cyl("Neck", 0.035, 0.07, (0, 0, 0.11), "Steel", verts=16),
        L.box("StopPlate", (0.06, 0.06, 0.012), (0, 0, 0.145), "Steel", 0.002),
    ]
    parts += L.ring_of("BoltA", 4, 0.1, -0.155, (0.02, 0.022, 0.022), "Graphite", axis="X", offset=math.pi / 4)
    parts += L.ring_of("BoltB", 4, 0.1, 0.155, (0.02, 0.022, 0.022), "Graphite", axis="X", offset=math.pi / 4)
    L.join("ValveBody", parts, root)


@asset
def ValveHandle():
    """Palanca de válvula; pivot en el origen, la palanca se extiende hacia +X (abierta = paralela a la cañería)."""
    root = L.empty("ValveHandle")
    parts = [
        L.cyl("Hub", 0.028, 0.03, (0, 0, 0.012), "Steel", verts=16),
        L.box("Lever", (0.26, 0.034, 0.012), (0.13, 0, 0.022), "Red", 0.003),
        L.cyl("Grip", 0.019, 0.1, (0.23, 0, 0.022), "Dark", rot=(0, 90, 0), verts=12),
        L.cyl("Nut", 0.014, 0.012, (0, 0, 0.032), "Steel", verts=6),
    ]
    L.join("Handle", parts, root)


@asset
def BleedValve():
    root = L.empty("BleedValve")
    parts = [
        L.box("Body", (0.07, 0.07, 0.07), (0, 0, 0), "Steel", 0.006),
        L.cyl("Nipple", 0.015, 0.09, (0, 0.045, 0.0), "Steel", rot=(90, 0, 0), verts=12),
        L.cyl("Spout", 0.012, 0.07, (0, 0, -0.06), "Steel", verts=12),
    ]
    L.join("BleedBody", parts, root)


@asset
def Gauge():
    """Manómetro con el dial mirando a -Y. El pivot de la aguja va en (0, -0.02, 0)."""
    root = L.empty("Gauge")
    parts = [
        L.cyl("Bezel", 0.09, 0.035, (0, 0, 0), "Steel", rot=(90, 0, 0), verts=40, bevel=0.004),
        L.cyl("Dial", 0.08, 0.004, (0, -0.017, 0), "White", rot=(90, 0, 0), verts=40, bevel=0),
        L.cyl("Socket", 0.016, 0.07, (0, 0.05, 0), "Steel", rot=(90, 0, 0), verts=12),
    ]
    for i in range(11):
        a = math.radians(-135 + i * 27)
        x, z = math.sin(a) * 0.066, math.cos(a) * 0.066
        material = "Red" if i >= 8 else "Dark"
        parts.append(L.box(f"Tick{i}", (0.004, 0.002, 0.014), (x, -0.0195, z), material, 0, rot=(0, math.degrees(a), 0)))
    L.join("GaugeBody", parts, root)


@asset
def GaugeNeedle():
    root = L.empty("GaugeNeedle")
    parts = [
        L.box("Needle", (0.006, 0.002, 0.062), (0, 0, 0.026), "Red", 0),
        L.cyl("Cap", 0.009, 0.006, (0, -0.002, 0), "Dark", rot=(90, 0, 0), verts=12, bevel=0),
    ]
    L.join("Needle", parts, root)


# ------------------------------------------------------------------ Eléctrico y mando

@asset
def ElectricalCabinet():
    root = L.empty("ElectricalCabinet")
    parts = [
        L.box("Cabinet", (1.8, 0.22, 1.5), (0, 0, 0), "Graphite", 0.012),
        L.box("Hood", (1.86, 0.28, 0.05), (0, -0.01, 0.775), "Graphite", 0.006),
        L.box("Plinth", (1.8, 0.2, 0.08), (0, 0, -0.79), "Dark", 0.004),
        L.box("Mounting", (1.6, 0.01, 0.7), (0, -0.111, 0.05), "Dark", 0.002),
        L.box("SeamV", (0.006, 0.006, 1.4), (0.0, -0.112, 0), "Dark", 0),
    ]
    for k in range(6):
        parts.append(L.box(f"Vent{k}", (0.22, 0.008, 0.012), (0.65, -0.112, -0.55 + k * 0.035), "Dark", 0))
    L.join("CabinetBody", parts, root)


@asset
def DisconnectSwitch():
    """Seccionador; el pivot de la palanca va en (0, -0.07, 0)."""
    root = L.empty("DisconnectSwitch")
    parts = [
        L.box("Enclosure", (0.3, 0.12, 0.42), (0, 0, 0), "Steel", 0.01),
        L.box("Window", (0.12, 0.005, 0.06), (0, -0.061, 0.15), "Dark", 0.002),
        L.cyl("BasePlate", 0.05, 0.012, (0, -0.064, 0), "Dark", rot=(90, 0, 0), verts=24),
        L.box("HaspA", (0.012, 0.03, 0.04), (0.09, -0.07, -0.13), "Steel", 0.002),
        L.box("HaspB", (0.012, 0.03, 0.04), (0.125, -0.07, -0.13), "Steel", 0.002),
    ]
    L.join("SwitchBody", parts, root)


@asset
def SwitchLever():
    """Palanca del seccionador: pivot en el origen, apunta a +Z (ON)."""
    root = L.empty("SwitchLever")
    parts = [
        L.cyl("Hub", 0.032, 0.03, (0, -0.012, 0), "Dark", rot=(90, 0, 0), verts=16),
        L.box("Arm", (0.04, 0.026, 0.19), (0, -0.02, 0.095), "Red", 0.005),
        L.sphere("Knob", 0.026, (0, -0.02, 0.2), "Red"),
    ]
    L.join("Lever", parts, root)


@asset
def ControlStation():
    """Botonera local sobre pedestal; origen en el piso. Botones en z=1.29, frente -Y."""
    root = L.empty("ControlStation")
    parts = [
        L.box("BasePlate", (0.3, 0.3, 0.02), (0, 0, 0.01), "Steel", 0.004),
        L.cyl("Post", 0.035, 1.12, (0, 0, 0.56), "Steel", verts=16),
        L.box("Enclosure", (0.34, 0.16, 0.36), (0, 0, 1.3), "Yellow", 0.012),
        L.box("Lid", (0.36, 0.17, 0.02), (0, 0, 1.49), "Yellow", 0.004),
        L.cyl("GuardStart", 0.042, 0.02, (-0.075, -0.085, 1.29), "Dark", rot=(90, 0, 0), verts=24),
        L.cyl("GuardStop", 0.042, 0.02, (0.075, -0.085, 1.29), "Dark", rot=(90, 0, 0), verts=24),
        L.box("LegendStart", (0.07, 0.004, 0.02), (-0.075, -0.082, 1.225), "White", 0),
        L.box("LegendStop", (0.07, 0.004, 0.02), (0.075, -0.082, 1.225), "White", 0),
        L.cyl("PilotRing", 0.03, 0.012, (0, -0.083, 1.43), "Steel", rot=(90, 0, 0), verts=20),
    ]
    L.join("StationBody", parts, root)


@asset
def PushButton():
    root = L.empty("PushButton")
    parts = [
        L.cyl("Stem", 0.026, 0.02, (0, -0.01, 0), "Steel", rot=(90, 0, 0), verts=20),
        L.cyl("Cap", 0.032, 0.018, (0, -0.028, 0), "Red", rot=(90, 0, 0), verts=24, bevel=0.006),
    ]
    L.join("Button", parts, root)


@asset
def PilotLight():
    root = L.empty("PilotLight")
    dome = L.half_sphere("Dome", 0.024, (0, 0, 0), "Pilot", scale=(1, 1, 0.8))
    dome.rotation_euler = (math.radians(90), 0, 0)
    L.set_parent(dome, root)


# ------------------------------------------------------------------ Dispositivos LOTO

@asset
def Padlock():
    """Candado LOTO; origen en el centro del cuerpo."""
    root = L.empty("Padlock")
    parts = [
        L.box("Body", (0.045, 0.022, 0.05), (0, 0, 0), "Red", 0.007),
        L.torus("Shackle", 0.015, 0.0035, (0, 0, 0.028), "Steel", rot=(90, 0, 0), major_seg=20, minor_seg=8),
        L.box("Keyhole", (0.006, 0.002, 0.012), (0, -0.0115, -0.008), "Dark", 0),
        L.box("Label", (0.03, 0.002, 0.012), (0, -0.0115, 0.012), "White", 0),
    ]
    L.join("PadlockMesh", parts, root)


@asset
def LotoTag():
    """Etiqueta de peligro; origen en el ojal (arriba)."""
    root = L.empty("LotoTag")
    parts = [
        L.box("Card", (0.07, 0.003, 0.12), (0, 0, -0.07), "White", 0.002),
        L.box("Header", (0.07, 0.0035, 0.03), (0, 0, -0.025), "Red", 0.001),
        L.torus("Grommet", 0.006, 0.002, (0, 0, -0.016), "Steel", rot=(90, 0, 0), major_seg=12, minor_seg=6),
        L.cyl("Tie", 0.0015, 0.03, (0, 0, -0.002), "Dark", verts=6, bevel=0),
    ]
    L.join("TagMesh", parts, root)


@asset
def ValveLockout():
    """Bloqueo de válvula tipo cuna que envuelve la palanca; origen en el centro."""
    root = L.empty("ValveLockout")
    parts = [
        L.box("ShellLeft", (0.14, 0.06, 0.1), (0, -0.035, 0), "Red", 0.012),
        L.box("ShellRight", (0.14, 0.06, 0.1), (0, 0.035, 0), "Red", 0.012),
        L.cyl("Hinge", 0.012, 0.15, (0, 0, 0.05), "Dark", rot=(0, 90, 0), verts=12),
        L.box("Tab", (0.03, 0.09, 0.04), (0.08, 0, -0.03), "Red", 0.004),
        L.box("Warning", (0.06, 0.002, 0.03), (0, -0.066, 0.01), "Yellow", 0),
    ]
    L.join("LockoutMesh", parts, root)


@asset
def LotoStationBoard():
    """Tablero de la estación de bloqueo; mira a -Y, origen en el centro."""
    root = L.empty("LotoStationBoard")
    parts = [
        L.box("Board", (1.3, 0.03, 1.1), (0, 0, 0), "Orange", 0.01),
        L.box("Header", (1.2, 0.005, 0.16), (0, -0.017, 0.44), "White", 0.002),
        L.box("HeaderBand", (1.2, 0.006, 0.03), (0, -0.018, 0.34), "Red", 0),
        L.box("Shelf", (1.2, 0.12, 0.02), (0, -0.06, -0.42), "Graphite", 0.004),
    ]
    for row, z in enumerate((0.2, -0.05)):
        for i in range(5):
            x = -0.48 + i * 0.24
            parts.append(L.cyl(f"Peg{row}{i}", 0.006, 0.06, (x, -0.04, z), "Steel", rot=(90, 0, 0), verts=8, bevel=0))
    L.join("BoardMesh", parts, root)


# ------------------------------------------------------------------ EPP

@asset
def Helmet():
    root = L.empty("Helmet")
    shell = L.half_sphere("Shell", 0.11, (0, 0, 0.0), "Yellow", scale=(1, 1.15, 0.85))
    brim = L.cyl("Brim", 0.125, 0.008, (0, -0.02, 0.004), "Yellow", verts=32, bevel=0.002)
    brim.scale = (1, 1.2, 1)
    L.join("HelmetMesh", [shell, brim], root)


@asset
def SafetyGlasses():
    root = L.empty("SafetyGlasses")
    parts = [
        L.box("LensL", (0.055, 0.005, 0.035), (-0.032, 0, 0), "Glass", 0.004),
        L.box("LensR", (0.055, 0.005, 0.035), (0.032, 0, 0), "Glass", 0.004),
        L.box("Frame", (0.13, 0.008, 0.008), (0, 0, 0.018), "Dark", 0.002),
        L.box("TempleL", (0.004, 0.13, 0.006), (-0.064, 0.065, 0.014), "Dark", 0.001),
        L.box("TempleR", (0.004, 0.13, 0.006), (0.064, 0.065, 0.014), "Dark", 0.001),
    ]
    L.join("GlassesMesh", parts, root)


@asset
def Gloves():
    """Par de guantes apoyados (material por defecto Orange: dieléctricos)."""
    root = L.empty("Gloves")
    parts = []
    for side, x in (("L", -0.055), ("R", 0.055)):
        parts.append(L.box(f"Palm{side}", (0.085, 0.13, 0.03), (x, 0, 0.015), "Orange", 0.012))
        parts.append(L.box(f"Cuff{side}", (0.095, 0.08, 0.035), (x, 0.1, 0.017), "Orange", 0.012))
        for f in range(4):
            fx = x - 0.03 + f * 0.02
            parts.append(L.cyl(f"Finger{side}{f}", 0.009, 0.06, (fx, -0.09, 0.012), "Orange", rot=(90, 0, 0), verts=8, bevel=0))
    L.join("GlovesMesh", parts, root)


@asset
def Cap():
    root = L.empty("Cap")
    crown = L.half_sphere("Crown", 0.09, (0, 0, 0), "Blue", scale=(1, 1.1, 0.75))
    visor = L.box("Visor", (0.12, 0.08, 0.008), (0, -0.12, 0.006), "Blue", 0.004)
    L.join("CapMesh", [crown, visor], root)


@asset
def Headphones():
    root = L.empty("Headphones")
    parts = [
        L.torus("Band", 0.075, 0.008, (0, 0, 0.07), "Dark", rot=(0, 90, 0), major_seg=24, minor_seg=6),
        L.cyl("CupL", 0.04, 0.03, (-0.075, 0, 0.0), "Red", rot=(0, 90, 0), verts=20, bevel=0.006),
        L.cyl("CupR", 0.04, 0.03, (0.075, 0, 0.0), "Red", rot=(0, 90, 0), verts=20, bevel=0.006),
    ]
    L.join("HeadphonesMesh", parts, root)


# ------------------------------------------------------------------ Mobiliario y ambientación

@asset
def Locker():
    """Locker doble; origen en el piso, frente -Y."""
    root = L.empty("Locker")
    parts = [L.box("Carcass", (0.7, 0.55, 1.9), (0, 0, 0.95), "Worker", 0.01),
             L.box("Plinth", (0.7, 0.5, 0.06), (0, 0.02, 0.03), "Dark", 0.004)]
    for side, x in (("L", -0.175), ("R", 0.175)):
        parts.append(L.box(f"Door{side}", (0.33, 0.012, 1.78), (x, -0.281, 0.98), "Worker", 0.004))
        parts.append(L.box(f"Handle{side}", (0.012, 0.02, 0.12), (x + (0.12 if side == "L" else -0.12), -0.295, 1.0), "Steel", 0.002))
        for k in range(5):
            parts.append(L.box(f"Vent{side}{k}", (0.16, 0.004, 0.012), (x, -0.288, 1.7 - k * 0.03), "Dark", 0))
    L.join("LockerMesh", parts, root)


@asset
def Workbench():
    """Mesa de trabajo 0.75 x 1.7, altura 0.9; origen en el piso."""
    root = L.empty("Workbench")
    parts = [L.box("Top", (0.75, 1.7, 0.05), (0, 0, 0.875), "Graphite", 0.008),
             L.box("Shelf", (0.68, 1.6, 0.025), (0, 0, 0.2), "Graphite", 0.004)]
    for i, x in enumerate((-0.33, 0.33)):
        for j, y in enumerate((-0.79, 0.79)):
            parts.append(L.box(f"Leg{i}{j}", (0.05, 0.05, 0.85), (x, y, 0.425), "Steel", 0.004))
    L.join("BenchMesh", parts, root)


@asset
def Desk():
    root = L.empty("Desk")
    parts = [L.box("Top", (0.7, 1.4, 0.04), (0, 0, 0.76), "Graphite", 0.006),
             L.box("Drawer", (0.6, 0.45, 0.6), (0, 0.42, 0.42), "Graphite", 0.006),
             L.box("LegA", (0.6, 0.04, 0.74), (0, -0.66, 0.37), "Steel", 0.004)]
    L.join("DeskMesh", parts, root)


@asset
def Monitor():
    root = L.empty("Monitor")
    parts = [L.box("Screen", (0.55, 0.03, 0.34), (0, 0, 0.3), "Dark", 0.008),
             L.box("Display", (0.51, 0.004, 0.3), (0, -0.016, 0.3), "Blue", 0),
             L.box("Neck", (0.04, 0.03, 0.14), (0, 0.03, 0.1), "Graphite", 0.004),
             L.box("Foot", (0.2, 0.16, 0.012), (0, 0.03, 0.006), "Graphite", 0.004)]
    L.join("MonitorMesh", parts, root)


@asset
def Radio():
    root = L.empty("Radio")
    parts = [L.box("Body", (0.06, 0.035, 0.13), (0, 0, 0.065), "Dark", 0.008),
             L.cyl("Antenna", 0.006, 0.09, (0.018, 0, 0.17), "Dark", verts=8),
             L.box("Screen", (0.035, 0.002, 0.025), (0, -0.018, 0.1), "Green", 0)]
    L.join("RadioMesh", parts, root)


@asset
def Operator():
    """Maniquí estilizado de operador (casco, chaleco); origen en el piso, mira a -Y."""
    root = L.empty("Operator")
    parts = [
        L.cyl("LegL", 0.07, 0.84, (-0.09, 0, 0.42), "Dark", verts=12),
        L.cyl("LegR", 0.07, 0.84, (0.09, 0, 0.42), "Dark", verts=12),
        L.box("BootL", (0.1, 0.2, 0.08), (-0.09, -0.03, 0.04), "Dark", 0.02),
        L.box("BootR", (0.1, 0.2, 0.08), (0.09, -0.03, 0.04), "Dark", 0.02),
        L.box("Torso", (0.36, 0.2, 0.58), (0, 0, 1.13), "Worker", 0.06),
        L.box("Vest", (0.38, 0.22, 0.42), (0, 0, 1.18), "Orange", 0.05),
        L.box("StripeA", (0.385, 0.225, 0.025), (0, 0, 1.08), "Steel", 0.005),
        L.box("StripeB", (0.385, 0.225, 0.025), (0, 0, 1.2), "Steel", 0.005),
        L.cyl("ArmL", 0.05, 0.6, (-0.23, 0, 1.12), "Worker", verts=10),
        L.cyl("ArmR", 0.05, 0.6, (0.23, 0, 1.12), "Worker", verts=10),
        L.sphere("HandL", 0.05, (-0.23, 0, 0.8), "Skin"),
        L.sphere("HandR", 0.05, (0.23, 0, 0.8), "Skin"),
        L.cyl("Neck", 0.05, 0.08, (0, 0, 1.45), "Skin", verts=10),
        L.sphere("Head", 0.11, (0, 0, 1.58), "Skin", scale=(0.9, 1, 1.1)),
    ]
    helmet = L.half_sphere("HelmetShell", 0.12, (0, 0, 1.63), "Yellow", scale=(1, 1.1, 0.8))
    brim = L.cyl("HelmetBrim", 0.13, 0.008, (0, -0.02, 1.632), "Yellow", verts=24)
    L.join("OperatorMesh", parts + [helmet, brim], root)


@asset
def CeilingLight():
    root = L.empty("CeilingLight")
    parts = [L.box("Housing", (1.25, 0.28, 0.07), (0, 0, 0), "Steel", 0.008),
             L.box("Diffuser", (1.15, 0.2, 0.01), (0, 0, -0.037), "Light", 0)]
    L.join("LightMesh", parts, root)


@asset
def CableTray():
    """Bandeja portacables tipo escalera de 3 m a lo largo de X."""
    root = L.empty("CableTray")
    parts = [L.box("RailA", (3.0, 0.012, 0.08), (0, -0.15, 0), "Steel", 0.002),
             L.box("RailB", (3.0, 0.012, 0.08), (0, 0.15, 0), "Steel", 0.002)]
    for k in range(10):
        parts.append(L.box(f"Rung{k}", (0.03, 0.3, 0.012), (-1.35 + k * 0.3, 0, -0.03), "Steel", 0.002))
    parts.append(L.box("Cables", (3.0, 0.2, 0.04), (0, 0, -0.004), "Dark", 0.01))
    L.join("TrayMesh", parts, root)


@asset
def Extinguisher():
    root = L.empty("Extinguisher")
    parts = [L.cyl("Body", 0.08, 0.5, (0, 0, 0.3), "Red", verts=20, bevel=0.03),
             L.cyl("Valve", 0.025, 0.08, (0, 0, 0.59), "Dark", verts=10),
             L.box("Handle", (0.12, 0.02, 0.015), (0.03, 0, 0.64), "Dark", 0.003),
             L.box("Label", (0.1, 0.003, 0.14), (0, -0.081, 0.3), "White", 0)]
    L.join("ExtinguisherMesh", parts, root)


@asset
def Bucket():
    root = L.empty("Bucket")
    parts = [L.cone("Pail", 0.11, 0.13, 0.26, (0, 0, 0.13), "Steel", verts=24),
             L.torus("Handle", 0.13, 0.004, (0, 0, 0.26), "Dark", rot=(90, 0, 0), major_seg=24, minor_seg=6)]
    L.join("BucketMesh", parts, root)


@asset
def Wrench():
    root = L.empty("Wrench")
    parts = [L.box("Shaft", (0.24, 0.025, 0.01), (0, 0, 0.005), "Steel", 0.003),
             L.box("Jaw", (0.05, 0.06, 0.012), (0.13, 0, 0.006), "Steel", 0.004),
             L.cyl("Ring", 0.022, 0.012, (-0.13, 0, 0.006), "Steel", verts=16)]
    L.join("WrenchMesh", parts, root)


@asset
def Screwdriver():
    root = L.empty("Screwdriver")
    parts = [L.cyl("Grip", 0.016, 0.1, (-0.05, 0, 0.016), "Red", rot=(0, 90, 0), verts=12, bevel=0.004),
             L.cyl("Shaft", 0.004, 0.12, (0.06, 0, 0.016), "Steel", rot=(0, 90, 0), verts=8, bevel=0)]
    L.join("ScrewdriverMesh", parts, root)


@asset
def Flange():
    """Brida de unión para cañerías (eje Z), con pernos."""
    root = L.empty("Flange")
    parts = [L.cyl("Disc", 0.13, 0.05, (0, 0, 0), "PipeGreen", verts=28)]
    parts += L.ring_of("Bolt", 6, 0.105, 0, (0.02, 0.02, 0.07), "Steel")
    L.join("FlangeMesh", parts, root)


def main():
    os.makedirs(OUT, exist_ok=True)
    names = [n for n in BUILDERS if not ONLY or n in ONLY]
    for name in names:
        L.reset()
        BUILDERS[name]()
        L.export(OUT, name)
    print(f"[done] {len(names)} assets -> {OUT}")


main()
