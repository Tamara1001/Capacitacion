# ROADMAP — Simulador LOTO

Plazo objetivo del MVP: **1–2 semanas** desde 2026-10-16. Principio: *funcionalidad primero, pulido por capas*.
Marcar `[x]` al completar. Cada hito termina con una build WebGL probada.

## M0 — Setup del proyecto (día 1)
- [x] Definir scope y documentación `.agents/`
- [x] Quitar paquetes Visual Scripting y Multiplayer Center
- [x] Borrar contenido de plantilla (`TutorialInfo/`, `Readme.asset`, `SampleScene`)
- [x] Verificar `.gitattributes` LFS (fbx, blend, png, wav, ttf…) y `.gitignore` (`Builds/`)
- [x] Crear estructura de carpetas `Assets/_Project` y `Blender/` (STRUCTURE.md)
- [x] Assembly definitions Runtime / Editor / Tests
- [x] `LotoSim.inputactions` con mapas *Player* y *UI* (DESIGN.md §Controles)
- [x] Escenas `Menu` y `PumpRoom` en Build Settings; plataforma Web; settings WebGL base
- [x] Localizar Blender y anotar ruta en WORKFLOW.md
- [x] Build WebGL vacía de prueba (11 MB, OK)

## M1 — Greybox jugable (días 2–4) ⭐ prioridad
- [x] Sala greybox con primitivas (paredes, bomba, tablero, botonera, estación LOTO, locker, operador)
- [x] `FirstPersonMotor` + `DesktopLook` con pointer lock + pausa al perder foco
- [x] Núcleo: `SimEvents`, `Services`, `GameFlow`, `SessionContext`
- [x] Interacción: `IInteractor`, `DesktopInteractor`, `IInteractable`, prompt HUD, rango "Acercate"
- [x] Devices: `PumpSystem` (+`PumpModel` puro), `DisconnectSwitch`, `Valve` (succión, descarga y purga), `PressureGauge`, `ControlStation`/`ControlButton`, `PumpCasing`, `DeviceRegistry`
- [x] LOTO: `LotoKit` (slots + llave), `LotoSocket` (apilado bloqueo→candado→etiqueta), `LotoStation`; EPP, rótulos y herramientas con `SignalInteractable`; operador y OT con `DialogueSource`
- [x] Procedure: SOs `ProcedureDefinition`/`StepDefinition`/`TaskDefinition`/condiciones + `ProcedureRunner`
- [x] Datos: `PROC_LOTO_P101` con los 8 pasos de PROCEDURE.md
- [x] HUD texto plano: paso actual, tareas, kit
- [x] Edit Mode tests del runner y del `PumpSystem`
- [x] Recorrido completo jugable de punta a punta (modo guiado sin resaltados aún)

## M2 — Loop completo de capacitación (días 5–6)
- [x] `MistakeCatalog` (19 errores) + `HazardMonitor` + `AssessmentTracker` (severidades, puntaje, tiempo; un registro por tipo de error)
- [x] `GuidanceController`: pulso de emisión + marcador flotante, fantasmas de dispositivos LOTO, explicación de errores (outline real queda para M3)
- [x] Modo Evaluación (sin guía, sin descripción ni tareas; errores sólo en el reporte)
- [x] EPP: locker con distractores
- [x] Operador NPC + diálogo con 3 opciones (radio: backlog)
- [x] Try-out y "mantenimiento realizado" con fundido y herramientas en el piso
- [x] Paso 7 completo (retiro de bloqueos con llave, restablecer, arranque)
- [x] Menú (nombre, modos, opciones, créditos), Briefing, Pausa con opciones (sensibilidad, invertir Y; guardadas en PlayerPrefs), Reporte
- [x] `Menu` como escena 0 del build (`LotoSim/Greybox/Construir todo`)
- [x] `StationTeleport` (T + 1–5) con fundido
- [x] Play Mode smoke test del procedimiento completo
- [x] Build WebGL M2 local
- [ ] Subir a itch.io (privada) — requiere cuenta/butler de la dueña del proyecto

