using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    /// <summary>Filtros de la bandeja de soporte (RF-08, RF-16).</summary>
    public class FiltroBandeja
    {
        /// <summary>Null = todos los estados abiertos (todo menos «Cerrado»).</summary>
        public EstadoTiquete? Estado { get; set; }
        public PrioridadTiquete? Prioridad { get; set; }
        public int? CategoriaId { get; set; }

        /// <summary>Texto a buscar en el asunto, o número de tiquete (con o sin «#»).</summary>
        public string? Busqueda { get; set; }
    }
}
