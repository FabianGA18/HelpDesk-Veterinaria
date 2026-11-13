using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public interface ITiqueteService
    {
        Task<List<Categoria>> ObtenerCategoriasActivasAsync();

        /// <summary>
        /// Crea un tiquete con estado «Nuevo», fecha y hora actuales (RF-02, RF-03)
        /// y registra el evento de creación en el historial. Devuelve el número del tiquete.
        /// </summary>
        Task<Resultado<int>> CrearAsync(int solicitanteId, string asunto, int categoriaId, string descripcion);

        /// <summary>Tiquetes creados por el solicitante, más recientes primero (RF-05, RNF-03).</summary>
        Task<List<Tiquete>> ListarDelSolicitanteAsync(int solicitanteId);

        /// <summary>
        /// Detalle de un tiquete con categoría, usuarios e historial.
        /// Si se indica <paramref name="soloDelSolicitanteId"/>, solo lo devuelve si le pertenece (RNF-03).
        /// </summary>
        Task<Tiquete?> ObtenerDetalleAsync(int id, int? soloDelSolicitanteId = null);

        // ----- Soporte (iteración 2) -----

        /// <summary>Bandeja de soporte con filtros por estado, prioridad, categoría y búsqueda (RF-08).</summary>
        Task<List<Tiquete>> BuscarAsync(FiltroBandeja filtro);

        /// <summary>Usuarios activos que pueden ser responsables (soporte y administradores).</summary>
        Task<List<Usuario>> ObtenerAgentesAsync();

        /// <summary>Todas las categorías; las inactivas solo se muestran si el tiquete ya la tiene.</summary>
        Task<List<Categoria>> ObtenerCategoriasAsync();

        /// <summary>
        /// Cambia responsable (RF-09), prioridad (RF-10) y categoría (RF-11) y lo registra en el historial.
        /// Al asignar un tiquete «Nuevo» pasa a «Asignado»; al quitar el responsable de uno «Asignado» vuelve a «Nuevo».
        /// </summary>
        Task<Resultado> ActualizarAsync(int id, int actorId, int? responsableId, PrioridadTiquete? prioridad, int categoriaId);

        /// <summary>Cambia el estado siguiendo el flujo definido (RF-12).</summary>
        Task<Resultado> CambiarEstadoAsync(int id, int actorId, EstadoTiquete nuevoEstado);

        /// <summary>Registra la solución y pasa el tiquete de «En progreso» a «Resuelto» (RF-13).</summary>
        Task<Resultado> RegistrarSolucionAsync(int id, int actorId, string solucion);

        /// <summary>Agrega un comentario mientras el tiquete no esté cerrado (RF-06, RF-13).</summary>
        Task<Resultado> ComentarAsync(int id, int actorId, string comentario);
    }
}
