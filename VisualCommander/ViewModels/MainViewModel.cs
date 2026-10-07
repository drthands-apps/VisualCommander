using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Data;
using VisualCommander.Models;


namespace VisualCommander.ViewModels
{
    /// <summary>
    /// ViewModel principal. Contiene el catálogo de comandos, la colección
    /// de líneas de comandos y coordina la línea activa.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // ==== Catálogo ====

        public ObservableCollection<Comando> Comandos { get; } = new();
        public ICollectionView ComandosView { get; }

        // ==== Líneas de comando ====

        /// <summary>Todas las líneas que el usuario ha creado.</summary>
        public ObservableCollection<LineaComando> Lineas { get; } = new();

        private LineaComando _lineaActiva = null!;
        /// <summary>Línea que se está editando en el constructor visual.</summary>
        public LineaComando LineaActiva
        {
            get => _lineaActiva;
            set
            {
                if (ReferenceEquals(_lineaActiva, value)) return;
                _lineaActiva = value;
                OnPropertyChanged();
            }
        }

        // ==== Estado de ejecución ====

        /// <summary>Líneas de salida del último comando ejecutado.</summary>
        public ObservableCollection<string> Salida { get; } = new();

        private bool _estaEjecutando;
        public bool EstaEjecutando
        {
            get => _estaEjecutando;
            private set { _estaEjecutando = value; OnPropertyChanged(); }
        }

        private string _mensajeCmd = "SIMULACIÓN — el comando aún no se ha ejecutado";
        public string MensajeCmd
        {
            get => _mensajeCmd;
            private set { _mensajeCmd = value; OnPropertyChanged(); }
        }

        private Process? _procesoActual;

        // ==== Estado de selección de catálogo ====

