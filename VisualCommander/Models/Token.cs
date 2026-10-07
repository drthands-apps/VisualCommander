namespace VisualCommander.Models
{
    /// <summary>
    /// Tipo de token. Determina cómo se pinta y qué acepta.
    /// </summary>
    public enum TipoToken
    {
        Comando,     // dir, copy, robocopy...
        Parametro,   // /S, -t, /A:D ...
        Operador,    // |, &&, >, >>...
        Variable,    // %TEMP%, %USERPROFILE%...
        Constante,   // NUL, CON, PRN...
        Ruta,        // "C:\Users\yo\docs"
        Texto,       // cualquier texto libre
        Separador    // para huecos estructurales (no se pinta en la línea)
    }

    /// <summary>
    /// Una pieza atómica del comando que el usuario va componiendo.
    /// La suma ordenada de sus Textos forma la línea de comando final.
    /// </summary>
    public class Token
    {
        /// <summary>Tipo del token.</summary>
        public TipoToken Tipo { get; set; }

        /// <summary>Texto literal que se escribe en la línea de comando.</summary>
        public string Texto { get; set; } = "";

        /// <summary>Descripción corta, para tooltips y ayuda.</summary>
        public string? Descripcion { get; set; }

        /// <summary>Color del bloque en formato hex SIN "#" (ej. "F4B400").</summary>
        public string Color { get; set; } = "CCCCCC";

        /// <summary>Icono o emoji opcional.</summary>
        public string? Icono { get; set; }

        /// <summary>
        /// Referencia opcional al objeto de origen (Comando, Parametro...).
        /// Nos servirá luego para validar y para reconstruir la línea al editar.
        /// </summary>
        public object? Origen { get; set; }

        /// <summary>Texto que se muestra dentro del bloque en la UI.</summary>
        public string TextoMostrado =>
            Tipo == TipoToken.Separador ? "␣" : Texto;
    }
}