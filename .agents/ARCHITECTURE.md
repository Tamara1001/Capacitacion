# ARCHITECTURE — Sistemas de runtime

Objetivo: lógica de capacitación **desacoplada** del input y de la presentación, para que el port a VR sólo cambie la capa de interacción/locomoción y la UI de pantalla.

```
        Input (Desktop hoy / XR mañana)
                    │
     ┌──────────────▼──────────────┐
     │  Player: Locomotion +        │   IInteractor (DesktopInteractor / XRInteractor)
     │  Interactor + HandSlot/Kit   │
     └──────────────┬──────────────┘
                    │ Interact / Place / Take
     ┌──────────────▼──────────────┐
     │  World: Devices &            │   DisconnectSwitch, LeverValve, BleedValve,
     │  IsolationPoints & Sockets   │   PressureGauge, ControlStation, LotoSocket…
     └──────────────┬──────────────┘
                    │ SimEvent (DeviceStateChanged, ItemPlaced, …)
     ┌──────────────▼──────────────┐
     │  Procedure Runner            │◄── ProcedureDefinition (ScriptableObject)
     │  + Rules / Hazard checks     │
     └──────┬───────────────┬──────┘
            │               │
   ┌────────▼───┐   ┌───────▼────────┐
   │ Evaluation │   │ Guidance        │  (highlights, ghosts, avisos)
   └────────┬───┘   └───────┬────────┘
            └──────► UI (Tablet, HUD, Report) / Audio
```

## Módulos (namespaces `LotoSim.*`)

### Core
- `GameFlow`: máquina de estados `Boot → Menu → Briefing → Playing ⇄ Paused → Report`. Maneja carga de escenas.
- `SimEvents`: bus de eventos tipado y estático (`SimEvents.Publish<T>(T evt)` / `Subscribe<T>`). Eventos son `readonly struct`. Sin dependencias a UnityEngine más allá de lo mínimo.
- `SessionContext`: modo (`Guided`/`Assessment`), nombre del usuario, hora de inicio. Vive mientras dura la partida.
- `Services`: registro simple de servicios (`Services.Get<IAudioService>()`), poblado por un `Bootstrap` en la escena. **No** usar singletons sueltos.

### Interaction (clave para VR)
- `IInteractor`: quién interactúa. Expone `HeldItem`, `Hover`, eventos. Implementaciones: `DesktopInteractor` (raycast desde el centro de la cámara) ahora; `XRInteractorAdapter` (XR Interaction Toolkit) en v2.
- `IInteractable`: `CanInteract(IInteractor)`, `Interact(IInteractor)`, `GetPrompt()` → texto "Click: Cerrar válvula".
- `IHoverable`: para outline y prompt.
- `Pickable`: objeto que se puede tomar (candado, etiqueta, herramienta). En desktop va a `HandSlot`; en VR lo agarra la mano.
- `LotoSocket`: punto donde se coloca un dispositivo (acepta `ItemType`s, orden de apilado: bloqueo de válvula → candado → etiqueta). Muestra contorno fantasma en modo guiado.
- `LotoKit`: inventario de dispositivos LOTO (slots 1–4) + llave. Desktop: barra inferior; VR: cinturón/caja.
- **Regla:** ningún Device ni el Procedure lee `Mouse`/`Keyboard`. Sólo los `Desktop*` lo hacen, vía Input System actions.

### Devices (mundo)
- `Device` (base): `DeviceId` (ej. `DS-101`), estado, publica `DeviceStateChanged`.
- `DisconnectSwitch` (ON/OFF, bloqueable), `LeverValve` (abierta/cerrada, animación de palanca 90°), `BleedValve` (abierta/cerrada), `PressureGauge` (lee `PumpSystem`), `ControlStation` (START/STOP), `PumpSystem` (simula: energizado, en marcha, presión, flujo).
- `IsolationPoint`: une un `Device` con sus `LotoSocket`s; expone `IsIsolated`, `IsLocked`, `IsTagged`.
- `PumpSystem` es la única "física" del sistema: calcula `IsRunning = energized && startPressed && !locked`, presión según válvulas y purga. Determinístico, sin Rigidbody.

