namespace Sistema_Gestión_Citas.Models.Entidades
{
    public class Tiquete
    {
        /// <summary>Número único del tiquete (RF-03). Es identidad y empieza en 1000.</summary>
        public int Id { get; set; }

        public string Asunto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public EstadoTiquete Estado { get; set; } = EstadoTiquete.Nuevo;

        /// <summary>Null mientras soporte no la haya establecido.</summary>
        public PrioridadTiquete? Prioridad { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
        public DateTime? FechaCierre { get; set; }

        /// <summary>Solución aplicada, documentada por soporte (RF-13).</summary>
        public string? Solucion { get; set; }

        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; } = null!;

        public int SolicitanteId { get; set; }
        public Usuario Solicitante { get; set; } = null!;

        public int? ResponsableId { get; set; }
        public Usuario? Responsable { get; set; }

        public ICollection<HistorialTiquete> Historial { get; set; } = new List<HistorialTiquete>();
        public ICollection<Evidencia> Evidencias { get; set; } = new List<Evidencia>();
    }
}
