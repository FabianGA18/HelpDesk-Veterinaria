using System.Net;
using System.Text.RegularExpressions;

namespace HelpDesk.Tests.Infraestructura
{
    /// <summary>
    /// Cliente HTTP que se comporta como el navegador: guarda la cookie de sesión y envía el token
    /// antifalsificación que la app exige en todos los formularios POST.
    /// </summary>
    public class ClienteWeb
    {
        public const string UrlLogin = "/Login/InicioSesi%C3%B3n";
        public const string UrlLogout = "/Login/CerrarSesi%C3%B3n";

        public HttpClient Http { get; }

        public ClienteWeb(HttpClient http) => Http = http;

        public Task<HttpResponseMessage> GetAsync(string url) => Http.GetAsync(url);

        /// <summary>HTML de la página con las entidades decodificadas (á, «, », etc.).</summary>
        public async Task<string> GetHtmlAsync(string url)
        {
            var respuesta = await Http.GetAsync(url);
            Assert.True(respuesta.IsSuccessStatusCode, $"GET {url} devolvió HTTP {(int)respuesta.StatusCode}.");
            return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        }

        public static async Task<string> HtmlAsync(HttpResponseMessage respuesta) =>
            WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        /// <summary>Envía un formulario, tomando el token antifalsificación de la página <paramref name="paginaFormulario"/>.</summary>
        public async Task<HttpResponseMessage> PostAsync(string url, string paginaFormulario, Dictionary<string, string>? campos = null)
        {
            campos ??= new();
            campos["__RequestVerificationToken"] = await ObtenerTokenAsync(paginaFormulario);
            return await Http.PostAsync(url, new FormUrlEncodedContent(campos));
        }

        /// <summary>Envía un formulario con archivos en el campo «Evidencias».</summary>
        public async Task<HttpResponseMessage> PostArchivosAsync(string url, string paginaFormulario,
            Dictionary<string, string> campos, params (string Nombre, byte[] Contenido)[] archivos)
        {
            var contenido = new MultipartFormDataContent
            {
                { new StringContent(await ObtenerTokenAsync(paginaFormulario)), "__RequestVerificationToken" }
            };
            foreach (var (clave, valor) in campos)
                contenido.Add(new StringContent(valor), clave);
            foreach (var (nombre, bytes) in archivos)
                contenido.Add(new ByteArrayContent(bytes), "Evidencias", nombre);
            return await Http.PostAsync(url, contenido);
        }

        public Task<HttpResponseMessage> IniciarSesionAsync(string correo, string password) =>
            PostAsync(UrlLogin, UrlLogin, new() { ["Usuario"] = correo, ["Password"] = password });

        /// <summary>Crea un tiquete como solicitante y devuelve su número.</summary>
        public async Task<int> CrearTiqueteAsync(string asunto, int categoriaId, string descripcion = "Descripción de prueba")
        {
            var respuesta = await PostAsync("/Tickets/Crear", "/Tickets/Crear", new()
            {
                ["Asunto"] = asunto,
                ["CategoriaId"] = categoriaId.ToString(),
                ["Descripcion"] = descripcion
            });
            return IdDeRedireccion(respuesta);
        }

        /// <summary>Número de tiquete de una redirección a …/Detalle/{id}.</summary>
        public static int IdDeRedireccion(HttpResponseMessage respuesta)
        {
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            var m = Regex.Match(respuesta.Headers.Location?.ToString() ?? "", @"/Detalle/(\d+)");
            Assert.True(m.Success, $"Redirección inesperada: {respuesta.Headers.Location}");
            return int.Parse(m.Groups[1].Value);
        }

        private async Task<string> ObtenerTokenAsync(string pagina)
        {
            var html = await Http.GetStringAsync(pagina);
            var m = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
            Assert.True(m.Success, $"La página {pagina} no tiene token antifalsificación.");
            return m.Groups[1].Value;
        }
    }
}
