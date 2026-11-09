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
    }
}
