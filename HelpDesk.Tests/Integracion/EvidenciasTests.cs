using System.Net;
using System.Text.RegularExpressions;
using HelpDesk.Tests.Infraestructura;

namespace HelpDesk.Tests.Integracion
{
    /// <summary>HU-03 / RF-04 / RNF-06: adjuntar y descargar evidencias.</summary>
    [Trait("HU", "HU-03")]
    public class EvidenciasTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public EvidenciasTests(AppFactory app) => _app = app;

        private Dictionary<string, string> NuevoTiquete(string asunto) => new()
        {
            ["Asunto"] = asunto,
            ["CategoriaId"] = _app.IdCategoria(Datos.CategoriaAcceso).ToString(),
            ["Descripcion"] = "Con evidencia"
        };

        private int CantidadTiquetes() => _app.ConDb(db => db.Tiquetes.Count());

        [Fact]
        public async Task Crear_con_evidencias_las_guarda_y_permite_descargarlas()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);

            var respuesta = await marta.PostArchivosAsync("/Tickets/Crear", "/Tickets/Crear", NuevoTiquete("Con capturas"),
                ("captura.png", Archivos.Png), ("informe.pdf", Archivos.Pdf));

            var id = ClienteWeb.IdDeRedireccion(respuesta);
            var html = await marta.GetHtmlAsync($"/Tickets/Detalle/{id}");
            Assert.Contains("Se adjuntaron 2 evidencia(s).", html);
            Assert.Contains("captura.png", html);
            Assert.Contains("informe.pdf", html);
            Assert.Contains("Evidencia adjuntada: captura.png.", html);

            var evidencia = Regex.Match(html, @"/Evidencias/Descargar/(\d+)").Groups[1].Value;
            var descarga = await marta.GetAsync($"/Evidencias/Descargar/{evidencia}");
            Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
            Assert.Equal("image/png", descarga.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Archivos.Png, await descarga.Content.ReadAsByteArrayAsync());
        }

        [Theory]
        [Trait("RNF", "RNF-06")]
        [InlineData("virus.exe", "solo se permiten archivos PDF, JPG o PNG")]
        [InlineData("notas.txt", "solo se permiten archivos PDF, JPG o PNG")]
        public async Task Archivo_no_permitido_impide_crear_el_tiquete(string nombre, string mensaje)
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var antes = CantidadTiquetes();

            var respuesta = await marta.PostArchivosAsync("/Tickets/Crear", "/Tickets/Crear", NuevoTiquete("Rechazado"),
                (nombre, Archivos.Png));

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Contains(mensaje, await ClienteWeb.HtmlAsync(respuesta));
            Assert.Equal(antes, CantidadTiquetes());
        }

        [Fact]
        [Trait("RNF", "RNF-06")]
        public async Task Archivo_de_mas_de_10_MB_se_rechaza()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);

            var respuesta = await marta.PostArchivosAsync("/Tickets/Crear", "/Tickets/Crear", NuevoTiquete("Grande"),
                ("grande.png", Archivos.PngDeTamano(10 * 1024 * 1024 + 1)));

            Assert.Contains("supera el máximo de 10 MB", await ClienteWeb.HtmlAsync(respuesta));
        }

        [Fact]
        public async Task Adjuntar_a_un_tiquete_existente()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var id = await marta.CrearTiqueteAsync("Adjuntar después", _app.IdCategoria(Datos.CategoriaAcceso));

            await marta.PostArchivosAsync($"/Tickets/Adjuntar/{id}", $"/Tickets/Detalle/{id}", new(), ("foto.jpg", Archivos.Jpg));

            var html = await marta.GetHtmlAsync($"/Tickets/Detalle/{id}");
            Assert.Contains("Evidencia adjuntada correctamente.", html);
            Assert.Contains("foto.jpg", html);
        }

        [Fact]
        [Trait("RNF", "RNF-03")]
        public async Task Soporte_descarga_pero_otro_solicitante_no()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var respuesta = await marta.PostArchivosAsync("/Tickets/Crear", "/Tickets/Crear", NuevoTiquete("Permisos"),
                ("privado.pdf", Archivos.Pdf));
            var id = ClienteWeb.IdDeRedireccion(respuesta);
            var evidencia = _app.ConDb(db => db.Evidencias.Single(e => e.TiqueteId == id).Id);

            var juan = await _app.IniciarSesionAsync(Datos.SoporteJuan);
            Assert.Equal(HttpStatusCode.OK, (await juan.GetAsync($"/Evidencias/Descargar/{evidencia}")).StatusCode);
            Assert.Contains("privado.pdf", await juan.GetHtmlAsync($"/Soporte/Detalle/{id}"));

            var admin = await _app.IniciarSesionAsync(Datos.Admin);
            await admin.PostAsync("/Admin/GuardarUsuario", "/Admin/UsuarioForm", new()
            {
                ["Nombre"] = "Otro",
                ["Correo"] = "otro.evidencias@empresa.com",
                ["Rol"] = "Solicitante",
                ["Password"] = "Clave1234"
            });
            var otro = await _app.IniciarSesionAsync("otro.evidencias@empresa.com", "Clave1234");
            Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/Evidencias/Descargar/{evidencia}")).StatusCode);

            // Sin sesión: se pide iniciar sesión.
            var anonimo = await _app.NuevoCliente().GetAsync($"/Evidencias/Descargar/{evidencia}");
            Assert.Equal(HttpStatusCode.Redirect, anonimo.StatusCode);
            Assert.Contains("/Login/", anonimo.Headers.Location!.ToString());
        }
    }
}
