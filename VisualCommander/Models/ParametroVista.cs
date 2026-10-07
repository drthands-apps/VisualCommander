using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VisualCommander.Models
{
    /// <summary>
    /// Envuelve un Parametro para la UI, añadiendo el estado "marcado"
    /// con notificación de cambios. El Parametro original se conserva
    /// intacto en la propiedad Parametro.
    /// </summary>
    public class ParametroVista : INotifyPropertyChanged
    {
        /// <summary>El parámetro original del catálogo.</summary>
        public Parametro Parametro { get; }

        /// <summary>¿Está marcado por el usuario actualmente?</summary>
        private bool _estaSeleccionado;
        public bool EstaSeleccionado
        {
            get => _estaSeleccionado;
            set
            {
                if (_estaSeleccionado == value) return;
                _estaSeleccionado = value;
                OnPropertyChanged();
            }
        }

        /// <summary>¿Está actualmente gris / deshabilitado por incompatibilidad?</summary>
        private bool _estaBloqueado;
        public bool EstaBloqueado
        {
            get => _estaBloqueado;
            set
            {
                if (_estaBloqueado == value) return;
                _estaBloqueado = value;
                OnPropertyChanged();
            }
        }

        // Atajos cómodos hacia el Parametro subyacente.
        public string Nombre => Parametro.Nombre;
        public string Descripcion => Parametro.Descripcion;
        public TipoParametro Tipo => Parametro.Tipo;
        public bool Requerido => Parametro.Requerido;

        public ParametroVista(Parametro parametro)
        {
            Parametro = parametro;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? nombre = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
    }
}