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
- `SignalInteractable`: interacción configurable que levanta un *flag* y/o emite una *signal* y muestra un aviso (EPP, rótulos de identificación, herramientas).
- `LotoKit`: inventario de dispositivos LOTO (candado, etiqueta, bloqueo de válvula) + ítem seleccionado ("en mano") + llave. Desktop: barra inferior y teclas 1–3; VR: cinturón/caja.
- `LotoStation`: entrega el kit y lo recibe al final (sólo con todos los dispositivos de vuelta y tras el mantenimiento).
- `DialogueDefinition` (SO) + `DialogueSession` + `DialogueSource`: el operador y la orden de trabajo abren diálogos según el paso actual (provisto por `DialogueSource.CurrentStepIdProvider`, para que Interaction no dependa de Procedure).
- **Regla:** ningún Device ni el Procedure lee `Mouse`/`Keyboard`. Sólo los `Desktop*` lo hacen, vía Input System actions.

### Devices (mundo)
- `Device` (base): `DeviceId` (ej. `DS-101`), estado, publica `DeviceStateChanged`.
- `ToggleDevice` → `DisconnectSwitch` (ON/OFF) y `Valve` (abierta/cerrada; succión, descarga y purga usan la misma clase). `PressureGauge` (lee `PumpSystem`), `ControlStation` + `ControlButton` (START/STOP), `PumpCasing` (dispara el mantenimiento).
- `LotoSocket` (en el mismo GameObject del `Device`): apila bloqueo de válvula → candado → etiqueta; se retiran en orden inverso y el candado requiere la llave. El `Device` decide con un solo click si **coloca** (hay ítem en mano y se puede), **retira** (mano vacía y hay dispositivos) u **opera**.
- `PumpModel` (C# puro, testeado) + `PumpSystem` (MonoBehaviour): energizado = DS-101 en ON; en marcha = energizado y orden de marcha; 4 bar en marcha, 2.5 bar de línea con alguna válvula abierta, presión atrapada con ambas cerradas hasta purgar. Recalcula en `LateUpdate` para que los handlers vean el estado previo a la acción.

### Procedure
- `ProcedureDefinition` (SO): lista ordenada de `StepDefinition`.
- `StepDefinition` (SO): id (`S0`…`S7`), título, descripción (guiado), lista de `TaskDefinition`.
- `TaskDefinition` (clase serializable **dentro** del `StepDefinition`, no un asset aparte): id, texto, lista de `TaskCondition` (todas deben cumplirse) e ids a resaltar. `StepDefinition.Ordered` obliga a completar las tareas en orden.
- `TaskCondition` (struct plano editable en el Inspector): `DeviceState`, `SocketHas`, `SocketEmpty`, `PumpRunning/Stopped/Depressurized`, `Flag` (hito persistente, `Signals.RaiseFlag`) y `Signal` (acción momentánea, `Signals.Emit`: sólo cuenta durante el paso en curso y, en pasos ordenados, después de la tarea anterior).
- `ProcedureProgress` (C# puro, testeado contra un `IWorldState` falso) + `ProcedureRunner` (MonoBehaviour que implementa `IWorldState` y publica `StepStarted`, `TaskCompleted`, `ProcedureCompleted`).
- `HazardRule` (SO, **M2**): condición de peligro + severidad + mensaje (ej. "purga abierta con V-101 abierta").
- Las condiciones referencian dispositivos por **`DeviceId` string**, no por referencia de escena (los SO no pueden apuntar a objetos de escena). `DeviceRegistry` resuelve el id.

### Evaluation
- `AssessmentTracker`: acumula `MistakeRecord {stepId, severity, ruleId, message, time}`, tiempo y orden. Calcula puntaje (reglas en PROCEDURE.md).
- `SessionReport`: DTO inmutable para la pantalla de reporte.

### Guidance
- `GuidanceController`: en modo guiado, resalta objetivos de la tarea actual (`Highlighter` con outline), activa ghosts de sockets y muestra avisos de error. En modo evaluación sólo reproduce sonido de error.

### Player
- `FirstPersonMotor` (CharacterController, WASD, Shift para caminar rápido, sin salto).
- `StationTeleport`: lista de `StationPoint` (Locker, Estación LOTO, Tablero, Bomba, Operador); menú `T` para saltar con fade.
- `DesktopLook` + `CursorControl`: mouse look con pointer lock (WebGL requiere click para capturar el cursor). Si el navegador no permite el lock (iframes restringidos, vistas embebidas), pasa a un modo de respaldo: la cámara sigue al mouse sin capturarlo y se puede mover e interactuar igual.

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

## Construcción de contenido por código
- `LotoSim/Datos/Regenerar procedimiento P-101` (`ProcedureDataBuilder`): genera pasos y diálogos desde el guion. Editar ahí y regenerar, o ajustar los assets a mano.
- `LotoSim/Greybox/Construir PumpRoom` (`GreyboxSceneBuilder`): reconstruye la escena completa (sala, equipos, jugador, HUD) de forma idempotente. Cuando lleguen los modelos de Blender se reemplazan los hijos visuales y se mantienen los componentes.

## Testing
- Edit Mode tests (Unity Test Framework) para `ProcedureRunner`, condiciones, `HazardRule`s y `AssessmentTracker` usando un `PumpSystem` falso.
- Play Mode: `ProcedureWalkthroughTests` recorre los 8 pasos invocando `Interact()` sobre los objetos reales de la escena (valida escena + datos + lógica).
