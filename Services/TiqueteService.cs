using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Data;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public class TiqueteService : ITiqueteService
    {
        private readonly AppDbContext _db;

        public TiqueteService(AppDbContext db) => _db = db;

        public Task<List<Categoria>> ObtenerCategoriasActivasAsync() =>
            _db.Categorias.AsNoTracking()
                .Where(c => c.Activa)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

        public async Task<Resultado<int>> CrearAsync(int solicitanteId, string asunto, int categoriaId, string descripcion)
        {
            asunto = asunto.Trim();
            descripcion = descripcion.Trim();

            if (string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(descripcion))
                return Resultado<int>.Falla("Completa asunto, categoría y descripción.");

            var categoriaValida = await _db.Categorias.AnyAsync(c => c.Id == categoriaId && c.Activa);
            if (!categoriaValida)
                return Resultado<int>.Falla("La categoría seleccionada no existe o está desactivada.");

            var ahora = DateTime.Now;
            var tiquete = new Tiquete
            {
                Asunto = asunto,
                Descripcion = descripcion,
                CategoriaId = categoriaId,
                SolicitanteId = solicitanteId,
                Estado = EstadoTiquete.Nuevo,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };
            tiquete.Historial.Add(new HistorialTiquete
            {
                UsuarioId = solicitanteId,
                Tipo = TipoEvento.Creacion,
                Descripcion = "Tiquete creado con estado «Nuevo».",
                Fecha = ahora
            });

            _db.Tiquetes.Add(tiquete);
            await _db.SaveChangesAsync();
            return Resultado<int>.Ok(tiquete.Id);
        }

        public Task<List<Tiquete>> ListarDelSolicitanteAsync(int solicitanteId) =>
            _db.Tiquetes.AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Responsable)
                .Where(t => t.SolicitanteId == solicitanteId)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

        public Task<Tiquete?> ObtenerDetalleAsync(int id, int? soloDelSolicitanteId = null)
        {
            var consulta = _db.Tiquetes.AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Solicitante)
                .Include(t => t.Responsable)
                .Include(t => t.Historial).ThenInclude(h => h.Usuario)
                .AsSplitQuery()
                .Where(t => t.Id == id);

            if (soloDelSolicitanteId is int sid)
                consulta = consulta.Where(t => t.SolicitanteId == sid);

            return consulta.FirstOrDefaultAsync();
        }
    }
}