        private Comando? _comandoSeleccionado;
        public Comando? ComandoSeleccionado
        {
            get => _comandoSeleccionado;
            set
            {
                if (ReferenceEquals(_comandoSeleccionado, value)) return;
                _comandoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoDescripcion));
                OnPropertyChanged(nameof(TextoAyuda));
            }
        }

        private string _filtro = "";
        public string Filtro
        {
            get => _filtro;
            set
            {
                if (_filtro == value) return;
                _filtro = value;
                OnPropertyChanged();
                ComandosView.Refresh();
            }
        }

        private string _status = "Listo";
        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }

        

        /// <summary>Ruta actual del sistema para simular el prompt de CMD.</summary>
        public string PromptCmd => Environment.CurrentDirectory + ">";

        // ==== Propiedades calculadas ====

        public string TextoDescripcion =>
            ComandoSeleccionado is null
                ? "Selecciona un comando de la lista para ver aquí su descripción, sintaxis, parámetros y ejemplos de uso."
                : $"📌 {ComandoSeleccionado.Descripcion}\n\n" +
                  $"Sintaxis:\n{ComandoSeleccionado.Sintaxis}\n\n" +
                  $"Ejemplos:\n  " + string.Join("\n  ", ComandoSeleccionado.Ejemplos);

        public string TextoAyuda =>
            ComandoSeleccionado is null
                ? "Aquí aparecerá información sensible al contexto: parámetros válidos para el comando seleccionado, combinaciones permitidas, advertencias y ejemplos."
                : (ComandoSeleccionado.Peligroso
                    ? "⚠ ATENCIÓN: este comando puede ser destructivo. Revisa bien los parámetros antes de ejecutarlo.\n\n"
                    : "") +
                  $"Parámetros válidos ({ComandoSeleccionado.Parametros.Count}):";

        // ==== Constructor ====

        public MainViewModel()
        {
            CargarComandos();

            ComandosView = CollectionViewSource.GetDefaultView(Comandos);
            ComandosView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Comando.Familia)));
            ComandosView.Filter = FiltroComando;

            // Creamos la primera línea.
            _lineaActiva = new LineaComando();
            Lineas.Add(_lineaActiva);
        }

        // ==== Gestión de líneas ====

        public void NuevaLinea()
        {
            var nueva = new LineaComando();
            Lineas.Add(nueva);
            LineaActiva = nueva;
            SetStatus($"Nueva línea creada ({Lineas.Count} en total)");
        }

        public void EliminarLineaActiva()
        {
            if (Lineas.Count <= 1)
            {
                SetStatus("Debe quedar al menos una línea");
                return;
            }

            var aBorrar = LineaActiva;
            var idx = Lineas.IndexOf(aBorrar);
            Lineas.Remove(aBorrar);

            // Elegimos la línea anterior si existe, o la última.
            LineaActiva = idx > 0 ? Lineas[idx - 1] : Lineas[^1];
            SetStatus($"Línea eliminada ({Lineas.Count} restantes)");
        }

        // ==== API pública para la línea activa ====

        public void AgregarComando(Comando cmd)
        {
            LineaActiva.AgregarToken(new Token
            {
                Tipo = TipoToken.Comando,
                Texto = cmd.Nombre,
                Descripcion = $"{cmd.Resumen}\n{cmd.Descripcion}",
                Color = cmd.ColorFamilia,
                Icono = cmd.Icono,
                Origen = cmd
            });
            SetStatus($"Añadido: {cmd.Nombre}");
        }

        public void AgregarComandoSeleccionado()
        {
            if (ComandoSeleccionado is null)
            {
                SetStatus("Ningún comando seleccionado");
                return;
            }
            AgregarComando(ComandoSeleccionado);
        }

        public void EliminarToken(Token token)
        {
            LineaActiva.QuitarToken(token);
            SetStatus($"Eliminado: {token.Texto}");
        }

        public void LimpiarTokens()
        {
            LineaActiva.Limpiar();
            SetStatus("Línea limpiada");
        }

        // ==== Helpers ====

        public void SetStatus(string mensaje) => Status = mensaje;

        private bool FiltroComando(object obj)
        {
            if (string.IsNullOrWhiteSpace(Filtro)) return true;
            if (obj is not Comando c) return false;
            var q = Filtro.Trim();
            return c.Nombre.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                || c.Resumen.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                || c.Familia.Contains(q, System.StringComparison.OrdinalIgnoreCase);
        }

        private void CargarComandos()
        {
            var ruta = Path.Combine(System.AppContext.BaseDirectory, "Data", "comandos.json");
            if (!File.Exists(ruta))
                ruta = Path.Combine(System.AppContext.BaseDirectory, "comandos.json");
            if (!File.Exists(ruta)) return;

            var json = File.ReadAllText(ruta);
            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var lista = JsonSerializer.Deserialize<System.Collections.Generic.List<Comando>>(json, opciones);
            if (lista == null) return;

            foreach (var c in lista.OrderBy(c => c.Familia).ThenBy(c => c.Nombre))
                Comandos.Add(c);
        }

        /// <summary>
        /// Añade una ruta (archivo o carpeta) a la línea activa, entre comillas
        /// si contiene espacios (como haría CMD).
        /// </summary>
        public void AgregarRuta(string ruta)
        {
            var texto = ruta.Contains(' ') ? $"\"{ruta}\"" : ruta;
            var esCarpeta = System.IO.Directory.Exists(ruta);

            var token = new Token
            {
                Tipo = TipoToken.Ruta,
                Texto = texto,
                Descripcion = esCarpeta
                    ? $"Carpeta: {ruta}"
                    : $"Archivo: {ruta}",
                Color = "DEEBF7",             // azul claro, distingue de comandos y parámetros
                Icono = esCarpeta ? "📁" : "📄",
                Origen = ruta
            };

            LineaActiva.AgregarToken(token);
            SetStatus($"Añadida ruta: {ruta}");
        }

        // ==== INotifyPropertyChanged ====

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? nombre = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
        // ==== Ejecución ====

        /// <summary>Simula la línea activa sin ejecutarla. Solo actúa con comandos peligrosos.</summary>
        public void Simular()
        {
            var r = SimuladorComando.Simular(LineaActiva);

            if (!r.Aplica)
            {
                SetStatus("Este comando no necesita simulación");
                Salida.Clear();
                Salida.Add("✔ Este comando no está marcado como peligroso.");
                Salida.Add("No hay nada que simular; puedes ejecutarlo directamente.");
                return;
            }

            Salida.Clear();
            Salida.Add($"🔍 SIMULACIÓN DE: {r.Comando}");
            Salida.Add(new string('─', 60));
            Salida.Add("");
            foreach (var linea in r.Resumen.Split('\n'))
                Salida.Add(linea);
            Salida.Add("");

            if (r.Afectados.Count > 0)
            {
                Salida.Add($"Elementos afectados ({r.Afectados.Count}):");
                Salida.Add(new string('─', 60));
                foreach (var item in r.Afectados)
                    Salida.Add(item);
                Salida.Add("");
            }

            Salida.Add(new string('─', 60));
            Salida.Add("Nada se ha ejecutado todavía.");
            SetStatus($"Simulación completada: {r.Afectados.Count} elemento(s) afectado(s)");
        }



        /// <summary>Ejecuta la línea activa y captura la salida en el panel inferior.</summary>
        public async Task EjecutarAsync()
        {
            if (EstaEjecutando) return;

            var comando = LineaActiva.Texto;
            if (string.IsNullOrWhiteSpace(comando))
            {
                SetStatus("Nada que ejecutar");
                return;
            }

            Salida.Clear();
            AgregarSalida($"> {comando}");
            AgregarSalida("");

            EstaEjecutando = true;
            MensajeCmd = "EJECUTANDO...";
            SetStatus("Ejecutando...");

            try
            {
                await EjecutarInternoAsync(comando);
            }
            catch (Exception ex)
            {
                AgregarSalida($"ERROR: {ex.Message}");
                MensajeCmd = "ERROR AL EJECUTAR";
                SetStatus("Error");
            }
            finally
            {
                EstaEjecutando = false;
                _procesoActual = null;
            }
        }

        private async Task EjecutarInternoAsync(string comando)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                // chcp 65001 cambia la codificación a UTF-8 para que los
                // acentos se lean correctamente desde nuestra lectura UTF-8.
                Arguments = "/c chcp 65001 >nul & " + comando,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = Environment.CurrentDirectory,
            };

            using var proceso = new Process { StartInfo = psi };
            _procesoActual = proceso;

            proceso.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    InvokeUI(() => AgregarSalida(e.Data));
            };
            proceso.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    InvokeUI(() => AgregarSalida("ERR: " + e.Data));
            };

            proceso.Start();
            proceso.BeginOutputReadLine();
            proceso.BeginErrorReadLine();

            await proceso.WaitForExitAsync();

            AgregarSalida("");
            AgregarSalida($"[Proceso terminado con código {proceso.ExitCode}]");

            MensajeCmd = proceso.ExitCode == 0
                ? "EJECUTADO (código 0)"
                : $"EJECUTADO (código {proceso.ExitCode})";

            SetStatus(proceso.ExitCode == 0
                ? "Comando ejecutado correctamente"
                : $"El comando devolvió código {proceso.ExitCode}");
        }

        /// <summary>Detiene el proceso en curso si lo hay.</summary>
        public void DetenerEjecucion()
        {
            if (_procesoActual == null || _procesoActual.HasExited) return;

            try
            {
                _procesoActual.Kill(entireProcessTree: true);
                AgregarSalida("");
                AgregarSalida("[Proceso cancelado por el usuario]");
                MensajeCmd = "CANCELADO";
                SetStatus("Ejecución detenida");
            }
            catch (Exception ex)
            {
                AgregarSalida($"ERROR al detener: {ex.Message}");
            }
        }

        // ==== Helpers ====

        private void AgregarSalida(string linea) => Salida.Add(linea);

        /// <summary>Ejecuta una acción en el hilo de la UI, venga de donde venga.</summary>
        private static void InvokeUI(Action accion)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                accion();
            else
                dispatcher.Invoke(accion);
        }
    }
}