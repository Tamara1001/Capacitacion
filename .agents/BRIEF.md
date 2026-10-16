# BRIEF — Simulador de capacitación LOTO

## Qué es
Demo de un simulador de capacitación laboral en 3D donde el usuario ejecuta el procedimiento **LOTO (Lockout/Tagout — bloqueo y etiquetado de energías peligrosas)** sobre una **bomba centrífuga con motor eléctrico** antes de realizar un **cambio de sello mecánico**.

- Plataforma v1: **WebGL**, mouse + teclado, publicado en **itch.io**.
- Plataforma futura: **VR** (Meta Quest / OpenXR). El código se diseña desde el día 1 para portarse.
- Motor: **Unity 6000.3 (Unity 6.3) + URP**, Input System, uGUI + TextMeshPro.

## Público
**Clientes / empresas** a quienes se les muestra la demo. Prioridades en orden:
1. Que funcione de punta a punta sin trabas (fluidez, cero bloqueos).
2. Primera impresión profesional (estética limpia, UI prolija, feedback claro).
3. Fidelidad al procedimiento OSHA (simplificado pero correcto).

## Objetivo de aprendizaje
Al terminar, el usuario sabe que antes de intervenir un equipo debe:
preparar → comunicar y detener → aislar → bloquear y etiquetar → liberar energía almacenada → verificar → (trabajar) → finalizar y restablecer.

## Alcance v1 (MVP, 1–2 semanas)
- 1 escena de juego: **Sala de bombas** interior + escena **Menú**.
- **7 pasos OSHA simplificados** + paso 0 de EPP. Partida de **5–10 minutos**.
- Energías: **eléctrica** (seccionador), **hidráulica** (válvulas de succión y descarga), **presión residual** (purga + manómetro).
- Dispositivos LOTO: candado personal con llave, etiqueta con datos, bloqueo de válvula.
- NPC operador estático + radio para comunicar.
- Modos: **Guiado** (resaltados, contornos fantasma) y **Evaluación** (sin pistas).
- Errores permitidos y penalizados; reporte final con tiempo, orden de pasos y errores.
- Prueba de arranque (*try-out*) como verificación.
- Pantallas: menú, briefing, pausa/opciones, reporte final.
- Audio: SFX + ambiente (sin voz).
- Locomoción: WASD libre + salto rápido a estaciones.

## Fuera de alcance v1 (backlog)
Port VR, pinza multi-candado (hasp), caja de bloqueo grupal, multímetro/detector de tensión, narración/TTS, localización a otros idiomas, exportar reporte, backend/LMS/SCORM, quizzes, otros equipos/escenarios, animaciones de personajes complejas, vestuario separado.

## Criterios de éxito de la demo
- Se completa sin errores técnicos en Chrome/Edge en una notebook de oficina (GPU integrada) a **≥30 FPS**.
- Build comprimida **< 50 MB**, carga inicial **< 20 s** en conexión normal.
- Un usuario sin experiencia completa el modo guiado sin ayuda externa.
- Los errores peligrosos se detectan y se explican en el reporte.
