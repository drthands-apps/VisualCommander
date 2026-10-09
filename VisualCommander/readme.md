# Visual Commander

Editor visual de línea de comandos para Windows.

Construye comandos de forma gráfica arrastrando bloques, previsualiza el
resultado en una consola simulada, y ejecútalos con seguridad (con modo
simulación para comandos destructivos).

## Estado

🚧 En desarrollo activo. Consulta `NOTAS.md` para el roadmap completo.

## Características actuales

- Catálogo de comandos de Windows agrupado por familia.
- Constructor visual con bloques arrastrables.
- Editor in-place por línea con parseo heurístico.
- Múltiples líneas con contexto de directorio independiente.
- Apariencia de consola CMD con marca de "SIMULACIÓN".
- Ejecución real con captura de salida en vivo.
- Simulador de comandos peligrosos (del, rd, taskkill).
- Árbol de archivos con unidades reales (carga perezosa).

## Características planeadas

Ver `NOTAS.md` para el detalle. En resumen:
- Pestaña de utilidades con ConPTY (diskpart, bcdboot, editores...).
- Plantillas de comandos frecuentes.
- Guardar/cargar sesión (.vcmd).
- Exportar a .bat, .ps1, .sh.
- Atajos de teclado completos.
- Modo aprendiz vs experto.

## Requisitos

- Windows 10 o superior.
- .NET 10 SDK.
- Visual Studio 2022 (o VS Code con extensión C#).

## Cómo ejecutar

1. Clonar el repositorio.
2. Abrir `VisualCommander.sln` en Visual Studio.
3. Compilar y ejecutar (F5).

## Licencia

(Por decidir — MIT, Apache 2.0, GPL... recomendable decidirlo pronto.)