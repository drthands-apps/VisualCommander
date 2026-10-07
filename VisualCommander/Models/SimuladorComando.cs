using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace VisualCommander.Models
{
    /// <summary>Resultado de una simulación previa a la ejecución.</summary>
    public class ResultadoSimulacion
    {
        /// <summary>¿Aplica esta simulación? (comando peligroso y simulable)</summary>
        public bool Aplica { get; set; }

        /// <summary>Nombre del comando simulado.</summary>
        public string Comando { get; set; } = "";

        /// <summary>Resumen legible del impacto.</summary>
        public string Resumen { get; set; } = "";

        /// <summary>Elementos concretos afectados (archivos, procesos, carpetas).</summary>
        public List<string> Afectados { get; set; } = new();
    }

    /// <summary>
    /// Analiza una línea de comando peligrosa y devuelve una previsualización
    /// de lo que haría sin llegar a ejecutarla.
    /// </summary>
    public static class SimuladorComando
    {
        public static ResultadoSimulacion Simular(LineaComando linea)
        {
            if (linea.ComandoActivo?.Origen is not Comando cmd)
                return new ResultadoSimulacion { Aplica = false };

            if (!cmd.Peligroso)
                return new ResultadoSimulacion { Aplica = false };

            var tokens = linea.Tokens.ToList();
            var nombre = cmd.Nombre.ToLowerInvariant();

            return nombre switch
            {
                "del" => SimularDel(tokens, cmd),
                "rd" or "rmdir" => SimularRd(tokens, cmd),
                "taskkill" => SimularTaskkill(tokens, cmd),
                _ => new ResultadoSimulacion
                {
                    Aplica = true,
                    Comando = cmd.Nombre,
                    Resumen =
                        $"⚠ '{cmd.Nombre}' está marcado como peligroso.\n" +
                        "No hay una simulación específica para este comando todavía.\n" +
                        "Revisa bien los parámetros antes de continuar."
                }
            };
        }

        // ==================== del ====================
        private static ResultadoSimulacion SimularDel(List<Token> tokens, Comando cmd)
        {
            var r = new ResultadoSimulacion { Aplica = true, Comando = cmd.Nombre };

            var recursivo = tokens.Any(t => t.Texto.Equals("/S", StringComparison.OrdinalIgnoreCase));
            var argumentos = ExtraerArgumentos(tokens);

            if (argumentos.Count == 0)
            {
                r.Resumen = "⚠ 'del' sin argumentos no borrará nada.";
                return r;
            }

            var rutaCompleta = Path.GetFullPath(argumentos[0]);

            List<string> archivos;
            try
            {
                var dir = Path.GetDirectoryName(rutaCompleta);
                var patron = Path.GetFileName(rutaCompleta);
                if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
                if (string.IsNullOrEmpty(patron)) patron = "*";

                var opcion = recursivo
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                archivos = Directory.EnumerateFiles(dir, patron, opcion).ToList();
            }
            catch (Exception ex)
            {
                r.Resumen = $"⚠ No se pudo simular 'del': {ex.Message}";
                return r;
            }

            r.Afectados.AddRange(archivos.Select(f => "🗑  " + f));

            r.Resumen = archivos.Count == 0
                ? $"⚠ 'del' no encontraría ningún archivo para borrar.\n   Patrón: {rutaCompleta}"
                : $"⚠ SE VAN A BORRAR {archivos.Count} ARCHIVO(S) de forma permanente.\n" +
                  "   (Los archivos NO van a la papelera de reciclaje.)\n\n" +
                  $"   Patrón:    {rutaCompleta}\n" +
                  $"   Recursivo: {(recursivo ? "sí (incluye subcarpetas)" : "no")}";

            return r;
        }

        // ==================== rd / rmdir ====================
        private static ResultadoSimulacion SimularRd(List<Token> tokens, Comando cmd)
        {
            var r = new ResultadoSimulacion { Aplica = true, Comando = cmd.Nombre };

            var recursivo = tokens.Any(t => t.Texto.Equals("/S", StringComparison.OrdinalIgnoreCase));
            var argumentos = ExtraerArgumentos(tokens);

            if (argumentos.Count == 0)
            {
                r.Resumen = "⚠ 'rd' sin argumentos no borrará nada.";
                return r;
            }

            var rutaCompleta = Path.GetFullPath(argumentos[0]);

            if (!Directory.Exists(rutaCompleta))
            {
                r.Resumen = $"⚠ La carpeta no existe:\n   {rutaCompleta}";
                return r;
            }

            var subcarpetas = recursivo
                ? Directory.EnumerateDirectories(rutaCompleta, "*", SearchOption.AllDirectories).ToList()
                : new List<string>();
            var archivos = recursivo
                ? Directory.EnumerateFiles(rutaCompleta, "*", SearchOption.AllDirectories).ToList()
                : Directory.EnumerateFiles(rutaCompleta).ToList();

            r.Afectados.Add("📁 " + rutaCompleta);
            r.Afectados.AddRange(subcarpetas.Select(s => "📂 " + s));
            r.Afectados.AddRange(archivos.Select(a => "🗑  " + a));

            r.Resumen =
                $"⚠ SE VA A BORRAR LA CARPETA Y TODO SU CONTENIDO.\n\n" +
                $"   Carpeta:             {rutaCompleta}\n" +
                $"   Subcarpetas:         {subcarpetas.Count}\n" +
                $"   Archivos:            {archivos.Count}\n" +
                $"   Total de elementos:  {1 + subcarpetas.Count + archivos.Count}";

            return r;
        }

        // ==================== taskkill ====================
        private static ResultadoSimulacion SimularTaskkill(List<Token> tokens, Comando cmd)
        {
            var r = new ResultadoSimulacion { Aplica = true, Comando = cmd.Nombre };

            var valorIm = BuscarValorParametro(tokens, "/IM");
            var valorPid = BuscarValorParametro(tokens, "/PID");

            if (valorIm != null)
            {
                var nombreExe = valorIm.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? valorIm[..^4] : valorIm;

                var procesos = Process.GetProcessesByName(nombreExe);

                r.Resumen =
                    $"⚠ SE VAN A CERRAR {procesos.Length} PROCESO(S) llamado(s) '{nombreExe}'.";

                foreach (var p in procesos.OrderBy(p => p.Id))
                {
                    long mb = 0;
                    try { mb = p.WorkingSet64 / 1024 / 1024; } catch { }
                    r.Afectados.Add($"⚙️  PID {p.Id,6}   {p.ProcessName}   ({mb} MB)");
                }
                return r;
            }

            if (valorPid != null && int.TryParse(valorPid, out var pid))
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    long mb = 0;
                    try { mb = p.WorkingSet64 / 1024 / 1024; } catch { }

                    r.Resumen = $"⚠ SE VA A CERRAR EL PROCESO PID {pid}.";
                    r.Afectados.Add($"⚙️  PID {p.Id}   {p.ProcessName}   ({mb} MB)");
                }
                catch
                {
                    r.Resumen = $"⚠ No existe ningún proceso con PID {pid}.";
                }
                return r;
            }

            r.Resumen = "⚠ 'taskkill' sin /IM ni /PID no hará nada.";
            return r;
        }

        // ==================== Helpers ====================
        private static List<string> ExtraerArgumentos(List<Token> tokens)
        {
            // Saltamos el token del comando y los parámetros (/X o -x).
            var resultado = new List<string>();
            for (int i = 1; i < tokens.Count; i++)
            {
                var txt = tokens[i].Texto;
                if (string.IsNullOrEmpty(txt)) continue;
                if (txt.StartsWith("/") || txt.StartsWith("-")) continue;
                resultado.Add(txt.Trim('"'));
            }
            return resultado;
        }

        private static string? BuscarValorParametro(List<Token> tokens, string nombreParametro)
        {
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Texto.Equals(nombreParametro, StringComparison.OrdinalIgnoreCase))
                    return tokens[i + 1].Texto.Trim('"');

                // Variante /IM:valor
                if (tokens[i].Texto.StartsWith(nombreParametro + ":", StringComparison.OrdinalIgnoreCase))
                    return tokens[i].Texto[(nombreParametro.Length + 1)..].Trim('"');
            }
            return null;
        }
    }
}