using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sistema_Gestión_Citas.Services;

namespace Sistema_Gestión_Citas.Infraestructura
{
    /// <summary>
    /// Revalida la cookie en cada solicitud (RNF-02): si el administrador desactivó al usuario
    /// o le cambió el rol, la sesión se cierra y debe volver a iniciar sesión.
    /// </summary>
    public static class ValidadorSesion
    {
        public static async Task ValidarAsync(CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;
            var idTexto = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (principal is null || !int.TryParse(idTexto, out var id))
            {
                await RechazarAsync(context);
                return;
            }

            var usuarios = context.HttpContext.RequestServices.GetRequiredService<IUsuarioService>();
            var usuario = await usuarios.ObtenerActivoAsync(id);
            if (usuario is null || usuario.Rol.Nombre != principal.FindFirstValue(ClaimTypes.Role))
                await RechazarAsync(context);
        }

        private static async Task RechazarAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
