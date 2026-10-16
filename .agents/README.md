# .agents — Documentación para agentes y colaboradores

Esta carpeta es la **fuente de verdad** del proyecto *Simulador LOTO* (bloqueo y etiquetado de energías peligrosas). Cualquier agente (Claude, Copilot, etc.) o persona que trabaje en el repo debe leerla antes de tocar código o assets.

## Orden de lectura

| # | Archivo | Para qué sirve |
|---|---------|----------------|
| 1 | [BRIEF.md](BRIEF.md) | Visión, público, alcance de la v1 y lo que **no** entra. |
| 2 | [DECISIONS.md](DECISIONS.md) | Registro de decisiones tomadas con la dueña del proyecto (no re-discutir sin motivo). |
| 3 | [PROCEDURE.md](PROCEDURE.md) | Guion completo del procedimiento LOTO de la demo, paso a paso, con acciones, errores y feedback. |
| 4 | [ARCHITECTURE.md](ARCHITECTURE.md) | Sistemas de runtime, flujo de datos, preparación para VR. |
| 5 | [STRUCTURE.md](STRUCTURE.md) | Estructura de carpetas, escenas, assemblies, naming de assets. |
| 6 | [CODESTYLE.md](CODESTYLE.md) | Convenciones de C#, Unity y commits. |
| 7 | [DESIGN.md](DESIGN.md) | Dirección de arte, UI/UX, controles, feedback, audio. |
| 8 | [ASSETS.md](ASSETS.md) | Lista de modelos/sonidos necesarios, pipeline Blender → Unity, licencias. |
| 9 | [WORKFLOW.md](WORKFLOW.md) | Git/LFS, uso de Unity MCP y Blender, build WebGL e itch.io. |
| 10 | [ROADMAP.md](ROADMAP.md) | Hitos, tareas y estado. **Actualizar al terminar cada tarea.** |
| 11 | [REFERENCES.md](REFERENCES.md) | Fuentes OSHA, videos, productos LOTO, referencias visuales. |

## Reglas rápidas para agentes

1. **Funcionalidad primero, pulido después.** Greybox jugable antes que arte final.
2. Mantener la **lógica independiente del input** (mouse/teclado hoy, VR mañana). Ver ARCHITECTURE.md §Interacción.
3. Todo texto visible al usuario en **español**; identificadores de código en **inglés**.
4. Commits **directos a `main`**, chicos y descriptivos (ver CODESTYLE.md §Commits). Pushear al terminar cada tarea.
5. Después de cambiar scripts: revisar la consola de Unity (MCP) y que no haya errores ni warnings nuevos.
6. Si una tarea cambia una decisión, actualizar `DECISIONS.md`; si cambia el plan, actualizar `ROADMAP.md`.
7. Cuidar tokens y tiempo: no generar assets ni subagentes innecesarios; preguntar ante ambigüedad real.