### Procedure
- `ProcedureDefinition` (SO): lista ordenada de `StepDefinition`.
- `StepDefinition` (SO): id (`S0`…`S7`), título, descripción (guiado), lista de `TaskDefinition`.
- `TaskDefinition` (SO): id, texto, `Condition` a cumplir (ej. `DeviceStateCondition{DS-101 = Off}`, `ItemPlacedCondition{socket, itemType}`, `DialogueChoiceCondition`), objetivos a resaltar (por `DeviceId`).
- `HazardRule` (SO): condición de peligro + severidad + mensaje (ej. "purga abierta con V-101 abierta").
- `ProcedureRunner`: escucha `SimEvents`, evalúa tareas del paso actual y reglas de peligro, avanza pasos, publica `TaskCompleted`, `StepStarted`, `MistakeMade`.
- Las condiciones referencian dispositivos por **`DeviceId` string**, no por referencia de escena (los SO no pueden apuntar a objetos de escena). `DeviceRegistry` resuelve el id.

### Evaluation
- `AssessmentTracker`: acumula `MistakeRecord {stepId, severity, ruleId, message, time}`, tiempo y orden. Calcula puntaje (reglas en PROCEDURE.md).
- `SessionReport`: DTO inmutable para la pantalla de reporte.

### Guidance
- `GuidanceController`: en modo guiado, resalta objetivos de la tarea actual (`Highlighter` con outline), activa ghosts de sockets y muestra avisos de error. En modo evaluación sólo reproduce sonido de error.

### Player
- `FirstPersonMotor` (CharacterController, WASD, Shift para caminar rápido, sin salto).
- `StationTeleport`: lista de `StationPoint` (Locker, Estación LOTO, Tablero, Bomba, Operador); menú `T` para saltar con fade.
- `DesktopLook`: mouse look con pointer lock (WebGL requiere click para capturar el cursor).

### UI
- `TabletView` (world-space uGUI, sostenida/abierta con `Tab`): checklist de pasos, tarjeta de LOTO, OT.
- `HudView` (screen-space): crosshair, prompt de interacción, paso actual compacto, barra del kit, toasts.
- `MenuView`, `BriefingView`, `PauseView`, `ReportView`, `DialogueView` (opciones del operador).
- Las vistas **sólo leen** estado y escuchan eventos; las acciones pasan por métodos de servicios.

### Audio
- `AudioService` con `AudioCue` (SO: clips, volumen, pitch random, 2D/3D). Bus: Master/SFX/Ambiente/UI en un AudioMixer.

## Flujo de una acción (ejemplo)
1. `DesktopInteractor` hace raycast → `LeverValve V-102` es `IHoverable` → HUD muestra "Click: Cerrar válvula de descarga".
2. Click → `LeverValve.Interact()` anima la palanca y publica `DeviceStateChanged{V-102, Closed}`.
3. `PumpSystem` recalcula; `ProcedureRunner` evalúa: tarea "Cerrar V-102" ✓, reglas de peligro (¿bomba en marcha? → `MistakeMade`).
4. `GuidanceController` mueve el resaltado a V-101; `TabletView` tilda la tarea; `AudioService` reproduce el clic.

## Preparación para VR (v2)
- Instalar XR Interaction Toolkit + OpenXR; crear `XRInteractorAdapter` que implementa `IInteractor` y mapea `Pickable`/`LotoSocket` a `XRGrabInteractable`/`XRSocketInteractor`.
- `TabletView` se ancla a la muñeca; `HudView` se desactiva.
- `StationTeleport` → teleport anchors de XRI; `FirstPersonMotor` → continuous move provider.
- El Procedure, Devices, Evaluation y Guidance **no cambian**.

## Testing
- Edit Mode tests (Unity Test Framework) para `ProcedureRunner`, condiciones, `HazardRule`s y `AssessmentTracker` usando un `PumpSystem` falso.
- Play Mode smoke test: recorrer el procedimiento completo invocando `Interact()` por código.
