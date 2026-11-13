using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Models.ViewModels;
using Sistema_Gestión_Citas.Services;

namespace Sistema_Gestión_Citas.Controllers
{
    /// <summary>
    /// Bandeja y gestión de soporte: HU-06 (bandeja), HU-07 (asignar), HU-08 (clasificar)
    /// y HU-09 (registrar solución). RNF-02: solo personal de soporte y administradores.
    /// </summary>
    [Authorize(Roles = Roles.Gestores)]
    public class SoporteController : Controller
    {
        private readonly ITiqueteService _tiquetes;

        public SoporteController(ITiqueteService tiquetes) => _tiquetes = tiquetes;

        /// <summary>Bandeja con filtros por estado, prioridad, categoría y búsqueda (RF-08).</summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? estado, string? prioridad, int? categoria, string? q)
        {
            var filtro = new FiltroBandeja
            {
                Estado = Formato.ParsearEnum<EstadoTiquete>(estado),
                Prioridad = Formato.ParsearEnum<PrioridadTiquete>(prioridad),
                CategoriaId = categoria,
                Busqueda = q
            };
            // Los cerrados se consultan en «Historial».
            if (filtro.Estado == EstadoTiquete.Cerrado)
                filtro.Estado = null;

            return View(await CrearBandejaAsync(filtro));
        }

        /// <summary>Tiquetes en estado «Cerrado».</summary>
        [HttpGet]
        public async Task<IActionResult> Historial(string? q)
        {
            var filtro = new FiltroBandeja { Estado = EstadoTiquete.Cerrado, Busqueda = q };
            return View(await CrearBandejaAsync(filtro));
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var tiquete = await _tiquetes.ObtenerDetalleAsync(id);
            if (tiquete is null)
                return NotFound();

            return View(new GestionTiqueteViewModel
            {
                Tiquete = TiqueteDetalleViewModel.Desde(tiquete),
                Agentes = await _tiquetes.ObtenerAgentesAsync(),
                Categorias = await _tiquetes.ObtenerCategoriasAsync(),
                Transiciones = FlujoEstados.TransicionesManuales(tiquete.Estado)
            });
        }

        /// <summary>Guarda responsable (RF-09), prioridad (RF-10) y categoría (RF-11).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, int? responsableId, string? prioridad, int categoriaId)
        {
            var resultado = await _tiquetes.ActualizarAsync(
                id, User.ObtenerUsuarioId(), responsableId,
                Formato.ParsearEnum<PrioridadTiquete>(prioridad), categoriaId);
            return VolverAlDetalle(id, resultado, "Cambios guardados.");
        }

        /// <summary>Cambia el estado según el flujo definido (RF-12).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, string? estado)
        {
            var nuevo = Formato.ParsearEnum<EstadoTiquete>(estado);
            var resultado = nuevo is null
                ? Resultado.Falla("Estado no válido.")
                : await _tiquetes.CambiarEstadoAsync(id, User.ObtenerUsuarioId(), nuevo.Value);
            return VolverAlDetalle(id, resultado, $"Estado cambiado a «{nuevo?.Texto()}».");
        }

        /// <summary>Registra la solución y marca el tiquete como «Resuelto» (RF-13).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarSolucion(int id, string? solucion)
        {
            var resultado = await _tiquetes.RegistrarSolucionAsync(id, User.ObtenerUsuarioId(), solucion ?? string.Empty);
            return VolverAlDetalle(id, resultado, "Solución registrada. El tiquete quedó «Resuelto».");
        }

        /// <summary>Comentario de seguimiento de soporte (RF-13).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comentar(int id, string? comentario)
        {
            var resultado = await _tiquetes.ComentarAsync(id, User.ObtenerUsuarioId(), comentario ?? string.Empty);
            return VolverAlDetalle(id, resultado, "Comentario agregado.");
        }

        private async Task<BandejaViewModel> CrearBandejaAsync(FiltroBandeja filtro)
        {
            var tiquetes = await _tiquetes.BuscarAsync(filtro);
            return new BandejaViewModel
            {
                Tiquetes = tiquetes.Select(TiqueteFilaViewModel.Desde).ToList(),
                Categorias = await _tiquetes.ObtenerCategoriasAsync(),
                Estado = filtro.Estado,
                Prioridad = filtro.Prioridad,
                CategoriaId = filtro.CategoriaId,
                Busqueda = filtro.Busqueda
            };
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
