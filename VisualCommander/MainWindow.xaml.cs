using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualCommander.Models;
using VisualCommander.ViewModels;


namespace VisualCommander
{
    public partial class MainWindow : Window
    {

        private const string FormatoComando = "VisualCommander.Comando";
        private const string FormatoParametro = "VisualCommander.Parametro";
        private const string FormatoRuta = "VisualCommander.Ruta";
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();



            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            // Auto-scroll del panel de salida cada vez que se añade una línea.
            ViewModel.Salida.CollectionChanged += (_, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add
                    && e.NewItems is { Count: > 0 })
                {
                    ListaSalida.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
                }
            };
        }
        private Point _posInicialDrag;

        private void ListaComandos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Guardamos dónde empezó el clic. Lo usaremos para medir el umbral.
            _posInicialDrag = e.GetPosition(null);
        }

        private void ListaComandos_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Solo arrastramos si el botón izquierdo sigue pulsado.
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // Solo si hay un comando seleccionado.
            if (ViewModel.ComandoSeleccionado is null) return;

            // ¿Ha superado el umbral mínimo de movimiento?
            var posActual = e.GetPosition(null);
            var delta = posActual - _posInicialDrag;

            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // Empaquetamos el comando y arrancamos el drag.
            var datos = new DataObject(FormatoComando, ViewModel.ComandoSeleccionado);
            DragDrop.DoDragDrop((DependencyObject)sender, datos, DragDropEffects.Copy);
        }

        private void ListaComandos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.AgregarComandoSeleccionado();
        }

        private void ListaComandos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.AgregarComandoSeleccionado();
                e.Handled = true; // evita que se propague
            }
        }
        private void EliminarToken_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Token token })
            {
                ViewModel.EliminarToken(token);
                e.Handled = true;  // evita que el clic se propague al Border padre
            }
        }
        private void PanelBloques_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DeterminarEfecto(e);
            e.Handled = true;
        }

        private void PanelBloques_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DeterminarEfecto(e);
            e.Handled = true;
        }

        private void PanelBloques_DragLeave(object sender, DragEventArgs e)
        {
            // Reservado para feedback visual futuro.
        }

        private void PanelBloques_Drop(object sender, DragEventArgs e)
        {
            // ¿Es un comando?
            if (e.Data.GetDataPresent(FormatoComando) &&
                e.Data.GetData(FormatoComando) is Comando cmd)
            {
                ViewModel.AgregarComando(cmd);
                e.Handled = true;
                return;
            }

            // ¿Es un parámetro?
            if (e.Data.GetDataPresent(FormatoParametro) &&
                e.Data.GetData(FormatoParametro) is ParametroVista pv)
            {
                // Regla: solo aceptamos parámetros si hay un comando activo.
                if (ViewModel.LineaActiva.ComandoActivo is null)
                {
                    ViewModel.SetStatus("Primero añade un comando para poder añadirle modificadores");
                    e.Handled = true;
                    return;
                }
                

                // Marcamos el checkbox; esto dispara automáticamente
                // ParametroVista_PropertyChanged → AgregarTokenParametro.
                pv.EstaSeleccionado = true;
                e.Handled = true;
            }
            // ¿Es una ruta del árbol de archivos?
            if (e.Data.GetDataPresent(FormatoRuta) &&
                e.Data.GetData(FormatoRuta) is string ruta)
            {
                if (ViewModel.LineaActiva.ComandoActivo is null)
                {
                    ViewModel.SetStatus("Primero añade un comando para poder añadirle una ruta");
                    e.Handled = true;
                    return;
                }

                ViewModel.AgregarRuta(ruta);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Decide el efecto del cursor según el formato arrastrado y el estado actual.
        /// </summary>
        private DragDropEffects DeterminarEfecto(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(FormatoComando))
                return DragDropEffects.Copy;

            if (e.Data.GetDataPresent(FormatoParametro))
            {
                return ViewModel.LineaActiva.ComandoActivo is null
                    ? DragDropEffects.None
                    : DragDropEffects.Copy;
            }

            if (e.Data.GetDataPresent(FormatoRuta))
            {
                // Aceptamos rutas si hay un comando activo (tiene sentido: la ruta
                // es un argumento de un comando).
                return ViewModel.LineaActiva.ComandoActivo is null
                    ? DragDropEffects.None
                    : DragDropEffects.Copy;
            }

            return DragDropEffects.None;
        }
        private Point _posInicialDragMod;

        private void Modificador_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _posInicialDragMod = e.GetPosition(null);
        }

        private void Modificador_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // Necesitamos que el Border tenga un ParametroVista en su DataContext.
            if (sender is not FrameworkElement { DataContext: ParametroVista pv }) return;

            // Umbral mínimo de movimiento.
            var posActual = e.GetPosition(null);
            var delta = posActual - _posInicialDragMod;

            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            var datos = new DataObject(FormatoParametro, pv);
            DragDrop.DoDragDrop((DependencyObject)sender, datos, DragDropEffects.Copy);
        }
        private void NuevaLinea_Click(object sender, RoutedEventArgs e)
    => ViewModel.NuevaLinea();

        private void EliminarLinea_Click(object sender, RoutedEventArgs e)
            => ViewModel.EliminarLineaActiva();

        private async void Ejecutar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.EjecutarAsync();
        }

        private void Detener_Click(object sender, RoutedEventArgs e)
            => ViewModel.DetenerEjecucion();
        private void Simular_Click(object sender, RoutedEventArgs e)
    => ViewModel.Simular();
        private void ArbolArchivos_Loaded(object sender, RoutedEventArgs e)
        {
            if (ArbolArchivos.Items.Count > 0) return; // solo poblar la primera vez

            var raiz = new TreeViewItem { Header = "💻 Este equipo", IsExpanded = true };

            foreach (var drive in System.IO.DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                string etiqueta = string.IsNullOrEmpty(drive.VolumeLabel)
                    ? drive.Name
                    : $"{drive.Name}  ({drive.VolumeLabel})";

                var nodoDrive = new TreeViewItem
                {
                    Header = "💽 " + etiqueta,
                    Tag = drive.RootDirectory.FullName
                };
                nodoDrive.Items.Add(new TreeViewItem { Header = "Cargando..." });
                nodoDrive.Expanded += NodoDrive_Expanded;
                raiz.Items.Add(nodoDrive);
            }

            ArbolArchivos.Items.Add(raiz);
        }

        private void NodoDrive_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem nodo) return;
            if (nodo.Items.Count != 1) return; // ya expandido antes
            if (nodo.Items[0] is not TreeViewItem placeholder) return;
            if (placeholder.Header as string != "Cargando...") return;

            nodo.Items.Clear();
            if (nodo.Tag is not string ruta) return;

            try
            {
                foreach (var dir in System.IO.Directory.EnumerateDirectories(ruta))
                {
                    var hijo = new TreeViewItem
                    {
                        Header = "📁 " + System.IO.Path.GetFileName(dir),
                        Tag = dir
                    };
                    hijo.Items.Add(new TreeViewItem { Header = "Cargando..." });
                    hijo.Expanded += NodoDrive_Expanded;
                    nodo.Items.Add(hijo);
                }
            }
            catch
            {
                nodo.Items.Add(new TreeViewItem { Header = "⛔ Acceso denegado" });
            }
        }
        private Point _posInicialDragArbol;

        private void ArbolArchivos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _posInicialDragArbol = e.GetPosition(null);
        }

        private void ArbolArchivos_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // Umbral mínimo de movimiento.
            var posActual = e.GetPosition(null);
            var delta = posActual - _posInicialDragArbol;

            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            // ¿Qué nodo está bajo el cursor?
            var nodoOrigen = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (nodoOrigen?.Tag is not string ruta || string.IsNullOrWhiteSpace(ruta))
                return;

            // Empaquetamos la ruta y arrancamos el drag.
            var datos = new DataObject(FormatoRuta, ruta);
            DragDrop.DoDragDrop(nodoOrigen, datos, DragDropEffects.Copy);
        }

        /// <summary>Sube por el árbol visual buscando un ancestro del tipo T.</summary>
        private static T? FindAncestor<T>(DependencyObject? actual) where T : DependencyObject
        {
            while (actual != null)
            {
                if (actual is T encontrado) return encontrado;
                actual = System.Windows.Media.VisualTreeHelper.GetParent(actual);
            }
            return null;
        }
    }
}