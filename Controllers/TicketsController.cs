using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Models.ViewModels;
using Sistema_Gestión_Citas.Services;

namespace Sistema_Gestión_Citas.Controllers
{
    /// <summary>Portal del solicitante: HU-02 (crear), HU-04 (consultar), HU-05 (comentar/cerrar).</summary>
    [Authorize(Roles = Roles.Solicitante)]
    public class TicketsController : Controller
    {
        private readonly ITiqueteService _tiquetes;

        public TicketsController(ITiqueteService tiquetes) => _tiquetes = tiquetes;

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var model = new CrearTiqueteViewModel
            {
                Categorias = await _tiquetes.ObtenerCategoriasActivasAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearTiqueteViewModel model)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _tiquetes.CrearAsync(
                    User.ObtenerUsuarioId(), model.Asunto, model.CategoriaId!.Value, model.Descripcion);

                if (resultado.Exito)
                {
                    TempData["Ok"] = $"Tiquete #{resultado.Valor} creado correctamente.";
                    return RedirectToAction(nameof(Detalle), new { id = resultado.Valor });
                }
                ModelState.AddModelError(string.Empty, resultado.Error!);
            }

            model.Categorias = await _tiquetes.ObtenerCategoriasActivasAsync();
            return View(model);
        }

        /// <summary>Tiquetes propios del usuario autenticado (RF-05, RNF-03).</summary>
        [HttpGet]
        public async Task<IActionResult> Mis()
        {
            var tiquetes = await _tiquetes.ListarDelSolicitanteAsync(User.ObtenerUsuarioId());
            return View(tiquetes.Select(TiqueteFilaViewModel.Desde).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            // Solo se encuentra si el tiquete pertenece al solicitante (RNF-03).
            var tiquete = await _tiquetes.ObtenerDetalleAsync(id, User.ObtenerUsuarioId());
            if (tiquete is null)
                return NotFound();

            return View(TiqueteDetalleViewModel.Desde(tiquete));
        }

        // TODO Iteración 3 (HU-05, RF-06): agregar comentario si el tiquete no está cerrado.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Comentar(int id)
        {
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // TODO Iteración 3 (HU-05, RF-07): cerrar si el tiquete está «Resuelto».
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cerrar(int id)
        {
            return RedirectToAction(nameof(Mis));
        }
    }
}
