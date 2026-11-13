using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Services;

namespace Sistema_Gestión_Citas.Controllers
{
    /// <summary>Descarga de evidencias con control de acceso (RNF-02, RNF-03).</summary>
    [Authorize]
    public class EvidenciasController : Controller
    {
        private readonly IEvidenciaService _evidencias;

        public EvidenciasController(IEvidenciaService evidencias) => _evidencias = evidencias;

        [HttpGet]
        public async Task<IActionResult> Descargar(int id)
        {
            var evidencia = await _evidencias.ObtenerAsync(id);
            if (evidencia is null)
                return NotFound();

            // Soporte y administradores ven todo; el solicitante solo las de sus tiquetes.
            var esGestor = User.IsInRole(Roles.Soporte) || User.IsInRole(Roles.Administrador);
            if (!esGestor && evidencia.Tiquete.SolicitanteId != User.ObtenerUsuarioId())
                return NotFound();

            var archivo = _evidencias.AbrirArchivo(evidencia);
            if (archivo is null)
                return NotFound();

            return File(archivo, evidencia.TipoContenido, evidencia.NombreOriginal);
        }
    }
}
