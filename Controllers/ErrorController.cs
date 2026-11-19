using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sistema_Gestión_Citas.Controllers
{
    /// <summary>
    /// Páginas de error amigables. Program.cs re-ejecuta aquí las respuestas 4xx/5xx que no traen
    /// contenido (por ejemplo, NotFound() o el rechazo del token antifalsificación), para que el
    /// usuario vea una pantalla del sistema en lugar de una página en blanco.
    /// </summary>
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("Error/{codigo:int}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Estado(int codigo)
        {
            var (titulo, mensaje) = codigo switch
            {
                400 => ("Solicitud no válida",
                        "No pudimos procesar el envío. Puede que el formulario haya expirado o que se haya enviado sin sus datos de seguridad. Vuelve a abrir la página e inténtalo de nuevo."),
                403 => ("Acceso no permitido", "No tienes permiso para realizar esta acción."),
                404 => ("Página no encontrada",
                        "El tiquete, archivo o página que buscas no existe o no tienes acceso a él."),
                _ => ("Ocurrió un problema", "No pudimos completar la solicitud. Inténtalo de nuevo en unos minutos."),
            };
            ViewData["Codigo"] = codigo;
            ViewData["Titulo"] = titulo;
            ViewData["Mensaje"] = mensaje;
            Response.StatusCode = codigo;
            return View();
        }
    }
}
