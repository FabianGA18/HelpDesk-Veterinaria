using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    /// <summary>Administración de categorías (HU-10, RF-14).</summary>
    public interface ICategoriaService
    {
        /// <summary>Todas las categorías con su cantidad de tiquetes abiertos (no cerrados).</summary>
        Task<List<(Categoria Categoria, int TiquetesActivos)>> ListarConConteoAsync();

        Task<Categoria?> ObtenerAsync(int id);

        Task<Resultado<int>> CrearAsync(string nombre, string? descripcion);

        /// <summary>Edita nombre y descripción, y activa o desactiva la categoría (no se eliminan).</summary>
        Task<Resultado> ActualizarAsync(int id, string nombre, string? descripcion, bool activa);
    }
}
