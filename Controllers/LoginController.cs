using Microsoft.AspNetCore.Mvc;

namespace Sistema_Gestión_Citas.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult InicioSesión()
        {
            return View();
        }

        [HttpPost]
        public IActionResult InicioSesión(string? usuario, string? password)
        {
            return View();
        }

        [HttpPost]
        public IActionResult CerrarSesión()
        {
            return RedirectToAction("InicioSesión");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}