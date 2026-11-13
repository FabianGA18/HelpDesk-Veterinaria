using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Models.ViewModels
{
    /// <summary>Fila de una lista de tiquetes («Mis tiquetes», bandeja de soporte).</summary>
    public class TiqueteFilaViewModel
    {
        public int Id { get; set; }
        public string Asunto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string? Solicitante { get; set; }
        public string? Responsable { get; set; }
        public EstadoTiquete Estado { get; set; }
        public PrioridadTiquete? Prioridad { get; set; }
        public DateTime FechaCreacion { get; set; }

        public static TiqueteFilaViewModel Desde(Tiquete t) => new()
        {
            Id = t.Id,
            Asunto = t.Asunto,
            Categoria = t.Categoria?.Nombre ?? string.Empty,
            Solicitante = t.Solicitante?.Nombre,
            Responsable = t.Responsable?.Nombre,
            Estado = t.Estado,
            Prioridad = t.Prioridad,
            FechaCreacion = t.FechaCreacion
        };
    }

    public class EventoHistorialViewModel
    {
        public string Usuario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public TipoEvento Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }

    public class EvidenciaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public long TamanoBytes { get; set; }

        public string Tamano => TamanoBytes >= 1024 * 1024
            ? $"{TamanoBytes / (1024.0 * 1024):0.#} MB"
            : $"{Math.Max(1, TamanoBytes / 1024)} KB";
    }

    public class TiqueteDetalleViewModel
    {
        public int Id { get; set; }
        public string Asunto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int CategoriaId { get; set; }
        public string Solicitante { get; set; } = string.Empty;
        public string? Responsable { get; set; }
        public int? ResponsableId { get; set; }
        public EstadoTiquete Estado { get; set; }
        public PrioridadTiquete? Prioridad { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? Solucion { get; set; }

        public List<EvidenciaViewModel> Evidencias { get; set; } = new();

        /// <summary>Más reciente primero.</summary>
        public List<EventoHistorialViewModel> Historial { get; set; } = new();

        /// <summary>RF-06: se puede comentar mientras el tiquete no esté cerrado.</summary>
        public bool PuedeComentar => Estado != EstadoTiquete.Cerrado;

        /// <summary>RF-07: el solicitante solo puede cerrar tiquetes en estado «Resuelto».</summary>
        public bool PuedeCerrar => Estado == EstadoTiquete.Resuelto;

        public static TiqueteDetalleViewModel Desde(Tiquete t) => new()
        {
            Id = t.Id,
            Asunto = t.Asunto,
            Descripcion = t.Descripcion,
            Categoria = t.Categoria.Nombre,
            CategoriaId = t.CategoriaId,
            Solicitante = t.Solicitante.Nombre,
            Responsable = t.Responsable?.Nombre,
            ResponsableId = t.ResponsableId,
            Estado = t.Estado,
            Prioridad = t.Prioridad,
            FechaCreacion = t.FechaCreacion,
            Solucion = t.Solucion,
            Evidencias = t.Evidencias
                .OrderBy(e => e.Id)
                .Select(e => new EvidenciaViewModel { Id = e.Id, Nombre = e.NombreOriginal, TamanoBytes = e.TamanoBytes })
                .ToList(),
            Historial = t.Historial
                .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
                .Select(h => new EventoHistorialViewModel
                {
                    Usuario = h.Usuario.Nombre,
                    Fecha = h.Fecha,
                    Tipo = h.Tipo,
                    Descripcion = h.Descripcion
                })
                .ToList()
        };
    }
}
