using System.Net;
using HelpDesk.Tests.Infraestructura;

namespace HelpDesk.Tests.Integracion
{
    /// <summary>HU-01 / RF-01: inicio de sesión. RNF-02: restricción de funciones por rol.</summary>
    [Trait("HU", "HU-01")]
    public class AutenticacionTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public AutenticacionTests(AppFactory app) => _app = app;

        [Fact]
        public async Task Anonimo_es_redirigido_al_login()
        {
            var respuesta = await _app.NuevoCliente().GetAsync("/Tickets/Mis");

            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Contains("/Login/", respuesta.Headers.Location!.ToString());
        }

        [Fact]
        public async Task Contrasena_incorrecta_muestra_error_generico()
        {
            var respuesta = await _app.NuevoCliente().IniciarSesionAsync(Datos.Solicitante, "incorrecta");

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Contains("Usuario o contraseña incorrectos.", await ClienteWeb.HtmlAsync(respuesta));
        }

        [Fact]
        public async Task Correo_inexistente_muestra_el_mismo_error()
        {
            var respuesta = await _app.NuevoCliente().IniciarSesionAsync("nadie@empresa.com", Datos.Password);

            Assert.Contains("Usuario o contraseña incorrectos.", await ClienteWeb.HtmlAsync(respuesta));
        }

        [Theory]
        [InlineData(Datos.Solicitante, "/Tickets/Mis")]
        [InlineData(Datos.SoporteJuan, "/Soporte")]
        [InlineData(Datos.Admin, "/Soporte")]
        public async Task Login_redirige_a_la_pantalla_del_rol(string correo, string destino)
        {
            var respuesta = await _app.NuevoCliente().IniciarSesionAsync(correo, Datos.Password);

            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.StartsWith(destino, respuesta.Headers.Location!.ToString());
        }

        [Fact]
        public async Task El_correo_no_distingue_mayusculas()
        {
            var respuesta = await _app.NuevoCliente().IniciarSesionAsync("MSOLIS@Empresa.com", Datos.Password);

            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        }

        [Fact]
        public async Task Cerrar_sesion_vuelve_a_pedir_login()
        {
            var cliente = await _app.IniciarSesionAsync(Datos.Solicitante);

            await cliente.PostAsync(ClienteWeb.UrlLogout, "/Tickets/Mis");
            var respuesta = await cliente.GetAsync("/Tickets/Mis");

            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Contains("/Login/", respuesta.Headers.Location!.ToString());
        }

        /// <summary>RNF-02: matriz de acceso por rol. true = puede entrar; false = acceso denegado.</summary>
        [Theory]
        [Trait("RNF", "RNF-02")]
        [InlineData(Datos.Solicitante, "/Tickets/Mis", true)]
        [InlineData(Datos.Solicitante, "/Soporte/Index", false)]
        [InlineData(Datos.Solicitante, "/Admin/Categorias", false)]
        [InlineData(Datos.SoporteJuan, "/Soporte/Index", true)]
        [InlineData(Datos.SoporteJuan, "/Tickets/Mis", false)]
        [InlineData(Datos.SoporteJuan, "/Admin/Usuarios", false)]
        [InlineData(Datos.Admin, "/Soporte/Index", true)]
        [InlineData(Datos.Admin, "/Admin/Categorias", true)]
        [InlineData(Datos.Admin, "/Admin/Usuarios", true)]
        [InlineData(Datos.Admin, "/Admin/Prioridad", true)]
        public async Task Cada_rol_solo_entra_a_sus_funciones(string correo, string url, bool permitido)
        {
            var cliente = await _app.IniciarSesionAsync(correo);

            var respuesta = await cliente.GetAsync(url);

            if (permitido)
            {
                Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
                Assert.Contains("AccesoDenegado", respuesta.Headers.Location!.ToString());
            }
        }
    }
}
