using System.Collections.Generic;
using System.Windows;
using VisualCommander.Models;

namespace VisualCommander
{
    public partial class ConfirmarEjecucionDialog : Window
    {
        /// <summary>¿El usuario confirmó la ejecución?</summary>
        public bool Confirmado { get; private set; }

        /// <summary>¿Marcó "no volver a preguntar"?</summary>
        public bool NoPreguntarMas => ChkNoPreguntar.IsChecked == true;

        public ConfirmarEjecucionDialog(
            string comando,
            string directorioTrabajo,
            ResultadoSimulacion simulacion)
        {
            InitializeComponent();

            TxtComando.Text = comando;
            TxtDirectorio.Text = directorioTrabajo;
            TxtResumen.Text = simulacion.Resumen;

            // Coloreamos el directorio si es crítico.
            if (EsDirectorioCritico(directorioTrabajo))
            {
                CajaDirectorio.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0xE0, 0xE0));
                CajaDirectorio.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xC0, 0x39, 0x2B));
                TxtDirectorio.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xA0, 0x20, 0x15));
                TxtDirectorio.FontWeight = FontWeights.Bold;
            }

            // Rellenamos la lista de afectados.
            if (simulacion.Afectados.Count > 0)
            {
                foreach (var item in simulacion.Afectados)
                    ListaAfectados.Items.Add(item);

                TituloAfectados.Text =
                    $"ELEMENTOS AFECTADOS ({simulacion.Afectados.Count}):";
            }
            else
            {
                TituloAfectados.Visibility = Visibility.Collapsed;
                ListaAfectados.Visibility = Visibility.Collapsed;
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = false;
            DialogResult = false;
            Close();
        }

        private void Ejecutar_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = true;
            DialogResult = true;
            Close();
        }

        private static bool EsDirectorioCritico(string ruta)
        {
            var dir = ruta.TrimEnd('\\').ToLowerInvariant();
            if (dir.Length == 2 && dir[1] == ':') return true; // raíz de unidad

            string[] criticos =
            {
                @"c:\windows",
                @"c:\program files",
                @"c:\program files (x86)",
                @"c:\programdata",
                @"c:\users\default"
            };
            foreach (var c in criticos)
                if (dir == c || dir.StartsWith(c + "\\")) return true;

            return false;
        }
    }
}