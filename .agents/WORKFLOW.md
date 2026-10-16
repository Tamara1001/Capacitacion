# WORKFLOW — Cómo se trabaja en este repo

## Entorno
- Unity **6000.3.14f1**, URP 17.3, Input System 1.19 (Input Manager legado desactivado).
- Repo: `https://github.com/Tamara1001/Capacitacion` — rama única `main`.
- Herramientas disponibles para agentes: Unity MCP (escena, assets, consola, tests, build), Blender (headless por scripts), ffmpeg (procesar audio/video de referencia), git + Git LFS, GitHub CLI.
- **Blender 5.2.2 LTS** (Steam, no está en el PATH): `BLENDER_EXE = D:/Programas/Steam/steamapps/common/Blender/blender.exe`.
- Módulos de build instalados: Web (activo), Windows, **Android** (servirá para Quest en v2).

## Git
- Commits directos a `main` (decisión del proyecto). Pushear al cerrar cada tarea del ROADMAP.
- Formato de mensajes: ver CODESTYLE.md §Commits.
- **LFS** (`.gitattributes` ya lo define con la plantilla de Unity). Verificar que cubra: `*.fbx *.blend *.png *.jpg *.tga *.psd *.wav *.mp3 *.ogg *.ttf *.otf *.exr *.hdr`. Agregar lo que falte antes de commitear binarios.
- Antes de `git add`: `git status` y agregar **rutas explícitas**. No commitear cambios del editor ajenos a la tarea (ej. reimportes de settings) sin revisarlos.
- Nunca `push --force` a `main`.

## Ciclo de una tarea (agentes)
1. Leer la tarea en ROADMAP.md y los docs relevantes.
2. Implementar en pasos chicos; tras cada cambio de scripts, esperar recompilación y leer la consola (MCP `console` / `Unity_ReadConsole`). Cero errores, cero warnings nuevos.
3. Si toca lógica de procedimiento/evaluación: correr Edit Mode tests.
4. Verificar en Play Mode (captura de Game View vía MCP cuando sea visual).
5. Guardar escenas/prefabs (`save_all`).
6. Marcar la tarea en ROADMAP.md (`[x]`), actualizar DECISIONS.md si algo cambió.
7. Commit + push.

## Unity MCP — buenas prácticas
- Preferir crear/editar scripts como archivos (Write/Edit) y dejar que Unity recompile; usar MCP para escena, prefabs, componentes, serialized fields, settings y build.
- No dejar el editor en Play Mode al terminar.
- Para cambios masivos de escena, usar un script de editor (`LotoSim.Editor`) idempotente en vez de decenas de llamadas sueltas.
- Capturas de verificación: Game View a 1280×720.

## Blender
- Fuentes en `Blender/`, scripts en `Blender/scripts/`. Ejecutar headless:
  `"<BLENDER_EXE>" -b -P Blender/scripts/build_<asset>.py -- --out Assets/_Project/Art/Models/SM_<Asset>.fbx`
- Reglas de export: ASSETS.md §Reglas de exportación.
- Después de exportar: revisar import en Unity (escala, pivots, normales), crear/actualizar el prefab correspondiente reemplazando el greybox **sin** romper referencias (el prefab conserva componentes; sólo cambia el hijo visual).

## Build WebGL
- Build Profile: Web. Escenas: `Menu`, `PumpRoom`.
- Player Settings:
  - Compression **Brotli** + **Decompression Fallback ✓** (itch.io no envía el header `Content-Encoding`). Si da problemas, Gzip + fallback.
  - Code stripping **High**, IL2CPP *Optimize size*, Exceptions *Explicitly Thrown* (Development: *Full With Stacktrace*).
  - Color space Linear, Graphics API WebGL2 (WebGPU opcional en v2).
  - Template: personalizado minimalista (fondo grafito, barra de carga con la paleta del proyecto).
- Calidad para Web: sin sombras realtime suaves de alta resolución, MSAA 2x, render scale 1.0, lightmaps comprimidos.
- Salida: `Builds/WebGL/` (ignorado por git).
- Probar localmente con un servidor estático antes de subir.

## Publicación en itch.io
- Proyecto tipo *HTML*, "This file will be played in the browser", viewport 1280×720, ✓ fullscreen button, ✓ SharedArrayBuffer **no** necesario.
- Subida: zip de `Builds/WebGL/` manual, o **butler** (`butler push Builds/WebGL <usuario>/<juego>:html5`).
- **La dueña del proyecto** debe crear la página y hacer `butler login` (credenciales personales: el agente no las maneja).
- Visibilidad: *Restricted* con contraseña o *Draft* hasta que se apruebe la demo.

## Definición de "Hecho"
- Compila sin errores ni warnings nuevos; tests verdes.
- Funciona en Play Mode y, al cierre de cada hito, en build WebGL (Chrome).
- Textos en español sin faltas; UI legible a 1280×720.
- ROADMAP actualizado y commit pusheado.
