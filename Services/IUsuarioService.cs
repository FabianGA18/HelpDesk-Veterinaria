using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public interface IUsuarioService
    {
        /// <summary>
        /// Valida correo y contraseña. Devuelve el usuario (con su rol) si las credenciales
        /// son correctas y la cuenta está activa; si no, null.
        /// </summary>
        Task<Usuario?> ValidarCredencialesAsync(string correo, string password);

        // ----- Administración de usuarios y roles (HU-11, RF-15) -----

        /// <summary>Usuario activo con su rol, para revalidar la sesión en cada solicitud.</summary>
        Task<Usuario?> ObtenerActivoAsync(int id);

        Task<List<Usuario>> ListarAsync();
        Task<Usuario?> ObtenerAsync(int id);
        Task<List<Rol>> ListarRolesAsync();

        Task<Resultado<int>> CrearAsync(string nombre, string correo, string rol, string password);

        /// <summary>
        /// Edita nombre, correo, rol y estado. Si <paramref name="nuevaPassword"/> trae valor, la restablece.
        /// <paramref name="actorId"/> es el administrador que hace el cambio: no puede desactivarse ni quitarse el rol a sí mismo.
        /// </summary>
        Task<Resultado> ActualizarAsync(int id, int actorId, string nombre, string correo, string rol, bool activo, string? nuevaPassword);
    }
}
