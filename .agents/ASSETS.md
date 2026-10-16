# ASSETS — Lista, pipeline y licencias

## Política
1. **Greybox primero:** todo objeto interactivo existe como primitivas de Unity con el pivot y collider correctos antes de modelarse.
2. Objetos **interactivos y protagonistas** → modelados a medida en **Blender** (vía scripts Python headless cuando sea posible).
3. **Ambientación, SFX, UI e íconos** → assets gratuitos permitidos (Kenney, Poly Haven, Quaternius, Freesound CC0, Google Fonts/OFL). Preferir **CC0**.
4. Toda pieza externa se registra en la tabla de licencias de abajo.

## Modelos a medida (Blender)
| Prioridad | Asset | Notas de diseño |
|-----------|-------|-----------------|
| A | `SM_PumpCentrifugal` | Carcasa voluta + motor + acople con guarda + base. Separar carcasa/tapa para el "cambio de sello". |
| A | `SM_LeverValve` | Válvula esférica bridada; palanca como objeto aparte con pivot en el eje; agujero para el bloqueo. |
| A | `SM_BleedValve` | Válvula chica de purga con manija mariposa. |
| A | `SM_PressureGauge` | Caja, dial (textura simple) y aguja separada (pivot en el centro). |
| A | `SM_DisconnectSwitch` | Caja de seccionador con palanca roja + orejas para candado (posición OFF). |
| A | `SM_ControlStation` | Pedestal con botón START verde, STOP rojo, luz piloto. |
| A | `SM_Padlock` | Candado LOTO rojo, arco metálico separado (abierto/cerrado). Referencia: IFAM / Brady. |
| A | `SM_LotoTag` | Etiqueta rectangular con ojal; la cara frontal es un quad con texto TMP en Unity. |
| A | `SM_ValveLockout` | Bloqueo de válvula de palanca (tipo "cuna" que envuelve la palanca). |
| A | `SM_Key` | Llave del candado. |
| B | `SM_LotoStation` | Tablero naranja con perchas. |
| B | `SM_Locker`, `SM_Helmet`, `SM_SafetyGlasses`, `SM_DielectricGloves` (+ distractores: gorra, guantes de tela, auriculares) | |
| B | `SM_ElectricalPanel` | Tablero con 3 seccionadores, rótulos. |
| B | `SM_Pipe_*` | Kit modular: recto 1 m, codo 90°, T, brida, soporte. |
| C | Sala: paredes, piso técnico, techo, luminarias, puerta | Modular 2 m. |
| C | `SM_Radio`, `SM_Bucket`, herramientas (`SM_Wrench`, `SM_Screwdriver`) | |
| C | NPC operador | Personaje low-poly estilizado (Quaternius/Kenney CC0) con idle; o maniquí estilizado propio. |

## Reglas de exportación Blender → Unity
- Unidades métricas, escala 1.0; **1 BU = 1 m**.
- Pivots en el punto lógico de interacción (eje de la palanca, centro de la aguja, ojal del candado).
- Aplicar transforms antes de exportar. Export FBX: *Apply Scalings: FBX All*, *Forward: -Z*, *Up: Y*, *Apply Transform* ✓, sólo mesh (sin cámaras/luces), normales con *Smoothing: Face*.
- Nombres de objetos dentro del FBX en inglés (`Lever`, `Needle`, `Shackle`) para buscarlos desde scripts/prefabs.
- UVs: proyectar a celdas del atlas de paleta (un solo material `M_Palette` para casi todo). UV2 para lightmaps: dejar que Unity genere (*Generate Lightmap UVs*) salvo problemas.
- Presupuesto: props chicos < 1.5k tris; bomba completa < 15k tris; escena total visible < 300k tris.
- Script de export reutilizable en `Blender/scripts/export_fbx.py`; cada asset tiene su script `Blender/scripts/build_<asset>.py` cuando se genera proceduralmente.
- Ejecución: `blender -b -P Blender/scripts/build_padlock.py -- --out Assets/_Project/Art/Models/SM_Padlock.fbx`.

## Audio necesario
| Clip | Fuente sugerida |
|------|-----------------|
| AMB_RoomHVAC, AMB_MotorHum | Freesound CC0 / Sonniss |
| SFX_SwitchLever, SFX_ValveTurn, SFX_BleedWater, SFX_WaterSpray | Freesound CC0 |
| SFX_PadlockClose/Open, SFX_TagAttach, SFX_LockoutAttach | Kenney Impact / Freesound |
| SFX_ButtonPress, SFX_TryoutClick, SFX_Spark, SFX_RadioSquelch | Kenney / Freesound |
| UI_Hover, UI_Click, UI_Ok, UI_Error, UI_StepComplete | Kenney Interface Sounds / UI Audio |

Formato en repo: `.wav` 44.1 kHz; import en Unity: SFX cortos *Decompress On Load*, ambientes *Streaming* (en WebGL todo se decodifica; mantener clips cortos y mono para 3D).

## Fuentes e íconos
- Fuente: **Inter** (SIL OFL) → TMP SDF con rango Latin + español (á é í ó ú ñ ¿ ¡ ü).
- Íconos: Kenney Game Icons / UI Pack (CC0) o set propio lineal.

## Registro de licencias
| Asset | Autor / Fuente | Licencia | URL | Usado en |
|-------|----------------|----------|-----|----------|
| _(vacío)_ | | | | |

Mantener este registro actualizado; se publica como *Créditos* en el menú.
