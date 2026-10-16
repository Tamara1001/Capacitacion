# STRUCTURE — Organización del proyecto

## Repo (raíz)
```
Capacitacion/
├─ .agents/                 Documentación para agentes (este directorio)
├─ Assets/
│  ├─ _Project/             TODO lo propio va acá
│  ├─ Settings/             URP assets y Volume profiles (de la plantilla; se dejan en su lugar)
│  └─ ThirdParty/           Assets externos sin modificar (Kenney, fuentes, etc.)
├─ Blender/                 Fuentes .blend y scripts de generación (LFS) — fuera de Assets
│  ├─ scripts/              *.py ejecutables con `blender -b -P`
│  └─ *.blend
├─ Builds/                  Salida de builds (ignorado por git)
├─ Packages/  ProjectSettings/
├─ CLAUDE.md                Puntero a .agents/ para agentes
└─ .gitattributes / .gitignore
```

## Assets/_Project
```
_Project/
├─ Art/
│  ├─ Models/        FBX exportados desde Blender (SM_*.fbx)
│  ├─ Materials/     M_*.mat
│  ├─ Textures/      T_*.png (incluye T_PaletteAtlas)
│  └─ Shaders/       Shader Graphs (outline, ghost, highlight pulse)
├─ Audio/
│  ├─ SFX/  Ambience/  UI/
│  ├─ Mixer/         LotoSim.mixer
│  └─ Cues/          AC_*.asset (AudioCue)
├─ Data/
│  ├─ Procedures/    PROC_LOTO_P101.asset
│  ├─ Steps/         STEP_S0_EPP.asset … STEP_S7_Restore.asset
│  ├─ Tasks/         TASK_S3_CloseV102.asset …
│  ├─ Hazards/       HZ_BleedBeforeIsolate.asset …
│  └─ Items/         ITEM_Padlock.asset, ITEM_Tag.asset …
├─ Input/            LotoSim.inputactions (asignado como acciones project-wide)
├─ Prefabs/
│  ├─ Devices/       PF_DisconnectSwitch, PF_LeverValve, PF_BleedValve, PF_Gauge, PF_ControlStation
│  ├─ Loto/          PF_Padlock, PF_Tag, PF_ValveLockout, PF_LotoStation
│  ├─ Environment/   PF_Wall_*, PF_Pipe_*, PF_Locker …
│  ├─ Player/        PF_PlayerDesktop
│  └─ UI/            PF_Tablet, PF_HUD, PF_Toast …
├─ Scenes/
│  ├─ Boot.unity     Bootstrap de servicios (opcional, puede fusionarse con Menu)
│  ├─ Menu.unity
│  └─ PumpRoom.unity
├─ Scripts/
│  ├─ Runtime/       LotoSim.Runtime.asmdef
│  │  ├─ Core/ Interaction/ Devices/ Procedure/ Evaluation/ Guidance/ Player/ UI/ Audio/
│  ├─ Editor/        LotoSim.Editor.asmdef (validadores, menús de utilidad)
│  └─ Tests/
│     ├─ EditMode/   LotoSim.Tests.EditMode.asmdef
│     └─ PlayMode/   LotoSim.Tests.PlayMode.asmdef
└─ UI/
   ├─ Fonts/         TMP font assets
   └─ Sprites/       Íconos (EPP, candado, etiqueta…)
```

## Prefijos de assets
| Tipo | Prefijo | Ejemplo |
|------|---------|---------|
| Static mesh (FBX) | `SM_` | `SM_PumpCentrifugal.fbx` |
| Material | `M_` | `M_Metal_Blue.mat` |
| Textura | `T_` | `T_PaletteAtlas.png` |
| Prefab | `PF_` | `PF_LeverValve.prefab` |
| Shader Graph | `SG_` | `SG_GhostOutline.shadergraph` |
| AudioClip | `SFX_` / `AMB_` / `UI_` | `SFX_PadlockClose.wav` |
| AudioCue (SO) | `AC_` | `AC_ValveTurn.asset` |
| ScriptableObject de datos | `PROC_`, `STEP_`, `TASK_`, `HZ_`, `ITEM_` | `HZ_ClosedValvesWhileRunning.asset` |
| Sprite UI | `ICO_` | `ICO_Padlock.png` |
| Animation clip | `AN_` | `AN_LeverDown.anim` |
| Animator Controller | `ANC_` | `ANC_Operator.controller` |

## Escenas
- **Menu**: cámara cinemática sobre la sala (o fondo estático), menú principal, selección de modo, opciones.
- **PumpRoom**: Briefing (overlay), juego, pausa y reporte (overlays). Jerarquía raíz:
  ```
  --- SYSTEMS ---     (Bootstrap, GameFlow, ProcedureRunner, Assessment, Guidance, Audio)
  --- PLAYER ---
  --- ENVIRONMENT --- (Static, marcar Contribute GI)
  --- DEVICES ---     (P-101, DS-101, V-101…, con DeviceId)
  --- LOTO ---        (estación, sockets)
  --- NPC ---
  --- LIGHTING ---
  --- UI ---
  ```
- Build Settings: `Menu` (0), `PumpRoom` (1).

## Datos fuera de Assets
- `Blender/` contiene fuentes; nunca importar `.blend` directo en Unity (exportar FBX).
- Material de referencia (videos, PDFs) **no** se commitea; se linkea en REFERENCES.md.
