# ROADMAP — Simulador LOTO

Plazo objetivo del MVP: **1–2 semanas** desde 2026-10-16. Principio: *funcionalidad primero, pulido por capas*.
Marcar `[x]` al completar. Cada hito termina con una build WebGL probada.

## M0 — Setup del proyecto (día 1)
- [x] Definir scope y documentación `.agents/`
- [ ] Quitar paquetes Visual Scripting y Multiplayer Center
- [ ] Borrar contenido de plantilla (`TutorialInfo/`, `Readme.asset`, `SampleScene`)
- [ ] Verificar `.gitattributes` LFS (fbx, blend, png, wav, ttf…) y `.gitignore` (`Builds/`)
- [ ] Crear estructura de carpetas `Assets/_Project` y `Blender/` (STRUCTURE.md)
- [ ] Assembly definitions Runtime / Editor / Tests
- [ ] `LotoSim.inputactions` con mapas *Player* y *UI* (DESIGN.md §Controles)
- [ ] Escenas `Menu` y `PumpRoom` en Build Settings; plataforma Web; settings WebGL base
- [ ] Localizar Blender y anotar ruta en WORKFLOW.md
- [ ] Build WebGL vacía de prueba

## M1 — Greybox jugable (días 2–4) ⭐ prioridad
- [ ] Sala greybox con primitivas (paredes, bomba, tablero, botonera, estación LOTO, locker, operador)
- [ ] `FirstPersonMotor` + `DesktopLook` con pointer lock + pausa al perder foco
- [ ] Núcleo: `SimEvents`, `Services`, `GameFlow`, `SessionContext`
- [ ] Interacción: `IInteractor`, `DesktopInteractor`, `IInteractable`, prompt HUD, rango "Acercate"
- [ ] Devices: `PumpSystem`, `DisconnectSwitch`, `LeverValve`, `BleedValve`, `PressureGauge`, `ControlStation`, `DeviceRegistry`
- [ ] LOTO: `Pickable`, `LotoKit` (slots + llave), `LotoSocket` (apilado bloqueo→candado→etiqueta), `IsolationPoint`
- [ ] Procedure: SOs `ProcedureDefinition`/`StepDefinition`/`TaskDefinition`/condiciones + `ProcedureRunner`
- [ ] Datos: `PROC_LOTO_P101` con los 8 pasos de PROCEDURE.md
- [ ] HUD texto plano: paso actual, tareas, kit
- [ ] Edit Mode tests del runner y del `PumpSystem`
- [ ] Recorrido completo jugable de punta a punta (modo guiado sin resaltados aún)

## M2 — Loop completo de capacitación (días 5–6)
- [ ] `HazardRule`s y `AssessmentTracker` (severidades, puntaje, orden, tiempo)
- [ ] `GuidanceController`: outline, marcadores, ghosts, toasts de error
- [ ] Modo Evaluación (sin guía)
- [ ] EPP: locker con distractores
- [ ] Operador NPC + radio + `DialogueView` con 3 opciones
- [ ] Try-out y transición "mantenimiento realizado" (fade + herramientas en el piso)
- [ ] Paso 7 completo (retiro de bloqueos con llave, restablecer, arranque)
- [ ] Menú, Briefing, Pausa/Opciones, Reporte (funcionales, estilo básico)
- [ ] `StationTeleport` (T) con fade
- [ ] Play Mode smoke test del procedimiento completo
- [ ] Build WebGL M2 en itch (privada)

## M3 — Arte y audio v1 (días 7–9)
- [ ] Atlas de paleta + material `M_Palette`; shaders de outline/ghost
- [ ] Modelos Blender prioridad A (bomba, válvulas, manómetro, seccionador, botonera, candado, etiqueta, bloqueo, llave)
- [ ] Modelos prioridad B (estación LOTO, locker + EPP, tablero, kit de cañerías)
- [ ] Sala modular, ambientación y señalética; NPC CC0
- [ ] Iluminación horneada + post-procesado leve
- [ ] Tablet diegética (pestañas Pasos / Tarjeta LOTO / OT)
- [ ] UI estilizada (Inter, paneles, íconos) según DESIGN.md
- [ ] SFX + ambiente + mixer; registrar licencias en ASSETS.md

## M4 — Pulido, rendimiento y entrega (días 10–12)
- [ ] Perf en notebook con GPU integrada: ≥30 FPS, build < 50 MB, carga < 20 s
- [ ] WebGL template propio + página de itch (capturas, descripción)
- [ ] QA: recorrer ambos modos, todos los errores, reintentar, volver al menú
- [ ] Revisión de textos (ortografía, tono)
- [ ] Créditos
- [ ] Build final publicada

## Backlog v2+
- Port VR: XR Interaction Toolkit + OpenXR, `XRInteractorAdapter`, tablet en muñeca, teleport.
- Multímetro / detector de tensión en la verificación.
- Pinza multi-candado (hasp) y caja de bloqueo grupal (varios trabajadores).
- Quiz en momentos clave.
- Narración/TTS; localización (inglés/portugués).
- Exportar reporte (PDF/JSON), integración LMS (SCORM/xAPI).
- Más escenarios: cinta transportadora, equipo de pozo (Oil & Gas), tablero eléctrico.
- Mantenimiento jugable (cambio de sello real).
- Consecuencias simuladas más dramáticas (opcional).
