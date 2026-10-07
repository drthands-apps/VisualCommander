# Visual Commander — Notas de diseño y planificación

Este archivo centraliza decisiones, features pendientes y visiones de futuro.
Actualizar cada vez que se tome una decisión estructural o se añada una idea nueva.


## Filosofía: Editor, no consola

Visual Commander es un EDITOR de comandos, no una consola en vivo.
La ejecución es una fase posterior al ciclo de composición, no simultánea.
Esto implica:

- Los comandos `cd`, `set`, `path` etc. MODIFICAN EL PLAN, no el sistema.
- El DirectorioTrabajo es POR LÍNEA, heredado de la línea anterior.
- Pulsar Enter tras un `cd` cierra la línea con un nuevo contexto.
- La ejecución real solo ocurre al pulsar Ejecutar o al exportar el .bat.
- Un .bat exportado contiene los `cd` tal cual; el plan se ejecuta íntegro.

Analogía: Excel es un editor de cálculos, no un intérprete.
Git es un editor de cambios, no una escritura automática.
Visual Commander es el editor de comandos de Windows.





---

## Estado actual

- [x] Layout y paneles redimensionables
- [x] Catálogo JSON + agrupación + buscador
- [x] Tokens, drag & drop, modificadores dinámicos
- [x] Multi-línea (LineaComando)
- [x] Apariencia CMD simulada
- [x] Ejecución real con captura de salida
- [x] Simulador de comandos peligrosos (del, rd, taskkill)
- [x] Árbol de archivos con unidades reales (carga perezosa)
- [x] DirectorioTrabajo + prompt dinámico + selector + drag al prompt
- [x] Coloreado del prompt según peligrosidad (verde/amarillo/rojo)
- [x] DirectorioTrabajo vive en LineaComando
- [x] MainViewModel delega a LineaActiva con puente de notificaciones
- [x] Nueva línea hereda el directorio de la anterior
- [x] Cambiar línea activa actualiza el prompt de la UI
- [x] ItemsControl apilando líneas cerradas
- [x] La línea activa no se duplica (oculta del ItemsControl)
- [x] Editor único al final
- [x] Clic sobre línea cerrada la hace activa
- [x] Fix NullReference al eliminar



---





### Pendiente D.2
- [ ] Ordenación correcta: no permitir intercalar una línea activa
      entre líneas cerradas sin coherencia. Al reactivar una línea
      antigua, debe moverse al final de la pila (o bloquear la
      reordenación hasta que se haga explícita).
- [ ] Recuperación del "último nivel" tras intercalar: garantizar
      que siempre se puede volver al estado anterior.
- [ ] Auto-scroll al editor cuando se añaden líneas.
- [ ] Botón ✕ por línea cerrada en el ItemsControl.
- [ ] Indicador visual por línea (pendiente / ejecutada / con error).

## Atajos de teclado (pendiente)

- [ ] Ctrl+Z / Ctrl+Y: Undo / Redo sobre la línea activa
- [ ] Ctrl+Shift+Z: alternativa para Redo
- [ ] Ctrl+Enter: ejecutar la línea activa
- [ ] Ctrl+N: nueva línea
- [ ] F5: ejecutar todo el plan
- [ ] Esc: cancelar edición en curso

## Fase C — Enter crea nueva línea

Cuando el usuario pulsa Enter sobre una línea con:
- Un `cd` o `pushd` con ruta válida → crear nueva línea con el
  directorio heredado del nuevo path.
- Un operador (`|`, `&&`, `||`) → crear nueva línea (el comando
  anterior queda cerrado).
- Cualquier comando "normal" → por ahora, mantener la línea abierta
  (el usuario decide cuándo cerrarla con `＋ Nueva`).

### Por qué no siempre
Forzar la creación de nueva línea en cada Enter convertiría la
herramienta en una consola, no en un editor. El usuario debe poder
componer varias líneas sin que se cierren automáticamente.


## Plan multi-línea (Fase D.2)

