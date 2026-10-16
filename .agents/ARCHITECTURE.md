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
- `MistakeCatalog` (SO, `Data/Mistakes/MISTAKES_P101`): id, severidad, título y explicación de cada error. Si un error tiene `TriggerSignal`, se detecta solo al emitirse esa señal (EPP equivocado, rótulo equivocado, mensaje incorrecto al operador).
- `HazardMonitor`: reglas por código que dependen del estado (corte bajo carga, válvula cerrada en marcha, purga presurizada, trabajo sin verificar, bloqueo retirado antes de tiempo, orden de válvulas, etiqueta sin candado, restablecimiento sin avisar, etc.). Corre en los handlers de eventos, antes del `LateUpdate` de la bomba.
- `AssessmentTracker`: registra cada tipo de error **una sola vez** (`MistakeRecord`), publica `MistakeMade` y, al completar el procedimiento, arma el `SessionReport` y publica `ReportReady`.
- `SessionReport` (C# puro, testeado): puntaje 100 − 5 por menor − 20 por crítico (mín. 0); aprobado con ≥ 70 y sin críticos.

### Guidance
- `HighlightTarget`: objeto resaltable; sin ids configurados su id es el nombre del GameObject (`DS-101`, `PPE-HELMET`…). Resalta con un pulso de `_EmissionColor` vía `MaterialPropertyBlock` (los materiales greybox tienen emisión habilitada en negro).
- `GuidanceController` (sólo modo guiado): resalta los `HighlightIds` de las tareas pendientes (en pasos ordenados, sólo la siguiente) con pulso + marcador flotante; muestra fantasmas (`LotoSocket.ShowGhost`) donde se puede colocar el ítem en mano; convierte `MistakeMade` en avisos con explicación. En modo evaluación no muestra nada de esto.

### Player
- `FirstPersonMotor` (CharacterController, WASD o flechas, Shift para caminar rápido, sin salto). Lee `Player/Move`; si llega en cero consulta `Keyboard.current` directamente (`UsedKeyboardFallback`).
- `StationTeleport`: 5 estaciones (Locker, Estación LOTO, Tablero, Bomba, Operador); `T` abre el mapa y 1–5 saltan con fundido (`ScreenFade`). Mientras el mapa está abierto `OverlayMenu.IsOpen` bloquea el interactor.
- `DesktopLook` + `CursorControl`: mouse look con pointer lock (WebGL requiere click para capturar el cursor). Si el navegador no permite el lock (iframes restringidos, vistas embebidas), pasa a un modo de respaldo: la cámara sigue al mouse sin capturarlo y se puede mover e interactuar igual.

### UI
- `HudView` (screen-space): paso actual y tareas, crosshair, prompt, barra del kit, avisos apilados (hasta 3), viñeta de error, diálogo, mapa de estaciones, pausa con opciones.
- `BriefingView` (estado `Briefing` al entrar), `ReportView` (escucha `ReportReady`), `FadeView` (implementa `ScreenFade`), `MenuController` (escena Menu).
- Regla: un componente que escucha eventos **no** puede vivir en el panel que él mismo desactiva (vive en el canvas).
- `TabletView` (world-space, `Tab`): **M3**.
- Las vistas **sólo leen** estado y escuchan eventos; las acciones pasan por métodos de servicios.
- `InputDebugOverlay` (F3): estado del cursor, de las acciones y del teclado (teclas presionadas, entrada aplicada por el motor, posición). Primera herramienta ante "no me responde el teclado".

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
