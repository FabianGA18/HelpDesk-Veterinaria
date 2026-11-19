using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sistema_Gestión_Citas.Controllers;
using Sistema_Gestión_Citas.Data;

namespace HelpDesk.Tests.Infraestructura
{
    /// <summary>
    /// Levanta la aplicación completa en memoria (sin abrir puertos) con una base SQLite propia
    /// y una carpeta temporal para las evidencias. Cada clase de pruebas recibe su propia instancia.
    /// </summary>
    public class AppFactory : WebApplicationFactory<LoginController>
    {
        private readonly SqliteConnection _conexion = new("DataSource=:memory:");

        public string CarpetaEvidencias { get; } =
            Path.Combine(Path.GetTempPath(), "helpdesk-pruebas", Guid.NewGuid().ToString("N"));

        public AppFactory()
        {
            _conexion.Open();
            BaseDatosPrueba.PrepararEsquema(_conexion);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Evidencias:Ruta", CarpetaEvidencias);
            builder.ConfigureServices(servicios =>
            {
                // Reemplaza SQL Server por la base SQLite en memoria.
                servicios.RemoveAll<DbContextOptions<AppDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                servicios.AddDbContext<AppDbContext>(o => BaseDatosPrueba.Configurar(o, _conexion));
            });
        }

        /// <summary>Cliente sin sesión que no sigue redirecciones (para poder revisarlas).</summary>
        public ClienteWeb NuevoCliente() =>
            new(CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            }));

        /// <summary>Cliente con la sesión iniciada; falla la prueba si el login no funciona.</summary>
        public async Task<ClienteWeb> IniciarSesionAsync(string correo, string password = Datos.Password)
        {
            var cliente = NuevoCliente();
            var respuesta = await cliente.IniciarSesionAsync(correo, password);
            Assert.True(respuesta.StatusCode == System.Net.HttpStatusCode.Redirect,
                $"No se pudo iniciar sesión con {correo} (HTTP {(int)respuesta.StatusCode}).");
            return cliente;
        }

        /// <summary>Ejecuta una consulta directa sobre la base de datos de la app.</summary>
        public T ConDb<T>(Func<AppDbContext, T> consulta)
        {
            using var scope = Services.CreateScope();
            return consulta(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }

        public int IdCategoria(string nombre) => ConDb(db => db.Categorias.Single(c => c.Nombre == nombre).Id);
        public int IdUsuario(string correo) => ConDb(db => db.Usuarios.Single(u => u.Correo == correo).Id);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _conexion.Dispose();
                if (Directory.Exists(CarpetaEvidencias))
                    Directory.Delete(CarpetaEvidencias, recursive: true);
            }
        }
    }
}
