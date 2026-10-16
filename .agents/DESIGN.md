# DESIGN — Arte, UX, controles y audio

## Dirección de arte
**Estilizado limpio, industrial y legible** — como el video de referencia de VR (tablero gris oscuro, gabinete naranja, piso técnico claro, contornos amarillos). Debe verse *profesional*, no caricaturesco.

- Geometría low/mid-poly con **biseles** en bordes visibles (lee bien con luz).
- **Colores planos** desde un atlas de paleta (`T_PaletteAtlas`, 256×256 con celdas de color) + 2–3 materiales especiales (metal, vidrio del manómetro, emisivo de luces). Sin texturas fotográficas.
- Iluminación **horneada** (lightmaps) + 1 luz realtime principal; Ambient Occlusion horneado. Interior frío y bien iluminado (luminarias LED de techo).
- Post-procesado mínimo (Tonemapping Neutral/ACES suave, Bloom leve para emisivos). Nada de DOF ni motion blur.

### Paleta
| Uso | Color | Hex |
|-----|-------|-----|
| Piso técnico / paredes | Gris claro | `#D9DCE0` / `#C9CDD2` |
| Tableros, estructuras | Gris grafito | `#2E3238` |
| Bomba y motor | Azul industrial | `#2F5D8C` |
| Cañerías agua de proceso | Verde (código cañerías) | `#3E8E5A` |
| Estación LOTO / gabinete | Naranja seguridad | `#F28C1B` |
| Candados / etiquetas LOTO | Rojo seguridad | `#D3272E` + blanco |
| **Guía** (resaltados, ghosts) | Amarillo/ámbar | `#FFC629` |
| Correcto / completado | Verde | `#3BB273` |
| Error crítico | Rojo | `#E5484D` |
| Info / UI acento | Azul | `#2D7FF9` |
| Fondo UI | Grafito translúcido | `#1C1F24` @ 92% |

Regla: **amarillo = guía**, **rojo = LOTO/peligro**, **verde = OK**. No usar amarillo en objetos del entorno que no sean guía (salvo señalética fija con franjas).

## Ambiente: Sala de bombas
Interior ~12 × 9 m, techo 4 m. Contenido:
- **Bomba P-101** sobre base/skid con motor, acople con guarda, carcasa, cañería de succión (izq.) y descarga (der., hacia arriba) con V-101/V-102, purga V-103 bajo carcasa con balde, manómetro PI-101.
- **Tablero eléctrico** en pared con seccionador DS-101 (palanca roja, con orejas para candado) y otros 2 seccionadores de otros equipos (no interactivos, para que el usuario *identifique* el correcto por rótulo).
- **Botonera local CS-101** en pedestal junto a la bomba (START verde, STOP rojo, luz piloto).
- **Estación LOTO** (tablero naranja en pared) con candados, etiquetas, bloqueos de válvula y cartel con la tarjeta de LOTO.
- **Locker EPP** junto a la entrada.
- **Puesto del operador** (escritorio + monitor + radio) con el NPC.
- Ambientación: otra bomba fuera de servicio, cañerías al techo, bandejas portacables, extintor, señalética de seguridad (EPP obligatorio, riesgo eléctrico), rejillas, luces.

## Cámara y movimiento (desktop)
- Primera persona, altura de ojos 1.65 m, FOV 70, velocidad 2.2 m/s (Shift 3.5 m/s). Sin salto, sin agacharse en v1.
- Interacción a distancia ≤ 2.0 m. Más lejos: prompt gris "Acercate" (como el "Get closer" del video).
- Salto a estaciones con fade 0.25 s.

## Controles
| Acción | Teclado/Mouse | VR (futuro) |
|--------|---------------|-------------|
| Moverse | WASD | Stick izq. / teleport |
| Mirar | Mouse | Cabeza |
| Interactuar / colocar | Click izq. | Gatillo / grip |
| Seleccionar ítem del kit | 1–4 o rueda | Tomar del cinturón |
| Guardar ítem en mano | Click der. / R | Soltar en el cinturón |
| Tablet | Tab (1–3 cambia de pestaña) | Mirar muñeca |
| Diagnóstico de entrada | F3 | — |
| Mapa de estaciones | T | Teleport |
| Caminar rápido | Shift | — |
| Pausa | Esc / P (WebGL libera el cursor con Esc) | Menú |

Nota WebGL: Esc siempre libera el pointer lock del navegador; tratar la pérdida de foco/pointer lock como **pausa automática** y pedir "Click para continuar".

## UI / UX
- **Fuente:** Inter (OFL) — Semibold para títulos, Regular para cuerpo. Tamaño mínimo 18 px a 1080p.
- Paneles: fondo grafito translúcido, radio 12 px, padding 16/24, sombra suave. Íconos lineales (Kenney/Lucide estilo).
- **HUD:** crosshair puntito central; prompt de interacción debajo del crosshair ("Click · Cerrar válvula de descarga V-102"); paso actual arriba a la izquierda (título + n/7); barra del kit abajo centro; toasts arriba centro (verde OK, rojo error, ámbar aviso).
- **Tablet:** pestañas *Pasos* (checklist con tildes), *Tarjeta LOTO* (tabla de puntos de energía y su estado en vivo), *Orden de trabajo*.
- **Menú:** título del simulador, botones *Iniciar (Guiado)*, *Iniciar (Evaluación)*, *Opciones*, *Créditos*. Fondo: vista de la sala.
- **Briefing:** 3 tarjetas: ¿Qué es LOTO? · Tu tarea (OT-0427) · Controles. Botón "Comenzar".
- **Reporte:** estado Aprobado/No aprobado, puntaje, tiempo, timeline de los 8 pasos con check/cruz, lista de errores con explicación y referencia al paso; botones *Reintentar* y *Menú*.
- **Pausa/Opciones:** sensibilidad del mouse, invertir Y, volumen (Master/SFX/Ambiente), calidad (Baja/Alta), reiniciar, salir al menú.

### Feedback de interacción (guiado)
- Hover sobre interactuable: outline blanco fino.
- Objetivo de la tarea actual: outline ámbar pulsante + marcador flotante (chevron) si está fuera de vista.
- Socket de colocación: **ghost** semitransparente ámbar del objeto esperado (como en el video).
- Tarea completada: tilde verde + sonido "ok" corto; paso completado: toast + sonido más largo.
- Error: toast rojo con explicación de 1 línea + sonido de alerta; crítico además con viñeta roja breve en pantalla.

## Audio
- **Ambiente:** zumbido de sala (HVAC) en loop; zumbido del motor 3D mientras la bomba corre (se apaga con fade al detener).
- **SFX:** palanca de seccionador (clack pesado), válvula girando (metálico), candado cerrando/abriendo, etiqueta (plástico/papel), purga de agua, chorro de agua (error), chispa (error), botón START/STOP, clic seco del try-out, radio (squelch), pasos suaves (opcional).
- **UI:** hover, click, ok, error, paso completado, apertura de tablet.
- Fuentes: Kenney (UI/Impact/Interface sounds), Freesound CC0, Sonniss GDC bundle si aplica. Ver ASSETS.md.

## Accesibilidad
- Todo feedback crítico es **visual y sonoro**.
- No depender sólo del color: íconos ✓/✕/! acompañan verde/rojo/ámbar.
- Sensibilidad de mouse ajustable; sin cambios bruscos de cámara.
