namespace Sistema_Gestión_Citas.Models.Entidades
{
    /// <summary>
    /// Archivo adjunto a un tiquete (HU-03, RF-04). El archivo se guarda en disco con un
    /// nombre generado (GUID) y aquí se conserva el nombre original para mostrarlo y descargarlo.
    /// </summary>
    public class Evidencia
    {
        public int Id { get; set; }

        public int TiqueteId { get; set; }
        public Tiquete Tiquete { get; set; } = null!;

        public int SubidoPorId { get; set; }
        public Usuario SubidoPor { get; set; } = null!;

        /// <summary>Nombre del archivo tal como lo subió el usuario.</summary>
        public string NombreOriginal { get; set; } = string.Empty;

        /// <summary>Nombre con el que se guardó en disco (GUID + extensión).</summary>
        public string NombreArchivo { get; set; } = string.Empty;

        public string TipoContenido { get; set; } = string.Empty;
        public long TamanoBytes { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.Now;
    }
}
