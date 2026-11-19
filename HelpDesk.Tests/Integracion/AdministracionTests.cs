using System.Net;
using System.Text.RegularExpressions;
using HelpDesk.Tests.Infraestructura;

namespace HelpDesk.Tests.Integracion
{
    /// <summary>Administración: HU-10, HU-11 y HU-12 a través de la aplicación web.</summary>
    public class AdministracionTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public AdministracionTests(AppFactory app) => _app = app;

        [Fact]
        [Trait("HU", "HU-10")]
        public async Task Categoria_desactivada_deja_de_ofrecerse_al_crear()
        {
            var admin = await _app.IniciarSesionAsync(Datos.Admin);
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);

            await admin.PostAsync("/Admin/GuardarCategoria", "/Admin/CategoriaForm", new() { ["Nombre"] = "Facturación" });
            Assert.Contains("Facturación", await admin.GetHtmlAsync("/Admin/Categorias"));
            Assert.Contains("Facturación", await marta.GetHtmlAsync("/Tickets/Crear"));

            var id = _app.IdCategoria("Facturación");
            await admin.PostAsync($"/Admin/GuardarCategoria/{id}", $"/Admin/CategoriaForm/{id}", new()
            {
                ["Nombre"] = "Facturación",
                ["Activa"] = "false"
            });

            Assert.Contains("Inactiva", await admin.GetHtmlAsync("/Admin/Categorias"));
            Assert.DoesNotContain("Facturación", await marta.GetHtmlAsync("/Tickets/Crear"));
        }

        [Fact]
        [Trait("HU", "HU-10")]
        public async Task Categoria_duplicada_muestra_error()
        {
            var admin = await _app.IniciarSesionAsync(Datos.Admin);

            var respuesta = await admin.PostAsync("/Admin/GuardarCategoria", "/Admin/CategoriaForm", new() { ["Nombre"] = "acceso" });

            Assert.Contains("Ya existe una categoría", await ClienteWeb.HtmlAsync(respuesta));
        }

        [Fact]
        [Trait("HU", "HU-11")]
        public async Task Usuario_creado_inicia_sesion_con_su_rol()
        {
            var admin = await _app.IniciarSesionAsync(Datos.Admin);

            await admin.PostAsync("/Admin/GuardarUsuario", "/Admin/UsuarioForm", new()
            {
                ["Nombre"] = "Carlos Rojas",
                ["Correo"] = "crojas@empresa.com",
                ["Rol"] = "Soporte",
                ["Password"] = "Clave1234"
            });

            Assert.Contains("crojas@empresa.com", await admin.GetHtmlAsync("/Admin/Usuarios"));
            var carlos = await _app.IniciarSesionAsync("crojas@empresa.com", "Clave1234");
            Assert.Equal(HttpStatusCode.OK, (await carlos.GetAsync("/Soporte/Index")).StatusCode);
        }

        [Fact]
        [Trait("HU", "HU-11")]
        [Trait("RNF", "RNF-02")]
        public async Task Desactivar_usuario_cierra_su_sesion_abierta()
        {
            var admin = await _app.IniciarSesionAsync(Datos.Admin);
            await admin.PostAsync("/Admin/GuardarUsuario", "/Admin/UsuarioForm", new()
            {
                ["Nombre"] = "Temporal",
                ["Correo"] = "temporal@empresa.com",
                ["Rol"] = "Soporte",
                ["Password"] = "Clave1234"
            });
            var temporal = await _app.IniciarSesionAsync("temporal@empresa.com", "Clave1234");
            var id = _app.IdUsuario("temporal@empresa.com");

            await admin.PostAsync($"/Admin/GuardarUsuario/{id}", $"/Admin/UsuarioForm/{id}", new()
            {
                ["Nombre"] = "Temporal",
                ["Correo"] = "temporal@empresa.com",
                ["Rol"] = "Soporte",
                ["Activo"] = "false"
            });

            var respuesta = await temporal.GetAsync("/Soporte/Index");
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Contains("/Login/", respuesta.Headers.Location!.ToString());
            Assert.Equal(HttpStatusCode.OK, (await _app.NuevoCliente().IniciarSesionAsync("temporal@empresa.com", "Clave1234")).StatusCode);
        }

        [Fact]
        [Trait("HU", "HU-11")]
        public async Task El_admin_no_puede_quitarse_su_rol()
        {
            var admin = await _app.IniciarSesionAsync(Datos.Admin);
            var id = _app.IdUsuario(Datos.Admin);

            var respuesta = await admin.PostAsync($"/Admin/GuardarUsuario/{id}", $"/Admin/UsuarioForm/{id}", new()
            {
                ["Nombre"] = "Ana Mora",
                ["Correo"] = Datos.Admin,
                ["Rol"] = "Soporte",
                ["Activo"] = "true"
            });

            Assert.Contains("No puedes desactivarte ni quitarte el rol", await ClienteWeb.HtmlAsync(respuesta));
        }

        [Fact]
        [Trait("HU", "HU-12")]
        public async Task Tiquetes_por_prioridad_cuenta_y_filtra()
        {
            var admin = await _app.IniciarSesionAsync(Datos.Admin);
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var juan = await _app.IniciarSesionAsync(Datos.SoporteJuan);
            var id = await marta.CrearTiqueteAsync("Urgente de prioridad", _app.IdCategoria(Datos.CategoriaAcceso));
            await juan.PostAsync($"/Soporte/Actualizar/{id}", $"/Soporte/Detalle/{id}", new()
            {
                ["ResponsableId"] = "",
                ["Prioridad"] = "Alta",
                ["CategoriaId"] = _app.IdCategoria(Datos.CategoriaAcceso).ToString()
            });

            var altas = await admin.GetHtmlAsync("/Admin/Prioridad?prioridad=Alta");
            var bajas = await admin.GetHtmlAsync("/Admin/Prioridad?prioridad=Baja");

            Assert.Contains("Urgente de prioridad", altas);
            Assert.DoesNotContain("Urgente de prioridad", bajas);
            var conteoAlta = int.Parse(Regex.Match(altas, @"p-alta[^""]*""[^>]*><span class=""pill p-alta"">Alta</span><div class=""n"">(\d+)").Groups[1].Value);
            Assert.True(conteoAlta >= 1);
        }
    }
}
