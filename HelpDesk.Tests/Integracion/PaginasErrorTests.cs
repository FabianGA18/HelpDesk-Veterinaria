using System.Net;
using HelpDesk.Tests.Infraestructura;

namespace HelpDesk.Tests.Integracion
{
    /// <summary>Las respuestas de error muestran una página del sistema en lugar de una página en blanco.</summary>
    public class PaginasErrorTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public PaginasErrorTests(AppFactory app) => _app = app;

        [Theory]
        [InlineData(Datos.Solicitante, "/Tickets/Detalle/999999")]
        [InlineData(Datos.SoporteJuan, "/Soporte/Detalle/999999")]
        [InlineData(Datos.SoporteJuan, "/Evidencias/Descargar/999999")]
        [InlineData(Datos.Admin, "/Admin/CategoriaForm/999999")]
        public async Task Registro_inexistente_muestra_pagina_no_encontrada(string correo, string url)
        {
            var cliente = await _app.IniciarSesionAsync(correo);

            var respuesta = await cliente.GetAsync(url);

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            var html = await ClienteWeb.HtmlAsync(respuesta);
            Assert.Contains("Página no encontrada", html);
            Assert.Contains("Volver al inicio", html);
        }

        [Fact]
        public async Task Envio_sin_token_muestra_solicitud_no_valida()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var id = await marta.CrearTiqueteAsync("Sin token", _app.IdCategoria(Datos.CategoriaAcceso));

            var respuesta = await marta.Http.PostAsync($"/Tickets/Comentar/{id}",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["Comentario"] = "sin token" }));

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
            Assert.Contains("Solicitud no válida", await ClienteWeb.HtmlAsync(respuesta));
            Assert.DoesNotContain("sin token", await marta.GetHtmlAsync($"/Tickets/Detalle/{id}"));
        }

        [Fact]
        public async Task La_pagina_de_error_funciona_sin_sesion()
        {
            var respuesta = await _app.NuevoCliente().GetAsync("/pagina-que-no-existe");

            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
            Assert.Contains("Página no encontrada", await ClienteWeb.HtmlAsync(respuesta));
        }
    }
}
