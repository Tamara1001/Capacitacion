# PUBLISHING — Build y publicación de la demo web

## Generar la build
Desde Unity: menú **LotoSim ▸ Build ▸ WebGL** (≈ 3 minutos). Deja:
- `Builds/WebGL/` — la carpeta del juego (index.html, Build/…).
- `Builds/SimuladorLOTO-WebGL.zip` — el mismo contenido listo para itch.io (el `index.html` queda en la raíz del zip).
- `Logs/build-results.txt` — resultado, tamaño y duración.

Ambas rutas están en `.gitignore` (no se versionan).

Estado de referencia (2026-10-16): **12 MB**, 0 errores, 1 aviso. Template propio `Assets/WebGLTemplates/LotoSim` (canvas 16:9 responsive, pantalla de carga, errores sin `alert`, clic derecho y Tab sin interferir con el navegador, aviso en dispositivos táctiles).

## Configuración de la página en itch.io
Esto lo hace la dueña del proyecto (cuenta personal; el agente no maneja credenciales).

1. **Create new project** → *Kind of project*: **HTML**.
2. Subir `SimuladorLOTO-WebGL.zip` y marcar **This file will be played in the browser**.
3. **Embed options**: *Viewport dimensions* 1280 × 720 · ✓ *Fullscreen button* · ✓ *Click to launch in fullscreen* (opcional) · *Mobile friendly* **desactivado** (la demo es para teclado y mouse).
4. **Visibility**: *Draft* o *Restricted* (con contraseña) hasta que se apruebe la demo.
5. Sin SharedArrayBuffer: no hace falta activar nada extra.

Con **butler** (opcional): `butler login` una vez y luego
```bash
butler push Builds/WebGL <usuario>/<juego>:html5
```

## Texto sugerido para la página
**Título:** Simulador LOTO — Bloqueo y etiquetado de energías peligrosas

**Descripción corta:** Practicá el procedimiento LOTO (OSHA 29 CFR 1910.147) sobre una bomba centrífuga antes de cambiar su sello mecánico.

**Descripción:**
> Recorré los 7 pasos del control de energías peligrosas en una sala de bombas virtual: preparar la intervención, comunicar y detener el equipo, aislar las fuentes de energía, aplicar candados y etiquetas, liberar la energía almacenada, verificar el aislamiento y restablecer el equipo de forma segura.
>
> Dos modos: **guiado** (te indica cada paso y explica los errores) y **evaluación** (sin ayudas; el resultado y los errores se informan al final).
>
> Controles: WASD moverse · mouse mirar · click interactuar · 1/2/3 candado, etiqueta y bloqueo de válvula · R guardar · T mapa de estaciones · Tab tablet · Esc pausa.

**Requisitos:** computadora con teclado y mouse, Chrome / Edge / Firefox actualizados, WebGL 2.

**Capturas sugeridas (1280×720):** menú · briefing · bomba con candado y etiqueta · tablet con la tarjeta LOTO · reporte final.

## Rendimiento y compatibilidad
- Calidad **Alta/Baja** en Pausa y en Opciones; en la build web, si el promedio de FPS al empezar es < 24, baja sola a *Baja* y avisa.
- Primera carga: ~12 MB comprimidos (Brotli con *decompression fallback* porque itch.io no envía el header `Content-Encoding`).
- Pendiente de medir en una notebook de oficina real (GPU integrada): objetivo ≥ 30 FPS.

## Checklist antes de compartir el link
- [ ] Jugar un recorrido completo en modo guiado y otro en evaluación en la página ya publicada.
- [ ] Probar Chrome y Edge (y Firefox si hace falta).
- [ ] Verificar que la pantalla de carga y el aviso de errores se ven bien.
- [ ] Revisar *Créditos* (Barlow OFL, Kenney CC0).
