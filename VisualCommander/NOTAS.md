# Visual Commander — Notas de diseño y planificación

Este archivo centraliza decisiones, features pendientes y visiones de futuro.
Actualizar cada vez que se tome una decisión estructural o se añada una idea nueva.

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

---

## En curso / pendiente inmediato

- [ ] Drag & drop de rutas del árbol al constructor
- [ ] Entrada por teclado en la línea de comando (parseo → tokens)
- [ ] Diálogo de confirmación para comandos peligrosos
- [ ] Campo `requiereAdmin` en el JSON + elevación puntual por comando (Verb="runas")

---

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
### Pendientes
- [ ] Prompt coloreado según peligrosidad (DataTrigger sobre propiedad calculada)
- [ ] Cabecera estándar en exportación .bat con `cd /D`
- [ ] Arrastrar carpeta al prompt cambia directorio de trabajo
- [ ] Diálogo de selección de carpeta (OpenFolderDialog)
- [ ] Detección de rutas relativas en tokens (avisar si el comando las usa)

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