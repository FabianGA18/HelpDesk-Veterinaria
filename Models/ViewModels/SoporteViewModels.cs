using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Models.ViewModels
{
    /// <summary>Bandeja de soporte (HU-06) e historial de cerrados.</summary>
    public class BandejaViewModel
    {
        public List<TiqueteFilaViewModel> Tiquetes { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();

        /// <summary>Filtros aplicados, para marcar la pestaña y los combos seleccionados.</summary>
        public EstadoTiquete? Estado { get; set; }
        public PrioridadTiquete? Prioridad { get; set; }
        public int? CategoriaId { get; set; }
        public string? Busqueda { get; set; }

        public bool HayFiltros => Estado is not null || Prioridad is not null || CategoriaId is not null
                                  || !string.IsNullOrWhiteSpace(Busqueda);
    }

    /// <summary>Detalle del tiquete con las opciones de gestión de soporte (HU-07, HU-08, HU-09).</summary>
    public class GestionTiqueteViewModel
    {
        public TiqueteDetalleViewModel Tiquete { get; set; } = new();
        public List<Usuario> Agentes { get; set; } = new();
        public List<Categoria> Categorias { get; set; } = new();

        /// <summary>Estados a los que se puede pasar con un botón (RF-12).</summary>
        public IReadOnlyList<EstadoTiquete> Transiciones { get; set; } = Array.Empty<EstadoTiquete>();

        public bool EsCerrado => Tiquete.Estado == EstadoTiquete.Cerrado;

        /// <summary>RF-13: la solución se registra con el tiquete «En progreso».</summary>
        public bool PuedeRegistrarSolucion => Tiquete.Estado == EstadoTiquete.EnProgreso;
    }
}
