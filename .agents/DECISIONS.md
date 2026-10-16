# DECISIONS — Registro de decisiones

Decisiones acordadas con la dueña del proyecto (sesión de definición, 2026-10-16). Para cambiar una, agregar una entrada nueva abajo con fecha y motivo; no borrar las anteriores.

## Producto
| Tema | Decisión | Nota |
|------|----------|------|
| Equipo a intervenir | Bomba centrífuga + motor eléctrico (P-101) | Clásico de LOTO, cubre 3 tipos de energía. |
| Tarea que motiva el LOTO | Cambio de sello mecánico | Requiere abrir carcasa → justifica aislar todo. |
| Energías v1 | Eléctrica, hidráulica, presión residual | Decidido por el agente (la dueña delegó). |
| Alcance | 7 pasos OSHA simplificados (+ EPP) | 1–2 acciones físicas por paso. |
| Duración partida | 5–10 min | |
| Público | Clientes / empresas | Prioridad: fluidez y primera impresión. |
| Plazo | 1–2 semanas para el MVP | Priorizar jugable rápido. |
| Comunicar (paso 2) | NPC operador estático + radio | Elegir el mensaje correcto. |
| EPP | Sí, simple: locker con distractores | |
| Candado/etiqueta v1 | Candado personal con llave + etiqueta con datos | Hasp y caja grupal → v2. Decidido por el agente. |
| Guía | Modo Guiado + modo Evaluación | Construir primero Guiado. |
| Errores | Se permiten y se penalizan | Aviso + registro en reporte. |
| Evaluación | Orden de pasos, prueba de arranque, tiempo | Sin quiz en v1. |
| Resultados | Solo en pantalla | Sin persistencia. |
| Idioma / marca | Español, sin marca | Nombre genérico del simulador. |

## Experiencia
| Tema | Decisión |
|------|----------|
| Movimiento | WASD + mouse libre **y** salto rápido a estaciones |
| Interacción | Inventario (kit LOTO) + click en zona con contorno fantasma |
| UI de pasos | Tablet diegética en el mundo + HUD mínimo en pantalla |
| Pantallas | Menú, Briefing, Pausa/Opciones, Reporte final |
| Audio | SFX + ambiente, sin voz |
| Estilo | Estilizado limpio tipo video de referencia (low/mid-poly, colores planos) |
| Ambiente | Sala de bombas industrial interior |
| Escenas | Menú + 1 escena de sala |

## Técnica
| Tema | Decisión |
|------|----------|
| UI | uGUI + TextMeshPro |
| Port VR | Capa de input abstracta (`IInteractor`); sin XR instalado aún |
| Datos del procedimiento | ScriptableObjects |
| Deploy | itch.io (WebGL) |
| Hardware objetivo | Notebook de oficina, GPU integrada, ≥30 FPS, build < 50 MB |
| Assets 3D | Blender a medida + primitivas; assets gratuitos (Kenney, Poly Haven, etc.) permitidos, sobre todo para SFX, UI y ambientación |
| Git | Commits directos a `main` |
| Git LFS | Sí, para binarios |
| Paquetes a quitar | Visual Scripting, Multiplayer Center, contenido TutorialInfo/Readme |
