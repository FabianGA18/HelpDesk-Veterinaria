using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Models.ViewModels;
using Sistema_Gestión_Citas.Services;

namespace Sistema_Gestión_Citas.Controllers
{
    /// <summary>HU-01: inicio y cierre de sesión con acceso según el rol.</summary>
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IUsuarioService _usuarios;

        public LoginController(IUsuarioService usuarios) => _usuarios = usuarios;

        [HttpGet]
        public IActionResult InicioSesión(string? returnUrl = null)
        {
            // Si ya hay sesión, se envía directo a la pantalla de su rol.
            if (User.Identity?.IsAuthenticated == true)
                return RedirigirSegunRol(User.ObtenerRol());

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InicioSesión(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Error"] = "Ingresa tu correo y contraseña.";
                return View(model);
            }

            var usuario = await _usuarios.ValidarCredencialesAsync(model.Usuario, model.Password);
            if (usuario is null)
            {
                // Mensaje genérico: no se revela si el correo existe o si la cuenta está desactivada.
                ViewData["Error"] = "Usuario o contraseña incorrectos.";
                model.Password = string.Empty;
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.Nombre),
                new(ClaimTypes.Email, usuario.Correo),
                new(ClaimTypes.Role, usuario.Rol.Nombre)
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return RedirigirSegunRol(usuario.Rol.Nombre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesión()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(InicioSesión));
        }

        /// <summary>RNF-02: se muestra cuando un usuario intenta entrar a una función de otro rol.</summary>
        [HttpGet]
        public IActionResult AccesoDenegado() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View("~/Views/Shared/Error.cshtml",
                new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        private IActionResult RedirigirSegunRol(string? rol) => rol switch
        {
            Roles.Administrador => RedirectToAction("Index", "Soporte"),
            Roles.Soporte => RedirectToAction("Index", "Soporte"),
            Roles.Solicitante => RedirectToAction("Mis", "Tickets"),
            _ => RedirectToAction(nameof(AccesoDenegado))
        };
    }
}