- [ ] Mostrar todas las líneas del plan apiladas verticalmente.
- [ ] La línea activa resaltada.
- [ ] Las líneas anteriores atenuadas.
- [ ] Indicador de estado por línea (pendiente / ejecutada / con error).
- [ ] Historial visual tipo consola real.


## En curso / pendiente inmediato

- [ ] Drag & drop de rutas del árbol al constructor
- [ ] Entrada por teclado en la línea de comando (parseo → tokens)
- [ ] Diálogo de confirmación para comandos peligrosos
- [ ] Campo `requiereAdmin` en el JSON + elevación puntual por comando (Verb="runas")
- [ ] Cabecera estándar en exportación .bat con `cd /D`
- [ ] Diálogo de confirmación muestra en grande el directorio de trabajo
- [ ] Detección de rutas relativas en tokens
- [ ] Cabecera estándar en exportación .bat con `cd /D`
- [ ] Arrastrar carpeta al prompt cambia directorio de trabajo
- [ ] Diálogo de selección de carpeta (OpenFolderDialog)

---
## Gestión completa de líneas (Fase D.3)

Modelo mental: cada LineaComando es una unidad independiente y reordenable,
como una fila en Excel o un párrafo en un editor de texto.

### Operaciones
- Insertar nueva línea después de la activa (no al final)
- Mover líneas (reordenar)
- Eliminar líneas concretas
- Copiar / Cortar / Pegar líneas
- Duplicar línea activa
- Reactivar una línea existente para editarla in-place

### Decisiones
- El editor vive DENTRO de la línea activa (in-place), no al final.
- Solo hay una línea activa a la vez (selección simple).
- Portapapeles = texto plano del comando. Compatible con el sistema.
- "+" inserta después de la activa.

### Sub-fases
- [ ] D.3.A: Editor in-place
- [ ] D.3.B: "+" inserta después de la activa
- [ ] D.3.C: Copiar / Cortar / Pegar / Duplicar
- [ ] D.3.D: Mover arriba/abajo
- [ ] D.3.E: Menú contextual
- [ ] D.3.F: Atajos de teclado
- [ ] D.3.G: Selección múltiple (futuro)


## Gestión completa de líneas (Fase D.3)

Modelo mental: cada LineaComando es una unidad independiente y reordenable.

### Regla de "línea viva única"

En todo momento:
1. Hay exactamente una línea activa.
2. La activa puede estar en cualquier posición.
3. NO hay líneas vacías intermedias. Solo la activa puede estar vacía.
4. Al cerrar una línea, si deja una vacía huérfana entre cerradas,
   se elimina automáticamente.

### Señales visuales de la línea activa (combinadas)
- Borde izquierdo azul (ya)
- Fondo más claro (ya)
- Círculo brillante al principio (nuevo)
- Contador discreto a la derecha (nuevo)

### Ideas para fase de pulido visual (futuro)
- Animación de borde que "crece" al activar (Notion)
- Sombra suave alrededor (Figma, Linear)
- Transición de opacidad en el fondo

### Botones
- `＋ Nueva`: cierra la actual (si tiene contenido) y crea nueva después.
- `✓ Cerrar`: cierra la actual sin crear nueva. Pasa el foco a la siguiente.

### Sub-fases
- [ ] D.3.A: Editor in-place
- [ ] D.3.B: Regla de "línea viva única"
- [ ] D.3.C: `＋ Nueva` inserta después de la activa
- [ ] D.3.D: Botón ✓ Cerrar
- [ ] D.3.E: Copiar / Cortar / Pegar / Duplicar
- [ ] D.3.F: Mover arriba/abajo
- [ ] D.3.G: Menú contextual clic derecho
- [ ] D.3.H: Atajos de teclado
- [ ] D.3.I: Señales visuales adicionales


## Pendiente técnico (mejoras futuras al drag del árbol)

- [ ] Al arrastrar a la línea, permitir soltar en cualquier posición
      (hoy siempre se añade al final).
- [ ] Al arrastrar múltiples archivos seleccionados, añadirlos todos.
- [ ] Al arrastrar sobre un bloque existente, envolverlo (útil para
      rutas que envuelven a parámetros, como en xcopy origen destino).

