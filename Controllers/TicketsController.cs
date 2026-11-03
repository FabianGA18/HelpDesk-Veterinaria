using Microsoft.AspNetCore.Mvc;

namespace Sistema_Gestión_Citas.Controllers
{
    public class TicketsController : Controller
    {
        public IActionResult Crear()
        {
            return View();
        }

        public IActionResult Detalle()
        {
            return View();
        }
        //tickets propios
        public IActionResult Mis()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Crear(string? asunto, int? categoriaId, string? descripcion)
        {
            return RedirectToAction("Mis");
        }

        [HttpPost]
        public IActionResult Comentar(int id)
        {
            return RedirectToAction("Detalle", new { id });
        }

        [HttpPost]
        public IActionResult Cerrar(int id)
        {
            return RedirectToAction("Mis");
        }
    }
}
