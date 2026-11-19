using System.Net;
using HelpDesk.Tests.Infraestructura;

namespace HelpDesk.Tests.Integracion
{
    /// <summary>Portal del solicitante: HU-02, HU-04 y HU-05 a través de la aplicación web.</summary>
    public class SolicitanteTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public SolicitanteTests(AppFactory app) => _app = app;

        [Fact]
        [Trait("HU", "HU-02")]
        public async Task El_formulario_ofrece_las_categorias_activas()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);

            var html = await marta.GetHtmlAsync("/Tickets/Crear");

            Assert.Contains(Datos.CategoriaAcceso, html);
            Assert.Contains(Datos.CategoriaEquipos, html);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        public async Task Crear_tiquete_muestra_numero_y_estado_Nuevo()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);

            var id = await marta.CrearTiqueteAsync("Refrigerador de vacunas", _app.IdCategoria(Datos.CategoriaEquipos));
            var html = await marta.GetHtmlAsync($"/Tickets/Detalle/{id}");

            Assert.Contains($"Tiquete #{id} creado correctamente", html);
            Assert.Contains("Refrigerador de vacunas", html);
            Assert.Contains("class=\"st s-nuevo\"", html);
            Assert.Contains("Tiquete creado con estado «Nuevo».", html);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        public async Task Crear_sin_campos_obligatorios_muestra_errores()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);

            var respuesta = await marta.PostAsync("/Tickets/Crear", "/Tickets/Crear", new()
            {
                ["Asunto"] = "",
                ["CategoriaId"] = "",
                ["Descripcion"] = ""
            });

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var html = await ClienteWeb.HtmlAsync(respuesta);
            Assert.Contains("El asunto es obligatorio.", html);
            Assert.Contains("Selecciona una categoría.", html);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        public async Task Mis_tiquetes_lista_los_tiquetes_propios()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var id = await marta.CrearTiqueteAsync("Agenda duplicada", _app.IdCategoria(Datos.CategoriaAcceso));

            var html = await marta.GetHtmlAsync("/Tickets/Mis");

            Assert.Contains($"#{id}", html);
            Assert.Contains("Agenda duplicada", html);
        }

        [Fact]
        [Trait("RNF", "RNF-03")]
        public async Task Otro_solicitante_no_ve_ni_comenta_tiquetes_ajenos()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var id = await marta.CrearTiqueteAsync("Privado", _app.IdCategoria(Datos.CategoriaAcceso));
            var otro = await CrearOtroSolicitanteAsync();

            Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/Tickets/Detalle/{id}")).StatusCode);
            Assert.DoesNotContain($"#{id}", await otro.GetHtmlAsync("/Tickets/Mis"));

            await otro.PostAsync($"/Tickets/Comentar/{id}", "/Tickets/Crear", new() { ["Comentario"] = "intruso" });
            Assert.DoesNotContain("intruso", await marta.GetHtmlAsync($"/Tickets/Detalle/{id}"));
        }

        [Fact]
        [Trait("HU", "HU-05")]
        public async Task Comentar_y_cerrar_cuando_esta_resuelto()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var juan = await _app.IniciarSesionAsync(Datos.SoporteJuan);
            var id = await marta.CrearTiqueteAsync("Cierre", _app.IdCategoria(Datos.CategoriaAcceso));
            var detalle = $"/Tickets/Detalle/{id}";

            await marta.PostAsync($"/Tickets/Comentar/{id}", detalle, new() { ["Comentario"] = "Pasa todas las mañanas." });
            Assert.Contains("Pasa todas las mañanas.", await marta.GetHtmlAsync(detalle));

            // Aún no está resuelto: no se puede cerrar ni aparece el botón.
            Assert.DoesNotContain("cerrar tiquete", await marta.GetHtmlAsync(detalle));
            await marta.PostAsync($"/Tickets/Cerrar/{id}", detalle);
            Assert.Contains("Solo puedes cerrar un tiquete que esté en estado «Resuelto».", await marta.GetHtmlAsync(detalle));

            await SoporteTests.LlevarAResueltoAsync(_app, juan, id, "Se reemplazó el cable.");
            var resuelto = await marta.GetHtmlAsync(detalle);
            Assert.Contains("Se reemplazó el cable.", resuelto);
            Assert.Contains("cerrar tiquete", resuelto);

            await marta.PostAsync($"/Tickets/Cerrar/{id}", detalle);
            var cerrado = await marta.GetHtmlAsync(detalle);
            Assert.Contains("class=\"st s-cerrado\"", cerrado);
            Assert.DoesNotContain("Agregar comentario", cerrado);
        }

        private async Task<ClienteWeb> CrearOtroSolicitanteAsync()
        {
            var admin = await _app.IniciarSesionAsync(Datos.Admin);
            var correo = $"otro{Guid.NewGuid().ToString("N")[..8]}@empresa.com";
            await admin.PostAsync("/Admin/GuardarUsuario", "/Admin/UsuarioForm", new()
            {
                ["Nombre"] = "Otro Solicitante",
                ["Correo"] = correo,
                ["Rol"] = "Solicitante",
                ["Password"] = "Clave1234"
            });
            return await _app.IniciarSesionAsync(correo, "Clave1234");
        }
    }
}