---

## Features planificadas (Tier 1)

- [ ] Plantillas de comandos frecuentes (recetas preconfiguradas)
- [ ] Guardar/cargar sesión completa (archivo .vcmd en JSON)
- [ ] Exportar a .bat, .ps1, .sh, .md
- [ ] Historial con contexto (comando, cwd, timestamp, resultado)
- [ ] Modo aprendiz vs experto
- [ ] Edición e inserción de tokens in-place
- [ ] Undo / Redo

---

## Contexto de ejecución (diseño)

Tres contextos distintos:
1. Composición: dónde navega el usuario mientras construye el comando.
2. Ejecución inmediata: dónde se ejecuta el comando al pulsar Ejecutar.
3. Ejecución diferida: dónde se ejecutará el .bat cuando se abra, quizás
   en otro equipo, con otro usuario y otra sesión.
---

### Reglas acordadas
- `DirectorioTrabajo` es una propiedad explícita del ViewModel.
- El prompt verde del CMD simulado muestra SIEMPRE `DirectorioTrabajo`.
- El usuario puede cambiarlo con un selector o arrastrando una carpeta
  desde el árbol al prompt.
- Las rutas relativas se resuelven contra `DirectorioTrabajo`.
- Al exportar a .bat, SIEMPRE se antepone cabecera con `C:` + `cd /D "ruta"`.
- El prompt se colorea según peligrosidad del directorio:
  - Normal: verde CMD.
  - Sistema (C:\Windows, C:\Program Files, C:\ProgramData): amarillo.
  - Raíz de unidad (C:\): rojo.
- El diálogo de confirmación de comandos peligrosos muestra en grande
  el directorio de trabajo.
---



---



## Visión futura: Pestaña de Utilidades (ConPTY)

### Objetivo
Ofrecer un espacio para utilidades avanzadas que no encajan en el constructor
visual, pero que aprovechan la misma infraestructura (JSON, drag&drop, tokens).

### Arquitectura prevista
- **Pestaña "Utilidades"** que agrupa herramientas organizadas como plugins.
- Cada plugin declara: nombre, categoría, comando, parámetros, icono, color.
- **Carga dinámica desde carpeta `Plugins/`**: los plugins son archivos JSON
  que el usuario o terceros pueden añadir sin recompilar.
- **Motor ConPTY** para utilidades que requieren terminal real (editores
  interactivos, REPLs, etc.).
- [ ] Indicador visual permanente cuando la app o un comando se ejecuta en modo admin

### Ejemplos identificados
- Análisis de redes avanzado (nmap, netstat enriquecido, Wireshark CLI)
- Comparación de ficheros (fc, diff, windiff)
- Integridad del sistema (sfc, dism, chkdsk)
- Gestión de licencias (slmgr)
- Utilidades de disco avanzadas (diskpart en modo script)
- Gestión de arranque (bcdboot, bcdedit)
- Editores de texto interactivos (vi, vim, nano) → requiere ConPTY

### Tecnología ConPTY
- Biblioteca candidata: paquete NuGet con implementación ConPTY + control WPF.
- Permite embeder una consola real que funcione con procesos interactivos.
- Pendiente: evaluar opciones, hacer prototipo aislado, medir rendimiento.

### Cuándo
Después de completar Tier 1. Es una capa **aditiva**, no invasiva:
el constructor visual sigue siendo el flujo principal.

---

## Decisiones de diseño consolidadas

1. **MVVM estricto**: el modelo manda, la vista reacciona.
2. **JSON como base de datos declarativa**: añadir comandos o utilidades no
   requiere recompilar.
3. **Multi-línea desde el inicio**: `LineaComando` encapsula todo lo de una línea.
4. **Permisos elevados comando-a-comando** (no app-wide): control y sensación
   de control para el usuario.
5. **Apariencia CMD con marca de simulación**: seguridad + familiaridad.
6. **Modo simulación para comandos destructivos**: diferencial clave frente a CMD.