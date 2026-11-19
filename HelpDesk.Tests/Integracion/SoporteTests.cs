using System.Net;
using System.Text.RegularExpressions;
using HelpDesk.Tests.Infraestructura;

namespace HelpDesk.Tests.Integracion
{
    /// <summary>Atención de soporte: HU-06, HU-07, HU-08 y HU-09 a través de la aplicación web.</summary>
    public class SoporteTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public SoporteTests(AppFactory app) => _app = app;

        /// <summary>Asigna a Laura, pasa a «En progreso» y registra la solución.</summary>
        public static async Task LlevarAResueltoAsync(AppFactory app, ClienteWeb soporte, int id, string solucion)
        {
            var detalle = $"/Soporte/Detalle/{id}";
            var categoria = app.ConDb(db => db.Tiquetes.Single(t => t.Id == id).CategoriaId);
            await soporte.PostAsync($"/Soporte/Actualizar/{id}", detalle, new()
            {
                ["ResponsableId"] = app.IdUsuario(Datos.SoporteLaura).ToString(),
                ["Prioridad"] = "Media",
                ["CategoriaId"] = categoria.ToString()
            });
            await soporte.PostAsync($"/Soporte/CambiarEstado/{id}", detalle, new() { ["Estado"] = "EnProgreso" });
            await soporte.PostAsync($"/Soporte/RegistrarSolucion/{id}", detalle, new() { ["Solucion"] = solucion });
        }

        private async Task<(ClienteWeb Juan, int Id)> PrepararAsync(string asunto)
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var id = await marta.CrearTiqueteAsync(asunto, _app.IdCategoria(Datos.CategoriaAcceso));
            return (await _app.IniciarSesionAsync(Datos.SoporteJuan), id);
        }

        private static bool Muestra(string html, int id) => Regex.IsMatch(html, $@"<b>#{id}</b>");

        [Fact]
        [Trait("HU", "HU-06")]
        public async Task La_bandeja_muestra_tiquetes_reales_y_filtra()
        {
            var (juan, id) = await PrepararAsync("Bandeja filtrable");

            Assert.True(Muestra(await juan.GetHtmlAsync("/Soporte/Index"), id));
            Assert.True(Muestra(await juan.GetHtmlAsync("/Soporte/Index?estado=Nuevo"), id));
            Assert.False(Muestra(await juan.GetHtmlAsync("/Soporte/Index?estado=En%20progreso"), id));
            Assert.True(Muestra(await juan.GetHtmlAsync($"/Soporte/Index?categoria={_app.IdCategoria(Datos.CategoriaAcceso)}"), id));
            Assert.False(Muestra(await juan.GetHtmlAsync($"/Soporte/Index?categoria={_app.IdCategoria(Datos.CategoriaLaboratorio)}"), id));
            Assert.True(Muestra(await juan.GetHtmlAsync("/Soporte/Index?q=bandeja%20FILTRABLE"), id));
            Assert.True(Muestra(await juan.GetHtmlAsync($"/Soporte/Index?q=%23{id}"), id));
        }

        [Fact]
        [Trait("HU", "HU-07")]
        [Trait("HU", "HU-08")]
        public async Task Asignar_clasificar_y_ver_el_historial()
        {
            var (juan, id) = await PrepararAsync("Asignación");
            var detalle = $"/Soporte/Detalle/{id}";

            await juan.PostAsync($"/Soporte/Actualizar/{id}", detalle, new()
            {
                ["ResponsableId"] = _app.IdUsuario(Datos.SoporteLaura).ToString(),
                ["Prioridad"] = "Alta",
                ["CategoriaId"] = _app.IdCategoria(Datos.CategoriaLaboratorio).ToString()
            });

            var html = await juan.GetHtmlAsync(detalle);
            Assert.Contains("Cambios guardados.", html);
            Assert.Contains("class=\"st s-asignado\"", html);
            Assert.Contains("Asignado a Laura Vega. Estado: Asignado.", html);
            Assert.Contains("Prioridad cambiada de Sin prioridad a Alta.", html);
            Assert.Contains("Categoría cambiada de Acceso a Laboratorio.", html);
            Assert.True(Muestra(await juan.GetHtmlAsync("/Soporte/Index?prioridad=Alta"), id));
        }

        [Fact]
        [Trait("HU", "HU-08")]
        [Trait("RF", "RF-12")]
        public async Task Transicion_invalida_muestra_mensaje_y_no_cambia()
        {
            var (juan, id) = await PrepararAsync("Salto de estado");
            var detalle = $"/Soporte/Detalle/{id}";

            await juan.PostAsync($"/Soporte/CambiarEstado/{id}", detalle, new() { ["Estado"] = "Resuelto" });

            var html = await juan.GetHtmlAsync(detalle);
            Assert.Contains("No se puede pasar de «Nuevo» a «Resuelto».", html);
            Assert.Contains("class=\"st s-nuevo\"", html);
        }

        [Fact]
        [Trait("HU", "HU-09")]
        public async Task Flujo_completo_hasta_resuelto_y_reabrir()
        {
            var (juan, id) = await PrepararAsync("Flujo completo");
            var detalle = $"/Soporte/Detalle/{id}";

            await LlevarAResueltoAsync(_app, juan, id, "Se actualizó el controlador.");
            var resuelto = await juan.GetHtmlAsync(detalle);
            Assert.Contains("class=\"st s-resuelto\"", resuelto);
            Assert.Contains("Se actualizó el controlador.", resuelto);
            Assert.Contains("Reabrir (En progreso)", resuelto);
            Assert.DoesNotContain("Registrar solución aplicada", resuelto);

            await juan.PostAsync($"/Soporte/CambiarEstado/{id}", detalle, new() { ["Estado"] = "EnProgreso" });
            Assert.Contains("Tiquete reabierto.", await juan.GetHtmlAsync(detalle));
        }

        [Fact]
        [Trait("RF", "RF-13")]
        public async Task Soporte_agrega_comentarios()
        {
            var (juan, id) = await PrepararAsync("Comentario soporte");
            var detalle = $"/Soporte/Detalle/{id}";

            await juan.PostAsync($"/Soporte/Comentar/{id}", detalle, new() { ["Comentario"] = "Revisando logs del servidor." });

            Assert.Contains("Revisando logs del servidor.", await juan.GetHtmlAsync(detalle));
        }

        [Fact]
        public async Task Los_cerrados_pasan_al_historial()
        {
            var marta = await _app.IniciarSesionAsync(Datos.Solicitante);
            var (juan, id) = await PrepararAsync("Para historial");
            await LlevarAResueltoAsync(_app, juan, id, "Listo.");
            await marta.PostAsync($"/Tickets/Cerrar/{id}", $"/Tickets/Detalle/{id}");

            Assert.False(Muestra(await juan.GetHtmlAsync("/Soporte/Index"), id));
            Assert.True(Muestra(await juan.GetHtmlAsync("/Soporte/Historial"), id));
            Assert.DoesNotContain("Guardar cambios", await juan.GetHtmlAsync($"/Soporte/Detalle/{id}"));
        }

        [Fact]
        public async Task Tiquete_inexistente_da_404()
        {
            var juan = await _app.IniciarSesionAsync(Datos.SoporteJuan);
            Assert.Equal(HttpStatusCode.NotFound, (await juan.GetAsync("/Soporte/Detalle/999999")).StatusCode);
        }
    }
}
