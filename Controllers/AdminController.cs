using Microsoft.AspNetCore.Mvc;

namespace Sistema_Gestión_Citas.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult CategoriaForm()
        {
            return View();
        }

        public IActionResult Categorias()
        {
            return View();
        }

        public IActionResult Prioridad()
        {
            return View();
        }

        public IActionResult UsuarioForm()
        {
            return View();
        }

        public IActionResult Usuarios()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GuardarCategoria()
        {
            return RedirectToAction("Categorias");
        }

        [HttpPost]
        public IActionResult GuardarUsuario()
        {
            return RedirectToAction("Usuarios");
        }

    }
}
