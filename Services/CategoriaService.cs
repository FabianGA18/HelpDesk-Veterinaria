using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Data;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly AppDbContext _db;

        public CategoriaService(AppDbContext db) => _db = db;

        public async Task<List<(Categoria Categoria, int TiquetesActivos)>> ListarConConteoAsync()
        {
            var filas = await _db.Categorias.AsNoTracking()
                .OrderByDescending(c => c.Activa).ThenBy(c => c.Nombre)
                .Select(c => new
                {
                    Categoria = c,
                    Activos = c.Tiquetes.Count(t => t.Estado != EstadoTiquete.Cerrado)
                })
                .ToListAsync();
            return filas.Select(f => (f.Categoria, f.Activos)).ToList();
        }

        public Task<Categoria?> ObtenerAsync(int id) =>
            _db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Resultado<int>> CrearAsync(string nombre, string? descripcion)
        {
            var error = await ValidarAsync(null, ref nombre, ref descripcion);
            if (error is not null)
                return Resultado<int>.Falla(error);

            var categoria = new Categoria { Nombre = nombre, Descripcion = descripcion, Activa = true };
            _db.Categorias.Add(categoria);
            await _db.SaveChangesAsync();
            return Resultado<int>.Ok(categoria.Id);
        }

        public async Task<Resultado> ActualizarAsync(int id, string nombre, string? descripcion, bool activa)
        {
            var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id);
            if (categoria is null)
                return Resultado.Falla("La categoría no existe.");

            var error = await ValidarAsync(id, ref nombre, ref descripcion);
            if (error is not null)
                return Resultado.Falla(error);

            categoria.Nombre = nombre;
            categoria.Descripcion = descripcion;
            // Desactivar no afecta los tiquetes existentes; solo deja de ofrecerse al crear o reclasificar.
            categoria.Activa = activa;
            await _db.SaveChangesAsync();
            return Resultado.Ok();
        }

        private Task<string?> ValidarAsync(int? id, ref string nombre, ref string? descripcion)
        {
            nombre = nombre?.Trim() ?? string.Empty;
            descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

            if (nombre.Length == 0)
                return Task.FromResult<string?>("El nombre es obligatorio.");
            if (nombre.Length > 80)
                return Task.FromResult<string?>("El nombre admite máximo 80 caracteres.");
            if (descripcion?.Length > 250)
                return Task.FromResult<string?>("La descripción admite máximo 250 caracteres.");

            return ExisteNombreAsync(id, nombre);
        }

        private async Task<string?> ExisteNombreAsync(int? id, string nombre)
        {
            var normalizado = nombre.ToLower();
            var existe = await _db.Categorias.AnyAsync(c => c.Id != id && c.Nombre.ToLower() == normalizado);
            return existe ? $"Ya existe una categoría llamada «{nombre}»." : null;
        }
    }
}
