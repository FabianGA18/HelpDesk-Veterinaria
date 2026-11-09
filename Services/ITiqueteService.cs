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
    }
}
