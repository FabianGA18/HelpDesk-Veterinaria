using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    /// <summary>
    /// Máquina de estados del tiquete (RF-12):
    /// Nuevo → Asignado → En progreso ⇄ En espera; En progreso → Resuelto → Cerrado.
    /// Un tiquete «Resuelto» puede reabrirse (vuelve a «En progreso») si el problema persiste.
    /// </summary>
    public static class FlujoEstados
    {
        private static readonly Dictionary<EstadoTiquete, EstadoTiquete[]> Transiciones = new()
        {
            [EstadoTiquete.Nuevo] = new[] { EstadoTiquete.Asignado },
            [EstadoTiquete.Asignado] = new[] { EstadoTiquete.EnProgreso, EstadoTiquete.Nuevo },
            [EstadoTiquete.EnProgreso] = new[] { EstadoTiquete.EnEspera, EstadoTiquete.Resuelto },
            [EstadoTiquete.EnEspera] = new[] { EstadoTiquete.EnProgreso },
            [EstadoTiquete.Resuelto] = new[] { EstadoTiquete.Cerrado, EstadoTiquete.EnProgreso },
            [EstadoTiquete.Cerrado] = Array.Empty<EstadoTiquete>()
        };

        /// <summary>
        /// Transiciones que se hacen por otras acciones y no con el botón «Cambiar estado»:
        /// Asignado/Nuevo (asignar o quitar responsable), Resuelto (registrar solución)
        /// y Cerrado (lo hace el solicitante).
        /// </summary>
        private static readonly HashSet<EstadoTiquete> SoloPorAccionEspecial = new()
        {
            EstadoTiquete.Nuevo, EstadoTiquete.Asignado, EstadoTiquete.Resuelto, EstadoTiquete.Cerrado
        };

        public static bool EsValida(EstadoTiquete desde, EstadoTiquete hacia) =>
            Transiciones.TryGetValue(desde, out var destinos) && destinos.Contains(hacia);

        /// <summary>Estados a los que soporte puede mover el tiquete con «Cambiar estado».</summary>
        public static IReadOnlyList<EstadoTiquete> TransicionesManuales(EstadoTiquete desde) =>
            Transiciones[desde].Where(e => !SoloPorAccionEspecial.Contains(e)).ToList();
    }
}
