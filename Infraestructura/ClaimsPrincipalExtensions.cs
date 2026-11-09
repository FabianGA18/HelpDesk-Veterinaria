using System.Security.Claims;

namespace Sistema_Gestión_Citas.Infraestructura
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Id del usuario autenticado (claim NameIdentifier).</summary>
        public static int ObtenerUsuarioId(this ClaimsPrincipal user)
        {
            var valor = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(valor, out var id)
                ? id
                : throw new InvalidOperationException("El usuario no está autenticado.");
        }

        public static string? ObtenerRol(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role);
    }
}
