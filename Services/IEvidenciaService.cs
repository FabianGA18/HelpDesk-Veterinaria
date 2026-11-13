using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public interface IEvidenciaService
    {
        /// <summary>
        /// Valida los archivos según RNF-06: solo PDF, JPG o PNG (por extensión y por contenido)
        /// y máximo 10 MB cada uno. Devuelve la lista de errores (vacía si todo es válido).
        /// </summary>
        List<string> Validar(IReadOnlyCollection<IFormFile> archivos);

        /// <summary>Guarda los archivos (ya validados) en disco y los registra en el tiquete y su historial.</summary>
        Task<Resultado> AdjuntarAsync(int tiqueteId, int usuarioId, IReadOnlyCollection<IFormFile> archivos);

        /// <summary>Evidencia con su tiquete (para revisar permisos antes de descargar).</summary>
        Task<Evidencia?> ObtenerAsync(int id);

        /// <summary>Abre el archivo guardado para descargarlo; null si ya no existe en disco.</summary>
        Stream? AbrirArchivo(Evidencia evidencia);
    }
}
