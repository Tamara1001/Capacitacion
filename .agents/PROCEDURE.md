# PROCEDURE — Guion del procedimiento LOTO (Bomba P-101)

Base: síntesis de OSHA 29 CFR 1910.147 y su tutorial de control de energías peligrosas (ver REFERENCES.md). Versión **simplificada** para demo; cada paso tiene 1–3 acciones físicas.

## Escenario
- **Orden de trabajo OT-0427:** "Reemplazo de sello mecánico — Bomba centrífuga P-101 (agua de proceso)".
- **Trabajador (usuario):** técnico de mantenimiento, *autorizado* para LOTO.
- **Operador (NPC):** personal *afectado*, opera la sala desde su puesto.
- **Estado inicial:** la bomba está **en marcha** (motor zumbando, manómetro de descarga ~4 bar).

## Puntos de energía (tarjeta de LOTO del equipo)
| ID | Dispositivo | Energía | Estado normal → aislado |
|----|-------------|---------|-------------------------|
| DS-101 | Seccionador del motor (en tablero) | Eléctrica 380 V | ON → **OFF** + candado + etiqueta |
| V-101 | Válvula de succión (esférica, palanca) | Hidráulica | Abierta → **Cerrada** + bloqueo de válvula + candado + etiqueta |
| V-102 | Válvula de descarga (esférica, palanca) | Hidráulica | Abierta → **Cerrada** + bloqueo de válvula + candado + etiqueta |
| V-103 | Válvula de purga/drenaje | Presión residual | Cerrada → **Abierta** hasta manómetro = 0 |
| PI-101 | Manómetro de la carcasa | (indicador) | 4 bar → 0 bar |
| CS-101 | Botonera local START/STOP | Mando | Usada para detener y para *try-out* |

Kit LOTO (estación de bloqueo en la pared): 3 candados rojos personales (misma llave), 3 etiquetas "PELIGRO — NO OPERAR", 2 bloqueos de válvula de palanca. La **llave** queda en el inventario del usuario al colocar el primer candado.

---

## Paso 0 — EPP (antes de entrar)
- **Acción:** en el locker, equipar casco, anteojos de seguridad y guantes dieléctricos.
- **Distractores:** gorra, guantes de tela, auriculares.
- **Error (menor):** equipar un distractor o empezar el paso 1 sin EPP completo.

## Paso 1 — Preparar la intervención
- **Acción 1:** leer la OT en la tablet (botón "Leído").
- **Acción 2:** identificar las fuentes de energía: hacer click en DS-101, V-101, V-102 y V-103 (en la tablet se tildan en la tarjeta de LOTO).
- **Acción 3:** tomar el kit LOTO de la estación de bloqueo (habilita el inventario).
- **Feedback guiado:** los 4 puntos tienen un marcador pulsante amarillo hasta ser identificados.

## Paso 2 — Comunicar y detener
- **Acción 1:** hablar con el operador (o usar la radio) y elegir el mensaje correcto entre 3 opciones:
  - ✅ "Voy a bloquear la bomba P-101 para cambiar el sello. Va a quedar fuera de servicio."
  - ❌ "Voy a revisar la bomba, seguí trabajando normal." (no informa)
  - ❌ "Apagá todo el sector." (exceso / impreciso)
- **Acción 2:** presionar **STOP** en la botonera CS-101 → el motor se detiene (fade del zumbido, la manguera deja de vibrar).
- **Error (crítico):** abrir el seccionador **con el motor en marcha** (corte bajo carga) → chispa + mensaje "Detené el equipo con su mando normal antes de aislarlo".

## Paso 3 — Aislar las fuentes de energía
- **Acción 1:** bajar la palanca del seccionador DS-101 a **OFF**.
- **Acción 2:** cerrar **V-102 (descarga)** y luego **V-101 (succión)**.
- **Error (menor):** cerrar succión antes que descarga.
- **Error (crítico):** cerrar válvulas con la bomba en marcha (operación en vacío/cavitación) → sonido de cavitación + aviso.

## Paso 4 — Aplicar bloqueo y etiquetado
- **Acción:** para cada punto aparece un contorno fantasma; el usuario selecciona el ítem del kit y hace click:
  - DS-101: candado + etiqueta.
  - V-101 y V-102: bloqueo de válvula → candado → etiqueta.
- La etiqueta se autocompleta: *nombre del usuario (o "Técnico"), fecha, OT-0427, motivo*.
- **Error (menor):** colocar etiqueta sin candado ("la etiqueta sola no bloquea").
- **Error (crítico):** saltar a pasos 5–6 con algún punto sin candado.

## Paso 5 — Controlar la energía almacenada
- **Acción:** abrir la purga V-103; el manómetro PI-101 baja de 4 a 0 bar en ~4 s (sonido de fluido drenando a un balde/sumidero). Cerrar la purga cuando marque 0 es opcional (queda abierta durante el trabajo — ambas válidas).
- **Error (crítico):** abrir la purga **antes** de cerrar las válvulas → chorro de agua + aviso "Aislá antes de liberar".

## Paso 6 — Verificar el aislamiento
- **Acción 1 (try-out):** presionar **START** en CS-101 → no pasa nada (luz roja, clic seco) → aviso "Aislamiento verificado".
- **Acción 2:** **volver STOP** (dejar el mando en posición de parada).
- **Acción 3:** confirmar en la tablet que el manómetro marca 0.
- **Acción 4:** interactuar con la carcasa de la bomba: "Realizar el cambio de sello mecánico" (el mantenimiento no se juega en v1; en M2 se agrega un fade). Aparecen 2 herramientas olvidadas en el piso.
- **Error (crítico):** si el usuario intenta "Iniciar mantenimiento" (interacción en la carcasa) antes del try-out.

## Paso 7 — Finalizar y restablecer
- **Acción 1:** inspeccionar el área: juntar 2 herramientas del piso y cerrar la purga V-103.
- **Acción 2:** avisar al operador que se retira el bloqueo y que se aleje del equipo.
- **Acción 3:** retirar etiquetas y candados con la llave; retirar bloqueos de válvula; devolver todo a la estación.
- **Acción 4:** abrir **V-101 (succión)** y luego **V-102 (descarga)**; subir el seccionador a **ON**; presionar **START** → la bomba arranca.
- **Errores (menores):** dejar herramientas, no avisar al operador, abrir descarga antes que succión, energizar con purga abierta (pérdida de agua).
- **Fin:** pantalla de **Reporte**.

---

## Reglas de evaluación
- **Severidades:** `Minor` (−5 pts), `Critical` (−20 pts, se marca en rojo en el reporte con explicación). Puntaje base 100, mínimo 0.
- **Aprobado:** ≥ 70 y **sin errores críticos**.
- **Orden de pasos:** las acciones de un paso posterior pueden ejecutarse antes (se permite), pero cada desvío de orden genera un `Minor` "Fuera de orden" salvo que exista un error más específico.
- **Tiempo:** se mide desde el inicio del paso 0 hasta el arranque final; se muestra, no penaliza en v1.

## Diferencias por modo
| | Guiado | Evaluación |
|--|--------|-----------|
| Paso actual en tablet/HUD | Sí, con descripción | Solo el título del paso |
| Resaltado del objetivo | Sí (outline + marcador) | No |
| Contornos fantasma | Sí | No (el snap funciona igual) |
| Aviso inmediato de error | Sí, con explicación | Solo un sonido; explicación en el reporte |