## M3 — Arte y audio v1 (días 7–9)
- [x] **Bug WASD — causa encontrada**: el usuario juega por streaming desde el celular y el teclado virtual manda *caracteres* (eventos de texto), no teclas físicas: el Input System nunca ve la W "presionada". `TextKeyBridge` convierte w/a/s/d tipeadas en pasos cortos (0,4 s) y 1-5/t/r/p/Tab/Esc en atajos. Antes de dar con esto: no se podía reproducir con teclado real (funciona en el Editor y en Chrome). Se endureció igual: el movimiento cae al teclado directo (WASD/flechas) si la acción `Move` llega en cero, el Editor enruta el teclado al juego aunque la Game view no tenga foco (`LotoSim.inputsettings`) y F3 muestra un overlay de diagnóstico. Si el usuario lo sigue viendo, pedir una captura del F3.
- [ ] Atlas de paleta + material `M_Palette` (hoy: materiales planos `M_GB_*`); [x] shader de outline (`LotoSim/Outline`, hover blanco + objetivo ámbar); ghost sigue como material transparente
- [x] Modelos Blender prioridad A (bomba, válvulas, manómetro, seccionador, botonera, candado, etiqueta, bloqueo) — falta `SM_Key`
- [ ] Modelos prioridad B: [x] estación LOTO, locker + EPP, tablero; falta kit de cañerías (`SM_Pipe_*`) y `SM_ElectricalPanel` como modelo propio
- [x] Sala, ambientación y señalética; [x] operador (`SM_Operator`, propio en lugar de NPC CC0)
- [ ] Iluminación horneada (hoy realtime) + [x] post-procesado leve (`PP_PumpRoom`)
- [x] Tablet diegética (pestañas Pasos / Tarjeta LOTO / OT)
- [ ] UI estilizada: [x] fuente Barlow (reemplaza Inter), paneles; faltan íconos; evitar que el toast se superponga al panel de pasos en 16:10
- [x] SFX + ambiente + `SND_Library` + volumen maestro; [x] licencias registradas en ASSETS.md; [ ] mixer

## M4 — Pulido, rendimiento y entrega (días 10–12)
- [x] Build web liviana: **12 MB** (antes 13), 1 aviso (antes 354) tras quitar `com.unity.ai.inference` (Sentis, sin dependientes)
- [x] Control de calidad **Alta/Baja** (sombras, post-procesado y FXAA) en Pausa y Opciones, con auto-detección de FPS bajos (< 24) en la build web
- [ ] Medir en una notebook de oficina real (GPU integrada): ≥ 30 FPS, carga < 20 s. Sin hardware a mano no se pudo medir; el auto-ajuste cubre el caso
- [x] Template WebGL propio (`Assets/WebGLTemplates/LotoSim`): responsive 16:9, pantalla de carga, errores sin `alert`, aviso en táctiles
- [x] Zip para itch.io generado por `LotoSim ▸ Build ▸ WebGL` y guía en [PUBLISHING.md](PUBLISHING.md)
- [ ] Subir la demo a itch.io (la hace la dueña del proyecto) y probar ya publicada
- [x] QA automatizado: 16 Edit Mode + 8 Play Mode (recorrido completo sin errores = 100 pts, errores críticos, movimiento por teclas físicas, flechas y teclados de sólo texto)
- [x] QA visual en 16:9, 16:10 y 4:3 (Chrome real); revisión de textos (tildes y voseo)
- [x] Créditos (Barlow OFL, Kenney CC0)
- [ ] Pasada final de calidad visual con la dueña del proyecto (ver backlog de arte abajo)

## Backlog v2+
- Arte: iluminación horneada, atlas de paleta + `M_Palette`, `SM_Key`, kit de cañerías `SM_Pipe_*`, mixer de audio, íconos de UI.
- Port VR: XR Interaction Toolkit + OpenXR, `XRInteractorAdapter`, tablet en muñeca, teleport.
- Multímetro / detector de tensión en la verificación.
- Pinza multi-candado (hasp) y caja de bloqueo grupal (varios trabajadores).
- Quiz en momentos clave.
- Narración/TTS; localización (inglés/portugués).
- Exportar reporte (PDF/JSON), integración LMS (SCORM/xAPI).
- Más escenarios: cinta transportadora, equipo de pozo (Oil & Gas), tablero eléctrico.
- Mantenimiento jugable (cambio de sello real).
- Consecuencias simuladas más dramáticas (opcional).
