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

            // Cada vez que cambia la línea activa, forzamos el foco al editor.
            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.LineaActiva))
                    EnfocarEditorActivo();
            };

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
                ViewModel.LineaActiva.ConfirmarEstado();
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
                ViewModel.LineaActiva.ConfirmarEstado();
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
        {
            ViewModel.LineaActiva.ConfirmarEstado();
            ViewModel.CerrarYCrearSiguiente();
            EnfocarEditorActivo();
        }

        private void CerrarLinea_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LineaActiva.ConfirmarEstado();
            ViewModel.CerrarLineaActiva();
            EnfocarEditorActivo();
        }
        private void EliminarLinea_Click(object sender, RoutedEventArgs e)
            => ViewModel.EliminarLineaActiva();

        private async void Ejecutar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LineaActiva.ConfirmarEstado();
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
        private void CambiarDirectorio_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Seleccionar directorio de trabajo",
                InitialDirectory = ViewModel.DirectorioTrabajo
            };

            if (dialogo.ShowDialog() == true)
            {
                ViewModel.CambiarDirectorioTrabajo(dialogo.FolderName);
            }
        }
        private void Prompt_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(FormatoRuta)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Prompt_DragOver(object sender, DragEventArgs e)
        {
            // Igual que DragEnter; en WPF hay que gestionar ambos.
            e.Effects = e.Data.GetDataPresent(FormatoRuta)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Prompt_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(FormatoRuta)) return;
            if (e.Data.GetData(FormatoRuta) is not string ruta) return;

            // Solo aceptamos carpetas, no archivos.
            if (!System.IO.Directory.Exists(ruta))
            {
                ViewModel.SetStatus("Solo se puede cambiar el directorio a una carpeta");
                e.Handled = true;
                return;
            }

            ViewModel.CambiarDirectorioTrabajo(ruta);
            e.Handled = true;
        }
       

       

        private static bool TerminaEnOperador(LineaComando linea)
        {
            if (linea.Tokens.Count == 0) return false;
            var ultimo = linea.Tokens[^1].Texto;
            return ultimo is "|" or "&&" or "||" or "&";
        }

        private static bool EsComentario(LineaComando linea)
        {
            if (linea.Tokens.Count == 0) return false;
            var primero = linea.Tokens[0].Texto.ToLowerInvariant();
            return primero == "rem" || primero.StartsWith("::");
        }



        private void ParsearYReemplazar(TextBox tb, LineaComando linea)
        {
            var texto = tb.Text;
            if (string.IsNullOrWhiteSpace(texto)) return;

            var tokens = ComandoParser.Parsear(
                texto,
                ViewModel.Comandos,
                linea.DirectorioTrabajo);

            linea.ReemplazarCon(tokens);

            tb.CaretIndex = tb.Text.Length;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.CaretIndex = tb.Text.Length;
            }), System.Windows.Threading.DispatcherPriority.Background);

            ViewModel.SetStatus($"Línea {linea.NumeroLinea} parseada: {tokens.Count} token(s)");
        }
        private void LineaClic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: LineaComando linea }) return;

            if (ReferenceEquals(ViewModel.LineaActiva, linea)) return;

            // Confirmamos el estado de la línea que dejamos atrás.
            ViewModel.LineaActiva.ConfirmarEstado();

            ViewModel.LineaActiva = linea;
            e.Handled = true;
        }

        /// <summary>Busca un hijo visual del tipo T recursivamente.</summary>
        private static T? FindVisualChild<T>(DependencyObject padre) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(padre); i++)
            {
                var hijo = System.Windows.Media.VisualTreeHelper.GetChild(padre, i);
                if (hijo is T encontrado) return encontrado;
                var subHijo = FindVisualChild<T>(hijo);
                if (subHijo != null) return subHijo;
            }
            return null;
        }
        private void EditorComando_IsVisibleChanged(
    object sender, DependencyPropertyChangedEventArgs e)
        {
            // Solo nos interesa cuando pasa a Visible.
            if (e.NewValue is not true) return;

            if (sender is not TextBox tb) return;
            if (tb.DataContext is not LineaComando linea || !linea.EsActiva) return;

            // Damos el foco cuando el layout ya esté actualizado.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private void EnfocarEditorActivo()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var itemsControl = FindVisualChild<ItemsControl>(this);
                if (itemsControl == null) return;

                var generador = itemsControl.ItemContainerGenerator;
                foreach (var item in itemsControl.Items)
                {
                    if (item is not LineaComando linea || !linea.EsActiva) continue;

                    var contenedor = generador.ContainerFromItem(item) as DependencyObject;
                    if (contenedor == null) continue;

                    var tb = FindVisualChild<TextBox>(contenedor);
                    if (tb != null)
                    {
                        tb.Focus();
                        tb.CaretIndex = tb.Text.Length;
                    }
                    break;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private void CopiarLinea_Click(object sender, RoutedEventArgs e)
    => ViewModel.CopiarLineaActiva();

        private void CortarLinea_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CortarLineaActiva();
            EnfocarEditorActivo();
        }

        private void PegarLinea_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PegarLinea();
            EnfocarEditorActivo();
        }

        private void DuplicarLinea_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DuplicarLineaActiva();
            EnfocarEditorActivo();
        }
        private void MenuAcciones_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Si el foco está en un TextBox, primero gestionamos los atajos
            // que queremos interceptar aunque el foco esté dentro.
            var focoEnTextBox = Keyboard.FocusedElement is TextBox;

            // Ctrl+Z → deshacer
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                if (ViewModel.LineaActiva.Deshacer())
                    ViewModel.SetStatus("Deshecho");
                else
                    ViewModel.SetStatus("Nada que deshacer");
                e.Handled = true;
                return;
            }

            // Ctrl+Y (o Ctrl+Shift+Z) → rehacer
            if ((Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y) ||
                (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Z))
            {
                if (ViewModel.LineaActiva.Rehacer())
                    ViewModel.SetStatus("Rehecho");
                else
                    ViewModel.SetStatus("Nada que rehacer");
                e.Handled = true;
                return;
            }
            // Ctrl+D → duplicar línea
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D)
            {
                ViewModel.DuplicarLineaActiva();
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+↑/↓ → mover línea activa
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.Up)
                {
                    ViewModel.MoverActivaArriba();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Down)
                {
                    ViewModel.MoverActivaAbajo();
                    e.Handled = true;
                    return;
                }
            }

            // Ctrl+Enter → cerrar y crear nueva línea
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                ViewModel.CerrarYCrearSiguiente();
                EnfocarEditorActivo();
                e.Handled = true;
                return;
            }

            // ===== Atajos solo cuando el foco NO está en un TextBox =====

            if (focoEnTextBox) return;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.C: ViewModel.CopiarLineaActiva(); e.Handled = true; break;
                    case Key.X: ViewModel.CortarLineaActiva(); e.Handled = true; break;
                    case Key.V: ViewModel.PegarLinea(); e.Handled = true; break;
                }
            }
        }
        private void MoverArriba_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoverActivaArriba();
            EnfocarEditorActivo();
        }

        private void MoverAbajo_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MoverActivaAbajo();
            EnfocarEditorActivo();
        }
        private void EditorComando_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (tb.DataContext is not LineaComando linea) return;

            if (e.Key == Key.Space)
            {
                // Empujamos el texto al modelo para que se re-proyecten los tokens.
                // Escribimos el espacio primero, luego empujamos.
                EmpujarTextoAlModelo(tb, linea);
                return; // dejamos que el espacio se escriba normalmente
            }

            if (e.Key == Key.Enter)
            {
                EmpujarTextoAlModelo(tb, linea);
                linea.ConfirmarEstado();
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Escape)
{
    // Descartamos los cambios no confirmados: volvemos al texto del modelo.
    tb.Text = linea.Texto;
    tb.CaretIndex = tb.Text.Length;
    e.Handled = true;
    return;
}
        }

        private void EditorComando_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is LineaComando linea)
            {
                // Guardamos el estado al empezar a editar.
                linea.GuardarSnapshot();
            }
        }

        private void EditorComando_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (tb.DataContext is not LineaComando linea) return;

            EmpujarTextoAlModelo(tb, linea);
            linea.ConfirmarEstado();
        }

        /// <summary>
        /// Empuja el texto del TextBox al modelo. Solo si hay diferencia.
        /// </summary>
        private void EmpujarTextoAlModelo(TextBox tb, LineaComando linea)
        {
            if (tb.Text == linea.Texto) return;
            linea.ActualizarDesdeTexto(tb.Text);
            ViewModel.SetStatus($"Línea {linea.NumeroLinea} actualizada");
        }
    }
}