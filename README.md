# Simulador LOTO

Demo de un simulador de capacitación laboral en 3D: el usuario ejecuta el procedimiento **LOTO (Lockout/Tagout, bloqueo y etiquetado de energías peligrosas)** sobre una bomba centrífuga con motor eléctrico antes de hacer un cambio de sello mecánico.

Es una demo propia, pensada como base para producir simuladores de capacitación similares para empresas. Corre en el navegador (WebGL) y está diseñada desde el principio para portarse a VR.

## Qué se practica
Un recorrido de 5 a 10 minutos por la secuencia basada en OSHA 29 CFR 1910.147, simplificada pero correcta:

preparar y elegir el EPP → comunicar y detener el equipo → aislar las fuentes de energía → bloquear y etiquetar → liberar la energía almacenada → verificar el aislamiento → trabajar → retirar los bloqueos y restablecer.

- **Tres tipos de energía:** eléctrica (seccionador), hidráulica (válvulas de succión y descarga) y presión residual (purga y manómetro).
- **Dispositivos LOTO:** candado personal con llave, etiqueta con datos y bloqueo de válvula.
- **Modo guiado:** resalta cada paso y explica los errores.
- **Modo evaluación:** sin ayudas. Los errores se informan al final, con tiempo, orden de pasos y puntaje.
- Un catálogo de 19 errores con distintas severidades, un NPC operador y un reporte final.

## Controles
| Acción | Teclado y mouse |
|---|---|
| Moverse / mirar | WASD / mouse |
| Interactuar | Click izquierdo |
| Candado, etiqueta, bloqueo de válvula | 1, 2, 3 |
| Guardar ítem en mano | R |
| Mapa de estaciones | T |
| Tablet con la tarjeta LOTO | Tab |
| Pausa | Esc |

La lógica es independiente del dispositivo de entrada, para sumar VR (OpenXR) más adelante.

## Tecnología
- Unity 6 (6000.3), URP, Input System, uGUI + TextMeshPro.
- Modelos propios hechos en Blender (`Blender/`), con estética industrial de color plano.
- Procedimientos definidos como datos (ScriptableObjects), separados de la lógica de simulación.
- Tests de Edit Mode del ejecutor de procedimientos y del modelo de la bomba.
- Build WebGL de unos 12 MB, con plantilla propia.

## Documentación
La carpeta [`.agents/`](.agents/README.md) es la fuente de verdad del proyecto:
[brief](.agents/BRIEF.md), [procedimiento](.agents/PROCEDURE.md), [arquitectura](.agents/ARCHITECTURE.md), [diseño](.agents/DESIGN.md), [estructura](.agents/STRUCTURE.md), [assets](.agents/ASSETS.md), [publicación](.agents/PUBLISHING.md) y [roadmap](.agents/ROADMAP.md).

## Probarlo en el editor
1. Abrir el proyecto con Unity 6000.3 (ver `ProjectSettings/ProjectVersion.txt`). El repo usa Git LFS para modelos, texturas y audio, así que conviene tener `git lfs` instalado antes de clonar.
2. Abrir la escena `Assets/_Project/Scenes/Menu.unity` y darle Play.
3. Para la build web: menú **LotoSim > Build > WebGL**. Los detalles están en [`.agents/PUBLISHING.md`](.agents/PUBLISHING.md).

## Autoría y licencias
Creado por Tamara D'Angelo ([@Tamara1001](https://github.com/Tamara1001)).

- El **código** está bajo licencia MIT. Ver [`LICENSE`](LICENSE).
- Los **modelos, texturas, audio y fuentes** tienen el origen y la licencia registrados en [`.agents/ASSETS.md`](.agents/ASSETS.md). La licencia MIT no los cubre si el registro indica otra cosa.
- Es material de capacitación de demostración: no reemplaza una formación certificada en seguridad.
