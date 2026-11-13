using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Models.ViewModels;
using Sistema_Gestión_Citas.Services;

namespace Sistema_Gestión_Citas.Controllers
{
    /// <summary>
    /// Portal del solicitante: HU-02 (crear), HU-03 (evidencias), HU-04 (consultar)
    /// y HU-05 (comentar y cerrar).
    /// </summary>
    [Authorize(Roles = Roles.Solicitante)]
    public class TicketsController : Controller
    {
        /// <summary>Hasta 5 archivos de 10 MB más el resto del formulario.</summary>
        private const long LimiteSolicitud = 55 * 1024 * 1024;

        private readonly ITiqueteService _tiquetes;
        private readonly IEvidenciaService _evidencias;

        public TicketsController(ITiqueteService tiquetes, IEvidenciaService evidencias)
        {
            _tiquetes = tiquetes;
            _evidencias = evidencias;
        }

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
        [RequestSizeLimit(LimiteSolicitud)]
        [RequestFormLimits(MultipartBodyLengthLimit = LimiteSolicitud)]
        public async Task<IActionResult> Crear(CrearTiqueteViewModel model)
        {
            // RNF-06: si algún archivo no es válido no se crea el tiquete, para no perder la evidencia.
            foreach (var error in _evidencias.Validar(model.Evidencias))
                ModelState.AddModelError(nameof(model.Evidencias), error);

            if (ModelState.IsValid)
            {
                var usuarioId = User.ObtenerUsuarioId();
                var resultado = await _tiquetes.CrearAsync(
                    usuarioId, model.Asunto, model.CategoriaId!.Value, model.Descripcion);

                if (resultado.Exito)
                {
                    var mensaje = $"Tiquete #{resultado.Valor} creado correctamente.";
                    if (model.Evidencias.Count > 0)
                    {
                        var adjunto = await _evidencias.AdjuntarAsync(resultado.Valor, usuarioId, model.Evidencias);
                        mensaje += adjunto.Exito
                            ? $" Se adjuntaron {model.Evidencias.Count} evidencia(s)."
                            : " No se pudieron guardar las evidencias: " + adjunto.Error;
                    }
                    TempData["Ok"] = mensaje;
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

        /// <summary>Comentario adicional mientras el tiquete no esté cerrado (HU-05, RF-06).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comentar(int id, string? comentario)
        {
            var usuarioId = User.ObtenerUsuarioId();
            var resultado = await _tiquetes.ComentarAsync(id, usuarioId, comentario ?? string.Empty, soloDelSolicitanteId: usuarioId);
            return VolverAlDetalle(id, resultado, "Comentario agregado.");
        }

        /// <summary>Cierra el tiquete cuando está «Resuelto» (HU-05, RF-07).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id)
        {
            var resultado = await _tiquetes.CerrarAsync(id, User.ObtenerUsuarioId());
            return VolverAlDetalle(id, resultado, "Tiquete cerrado. ¡Gracias por confirmar!");
        }

        /// <summary>Adjunta evidencias a un tiquete ya creado (HU-03, RF-04).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(LimiteSolicitud)]
        [RequestFormLimits(MultipartBodyLengthLimit = LimiteSolicitud)]
        public async Task<IActionResult> Adjuntar(int id, List<IFormFile> evidencias)
        {
            var usuarioId = User.ObtenerUsuarioId();
            if (await _tiquetes.ObtenerDetalleAsync(id, usuarioId) is null)
                return NotFound();

            var resultado = await _evidencias.AdjuntarAsync(id, usuarioId, evidencias);
            return VolverAlDetalle(id, resultado, "Evidencia adjuntada correctamente.");
        }

        private IActionResult VolverAlDetalle(int id, Resultado resultado, string mensajeOk)
        {
            if (resultado.Exito)
                TempData["Ok"] = mensajeOk;
            else
                TempData["Error"] = resultado.Error;
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}
