using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sistema_Gestión_Citas.Data;
using Sistema_Gestión_Citas.Models.Entidades;

namespace HelpDesk.Tests.Infraestructura
{
    /// <summary>
    /// Base de datos SQLite en memoria para las pruebas. Vive mientras la conexión esté abierta.
    /// Las migraciones del proyecto son de SQL Server, así que el esquema se crea a partir del modelo
    /// y las migraciones se marcan como aplicadas para que Program/SeedData no intenten ejecutarlas.
    /// </summary>
    public static class BaseDatosPrueba
    {
        public static void Configurar(DbContextOptionsBuilder opciones, SqliteConnection conexion) =>
            opciones.UseSqlite(conexion)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        public static AppDbContext NuevoContexto(SqliteConnection conexion)
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>();
            Configurar(opciones, conexion);
            return new AppDbContext(opciones.Options);
        }

        public static void PrepararEsquema(SqliteConnection conexion)
        {
            using var db = NuevoContexto(conexion);
            db.Database.EnsureCreated();
            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT NOT NULL)");
            foreach (var migracion in db.Database.GetMigrations())
                db.Database.ExecuteSqlRaw(
                    "INSERT OR IGNORE INTO __EFMigrationsHistory VALUES ({0}, '10.0')", migracion);
        }
    }

    /// <summary>
    /// Base de datos con los datos semilla del sistema (roles, categorías y usuarios de prueba),
    /// para probar los servicios directamente sin levantar la aplicación web.
    /// </summary>
    public sealed class BaseConDatos : IDisposable
    {
        private readonly SqliteConnection _conexion = new("DataSource=:memory:");
        private readonly ServiceProvider _servicios;

        public IPasswordHasher<Usuario> Hasher { get; } = new PasswordHasher<Usuario>();

        public BaseConDatos()
        {
            _conexion.Open();
            BaseDatosPrueba.PrepararEsquema(_conexion);

            var servicios = new ServiceCollection();
            servicios.AddDbContext<AppDbContext>(o => BaseDatosPrueba.Configurar(o, _conexion));
            servicios.AddSingleton(Hasher);
            _servicios = servicios.BuildServiceProvider();
            SeedData.InicializarAsync(_servicios).GetAwaiter().GetResult();
        }

        /// <summary>Contexto nuevo (sin entidades en caché), como en cada solicitud web.</summary>
        public AppDbContext Db() => BaseDatosPrueba.NuevoContexto(_conexion);

        public int IdUsuario(string correo)
        {
            using var db = Db();
            return db.Usuarios.Single(u => u.Correo == correo).Id;
        }

        public int IdCategoria(string nombre)
        {
            using var db = Db();
            return db.Categorias.Single(c => c.Nombre == nombre).Id;
        }

        public void Dispose()
        {
            _servicios.Dispose();
            _conexion.Dispose();
        }
    }
}
