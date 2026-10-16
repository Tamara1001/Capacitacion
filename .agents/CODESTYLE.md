# CODESTYLE — Convenciones

## Idioma
- Identificadores (clases, métodos, variables, archivos de código): **inglés**.
- Comentarios y docs: **español**, breves; explicar el *por qué*, no el *qué*.
- Texto visible al usuario: **español**, centralizado en assets de datos (SO) o en constantes de UI, nunca disperso en lógica.

## C#
- Namespace por módulo: `LotoSim.Core`, `LotoSim.Interaction`, `LotoSim.Devices`, `LotoSim.Procedure`, `LotoSim.Evaluation`, `LotoSim.Guidance`, `LotoSim.Player`, `LotoSim.UI`, `LotoSim.Audio`.
- **Una clase pública por archivo**, nombre de archivo = nombre de clase. Excepción: los structs de eventos y los enums chicos de un módulo van juntos (`DeviceEvents.cs`, `SimEnums.cs`).
- Naming:
  - `PascalCase`: tipos, métodos, propiedades, eventos, constantes.
  - `_camelCase`: campos privados. `camelCase`: parámetros y locales.
  - Interfaces con `I` (`IInteractable`). Booleanos como pregunta (`IsLocked`, `HasTag`, `CanInteract`).
  - Eventos de `SimEvents` como hechos en pasado: `DeviceStateChanged`, `ItemPlaced`, `MistakeMade`.
- Campos serializados: `[SerializeField] private` + propiedad de sólo lectura si hace falta. **Nunca** campos públicos mutables.
- Agrupar miembros: campos serializados → campos privados → propiedades → eventos → métodos Unity (`Awake`, `OnEnable`, `Start`, `Update`, `OnDisable`) → métodos públicos → privados.
- Suscribirse en `OnEnable`, desuscribirse en `OnDisable`. Siempre.
- Evitar: `FindObjectOfType`/`GameObject.Find` en runtime, `SendMessage`, `Resources.Load` (salvo Bootstrap), LINQ en `Update`, `new` de colecciones por frame, corrutinas anidadas complejas (preferir `Awaitable` de Unity 6 o corrutinas simples).
- Nada de `Update()` para polling de estado del procedimiento: todo por eventos.
- `null` checks: usar `TryGetComponent`. Validar referencias en `OnValidate` y loguear con contexto (`Debug.LogError(msg, this)`).
- `readonly struct` para eventos; `enum` con valor explícito si se serializa.
- Atributos útiles: `[RequireComponent]`, `[DisallowMultipleComponent]`, `[Tooltip]` en todos los campos que diseña alguien.
- Sin `#region`. Métodos cortos (< ~40 líneas).
- Logs: prefijo `[Módulo]`, ej. `Debug.Log("[Procedure] Paso S3 iniciado")`. Logs de debug detrás de `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

### Ejemplo
```csharp
namespace LotoSim.Devices
{
    /// <summary>Válvula esférica de palanca (abierta/cerrada) bloqueable.</summary>
    [DisallowMultipleComponent]
    public sealed class LeverValve : Device, IInteractable
    {
        [SerializeField, Tooltip("Pivot de la palanca que rota 90°")]
        private Transform _lever;

        private bool _isOpen = true;

        public bool IsOpen => _isOpen;

        public bool CanInteract(IInteractor interactor) => !IsLocked;

        public void Interact(IInteractor interactor)
        {
            _isOpen = !_isOpen;
            AnimateLever(_isOpen);
            PublishStateChanged(_isOpen ? DeviceState.Open : DeviceState.Closed);
        }
        // ...
    }
}
```

## Unity
- Assembly Definitions según STRUCTURE.md; Runtime no referencia Editor ni Tests.
- Prefabs para todo lo que se repite; escenas con la jerarquía de separadores de STRUCTURE.md.
- Datos de diseño en ScriptableObjects (`[CreateAssetMenu(menuName = "LotoSim/...")]`).
- Referencias entre SO y escena por **id string** (`DeviceId`), resueltas por `DeviceRegistry`.
- Unidades: 1 unidad = 1 metro. Y arriba.
- Layers: `Interactable`, `Player`, `Ghost` (no colisiona). Tags al mínimo.
- Input: siempre vía `InputActionReference` / clase generada de `LotoSim.inputactions`. Nunca `Input.GetKey` (Input Manager legado desactivado).
- UI: TextMeshPro siempre; anchors correctos; probar en 16:9 y 16:10 (y 1280×720 mínimo).

## Tests
- Edit Mode para lógica pura (procedimiento, reglas, puntaje). Nombre: `Metodo_Escenario_Resultado`.
- Antes de commitear lógica de procedimiento, correr los tests (Unity MCP `run_tests`).

## Commits
- Directo a `main`. Commits **chicos y atómicos**, compilables.
- Formato (Conventional Commits, en español):
  ```
  tipo(ámbito): resumen en imperativo, minúscula, sin punto

  Cuerpo opcional: por qué y qué cambia.
  ```
  Tipos: `feat`, `fix`, `refactor`, `art`, `audio`, `ui`, `docs`, `build`, `chore`, `test`, `perf`.
  Ej.: `feat(procedure): agregar reglas de peligro del paso 5`.
- Siempre incluir los `.meta` de los assets nuevos/movidos. Nunca commitear `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`.
- No mezclar cambios propios con cambios ajenos sin revisar (`git status` antes de `git add`; preferir `git add <rutas>`).
