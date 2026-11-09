using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Controllers
{
    // RNF-02: solo personal de soporte y administradores.
    [Authorize(Roles = Roles.Gestores)]
    public class SoporteController : Controller
    {
        public IActionResult Detalle()
        {
            return View();
        }

        public IActionResult Historial()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Actualizar(int id)
        {
            return RedirectToAction("Detalle", new { id });
        }

        [HttpPost]
        public IActionResult CambiarEstado(int id)
        {
            return RedirectToAction("Detalle", new { id });
        }

        [HttpPost]
        public IActionResult RegistrarSolucion(int id)
        {
            return RedirectToAction("Detalle", new { id });
        }

        [HttpPost]
        public IActionResult Comentar(int id)
        {
            return RedirectToAction("Detalle", new { id });
        }
    }
}
