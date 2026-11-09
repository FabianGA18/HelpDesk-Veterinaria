using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Data
{
    /// <summary>
    /// Aplica las migraciones pendientes y carga los datos iniciales
    /// (roles, categorías y usuarios de prueba) si la base está vacía.
    /// </summary>
    public static class SeedData
    {
        /// <summary>Contraseña de los usuarios de prueba. Cambiarla fuera del entorno de desarrollo.</summary>
        public const string PasswordPrueba = "Vet12345!";

        public static async Task InicializarAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

            await db.Database.MigrateAsync();

            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Rol { Nombre = Roles.Solicitante, Descripcion = "Usuario que reporta problemas o solicitudes" },
                    new Rol { Nombre = Roles.Soporte, Descripcion = "Personal encargado de atender los tiquetes" },
                    new Rol { Nombre = Roles.Administrador, Descripcion = "Responsable de la administración del sistema" });
                await db.SaveChangesAsync();
            }

            if (!await db.Categorias.AnyAsync())
            {
                db.Categorias.AddRange(
                    new Categoria { Nombre = "Acceso", Descripcion = "Inicio de sesión, contraseñas y permisos" },
                    new Categoria { Nombre = "Datos de mascotas", Descripcion = "Expedientes, vacunas e historial clínico" },
                    new Categoria { Nombre = "Agenda de citas", Descripcion = "Reservación y programación de citas" },
                    new Categoria { Nombre = "Equipos médicos", Descripcion = "Fallas en equipos de la clínica" },
                    new Categoria { Nombre = "Inventario", Descripcion = "Medicamentos e insumos" },
                    new Categoria { Nombre = "Laboratorio", Descripcion = "Resultados y exámenes de laboratorio" });
                await db.SaveChangesAsync();
            }

            if (!await db.Usuarios.AnyAsync())
            {
                var roles = await db.Roles.ToDictionaryAsync(r => r.Nombre, r => r.Id);

                Usuario Nuevo(string nombre, string correo, string rol)
                {
                    var u = new Usuario { Nombre = nombre, Correo = correo, RolId = roles[rol] };
                    u.PasswordHash = hasher.HashPassword(u, PasswordPrueba);
                    return u;
                }

                db.Usuarios.AddRange(
                    Nuevo("Ana Mora", "amora@empresa.com", Roles.Administrador),
                    Nuevo("Juan Guillermo", "jguillermo@empresa.com", Roles.Soporte),
                    Nuevo("Laura Vega", "lvega@empresa.com", Roles.Soporte),
                    Nuevo("Marta Solís", "msolis@empresa.com", Roles.Solicitante));
                await db.SaveChangesAsync();
            }
        }
    }
}
