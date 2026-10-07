using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace VisualCommander.Models
{
    /// <summary>
    /// Representa una línea de comandos compuesta por una secuencia
    /// ordenada de tokens. Sabe calcular su propio texto, gestionar
    /// su comando activo y construir el panel de modificadores.
    /// </summary>
    public class LineaComando : INotifyPropertyChanged
    {
        // ==== Estado ====

        /// <summary>Tokens que forman esta línea.</summary>
        public ObservableCollection<Token> Tokens { get; } = new();

        /// <summary>Modificadores del comando activo, listos para la UI.</summary>
        public ObservableCollection<ParametroVista> Modificadores { get; } = new();

        /// <summary>
        /// Texto plano resultante: une los textos de todos los tokens
        /// (excepto los separadores) con espacios.
        /// </summary>
        public string Texto =>
            string.Join(" ", Tokens.Where(t => t.Tipo != TipoToken.Separador)
                                   .Select(t => t.Texto));

        /// <summary>Texto para mostrar en selectores; evita cadenas vacías.</summary>
        public string DisplayText =>
            string.IsNullOrWhiteSpace(Texto) ? "(línea vacía)" : Texto;

        private Token? _comandoActivo;
        /// <summary>Último comando de la línea (cerrado por operadores).</summary>
        public Token? ComandoActivo
        {
            get => _comandoActivo;
            private set
            {
                if (ReferenceEquals(_comandoActivo, value)) return;
                _comandoActivo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TituloModificadores));
                ReconstruirModificadores();
            }
        }

        /// <summary>Título que se muestra sobre el panel de modificadores.</summary>
        public string TituloModificadores =>
            ComandoActivo is null
                ? "MODIFICADORES (sin comando activo)"
                : $"MODIFICADORES DE: {ComandoActivo.Texto.ToUpperInvariant()}";

        // ==== Constructor ====

        public LineaComando()
        {
            Tokens.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Texto));
                OnPropertyChanged(nameof(DisplayText));
                RecalcularComandoActivo();
            };
        }

        // ==== API pública ====

        /// <summary>Añade un token al final de la línea.</summary>
        public void AgregarToken(Token token) => Tokens.Add(token);

        /// <summary>Inserta un token en una posición concreta (para el futuro).</summary>
        public void InsertarToken(Token token, int indice) => Tokens.Insert(indice, token);

        /// <summary>
        /// Quita un token. Si era un parámetro marcado, desmarca su checkbox
        /// y deja que el propio checkbox quite el token (evita doble borrado).
        /// </summary>
        public void QuitarToken(Token token)
        {
            if (token.Tipo == TipoToken.Parametro && token.Origen is Parametro p)
            {
                var pv = Modificadores.FirstOrDefault(m => m.Parametro == p);
                if (pv is { EstaSeleccionado: true })
                {
                    pv.EstaSeleccionado = false;
                    return; // el handler marcará la baja en Tokens
                }
            }
            Tokens.Remove(token);
        }

        /// <summary>Vacía la línea por completo.</summary>
        public void Limpiar()
        {
            Tokens.Clear();
            Modificadores.Clear();
            ComandoActivo = null;
        }

        // ==== Lógica interna ====

        /// <summary>
        /// Recalcula el comando activo: el último Token de tipo Comando que
        /// no esté "cerrado" por un operador.
        /// </summary>
        private void RecalcularComandoActivo()
        {
            Token? nuevo = null;
            for (int i = Tokens.Count - 1; i >= 0; i--)
            {
                var t = Tokens[i];
                if (t.Tipo == TipoToken.Operador)
                    break;                       // los operadores cierran el comando
                if (t.Tipo == TipoToken.Comando)
                {
                    nuevo = t;
                    break;
                }
            }
            ComandoActivo = nuevo;
        }

        /// <summary>Reconstruye la lista de modificadores desde el comando activo.</summary>
        private void ReconstruirModificadores()
        {
            // Desuscribimos los anteriores (evitamos fugas de memoria).
            foreach (var old in Modificadores)
                old.PropertyChanged -= ParametroVista_PropertyChanged;

            Modificadores.Clear();

            if (ComandoActivo?.Origen is not Comando cmd) return;

            foreach (var p in cmd.Parametros)
            {
                var pv = new ParametroVista(p);
                pv.PropertyChanged += ParametroVista_PropertyChanged;
                Modificadores.Add(pv);
            }
        }

        private void ParametroVista_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ParametroVista.EstaSeleccionado)) return;
            if (sender is not ParametroVista pv) return;

            if (pv.EstaSeleccionado)
                AgregarTokenParametro(pv);
            else
                QuitarTokenParametro(pv);
        }

        private void AgregarTokenParametro(ParametroVista pv)
        {
            if (pv.Parametro.Unico && Tokens.Any(t => ReferenceEquals(t.Origen, pv.Parametro)))
                return;

            Tokens.Add(new Token
            {
                Tipo = TipoToken.Parametro,
                Texto = pv.Nombre,
                Descripcion = pv.Descripcion,
                Color = "E2EFDA",
                Icono = "🔧",
                Origen = pv.Parametro
            });
        }

        private void QuitarTokenParametro(ParametroVista pv)
        {
            var token = Tokens.FirstOrDefault(t => ReferenceEquals(t.Origen, pv.Parametro));
            if (token != null)
                Tokens.Remove(token);
        }

        // ==== INotifyPropertyChanged ====

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? nombre = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    }
}