using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Data;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Usuario> _hasher;

        public UsuarioService(AppDbContext db, IPasswordHasher<Usuario> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<Usuario?> ValidarCredencialesAsync(string correo, string password)
        {
            var normalizado = correo.Trim().ToLower();
            var usuario = await _db.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo.ToLower() == normalizado);

            if (usuario is null || !usuario.Activo)
                return null;

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
            if (resultado == PasswordVerificationResult.Failed)
                return null;

            // Si el algoritmo de hash se actualizó, se vuelve a guardar con el nuevo formato.
            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.PasswordHash = _hasher.HashPassword(usuario, password);
                await _db.SaveChangesAsync();
            }

            return usuario;
        }

        // ----------------------------------------------------------------------
        // Administración de usuarios y roles (HU-11, RF-15)
        // ----------------------------------------------------------------------

        public const int LongitudMinimaPassword = 8;

        public Task<Usuario?> ObtenerActivoAsync(int id) =>
            _db.Usuarios.AsNoTracking().Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id && u.Activo);

        public Task<List<Usuario>> ListarAsync() =>
            _db.Usuarios.AsNoTracking()
                .Include(u => u.Rol)
                .OrderByDescending(u => u.Activo).ThenBy(u => u.Nombre)
                .ToListAsync();

        public Task<Usuario?> ObtenerAsync(int id) =>
            _db.Usuarios.AsNoTracking().Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);

        public Task<List<Rol>> ListarRolesAsync() =>
            _db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();

        public async Task<Resultado<int>> CrearAsync(string nombre, string correo, string rol, string password)
        {
            var (error, rolId) = await ValidarAsync(null, nombre, correo, rol);
            if (error is not null)
                return Resultado<int>.Falla(error);
            if (string.IsNullOrEmpty(password) || password.Length < LongitudMinimaPassword)
                return Resultado<int>.Falla($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");

            var usuario = new Usuario
            {
                Nombre = nombre.Trim(),
                Correo = correo.Trim().ToLower(),
                RolId = rolId,
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            usuario.PasswordHash = _hasher.HashPassword(usuario, password);
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
            return Resultado<int>.Ok(usuario.Id);
        }

        public async Task<Resultado> ActualizarAsync(int id, int actorId, string nombre, string correo, string rol, bool activo, string? nuevaPassword)
        {
            var usuario = await _db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario is null)
                return Resultado.Falla("El usuario no existe.");

            var (error, rolId) = await ValidarAsync(id, nombre, correo, rol);
            if (error is not null)
                return Resultado.Falla(error);

            var dejaDeSerAdminActivo = usuario.Rol.Nombre == Roles.Administrador && usuario.Activo
                                       && (!activo || rol != Roles.Administrador);
            if (dejaDeSerAdminActivo)
            {
                // Evita que el sistema quede sin administradores o que el admin se bloquee a sí mismo.
                if (id == actorId)
                    return Resultado.Falla("No puedes desactivarte ni quitarte el rol de administrador a ti mismo.");
                var otrosAdmins = await _db.Usuarios.CountAsync(u =>
                    u.Id != id && u.Activo && u.Rol.Nombre == Roles.Administrador);
                if (otrosAdmins == 0)
                    return Resultado.Falla("Debe quedar al menos un administrador activo.");
            }

            if (!string.IsNullOrEmpty(nuevaPassword))
            {
                if (nuevaPassword.Length < LongitudMinimaPassword)
                    return Resultado.Falla($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");
                usuario.PasswordHash = _hasher.HashPassword(usuario, nuevaPassword);
            }

            usuario.Nombre = nombre.Trim();
            usuario.Correo = correo.Trim().ToLower();
            usuario.RolId = rolId;
            usuario.Activo = activo;
            await _db.SaveChangesAsync();
            return Resultado.Ok();
        }

        private async Task<(string? Error, int RolId)> ValidarAsync(int? id, string nombre, string correo, string rol)
        {
            nombre = nombre?.Trim() ?? string.Empty;
            correo = correo?.Trim().ToLower() ?? string.Empty;

            if (nombre.Length == 0)
                return ("El nombre es obligatorio.", 0);
            if (nombre.Length > 100)
                return ("El nombre admite máximo 100 caracteres.", 0);
            if (correo.Length == 0 || correo.Length > 150 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(correo))
                return ("Ingresa un correo válido.", 0);

            var rolEntidad = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == rol);
            if (rolEntidad is null)
                return ("Selecciona un rol válido.", 0);

            if (await _db.Usuarios.AnyAsync(u => u.Id != id && u.Correo.ToLower() == correo))
                return ($"Ya existe un usuario con el correo {correo}.", 0);

            return (null, rolEntidad.Id);
        }
    }
}
