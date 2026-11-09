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
    }
}
