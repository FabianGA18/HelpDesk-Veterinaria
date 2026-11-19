using System.Text;
using HelpDesk.Tests.Infraestructura;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Sistema_Gestión_Citas.Services;

namespace HelpDesk.Tests.Unitarias
{
    /// <summary>RNF-06: solo PDF, JPG o PNG de máximo 10 MB.</summary>
    [Trait("RNF", "RNF-06")]
    [Trait("HU", "HU-03")]
    public class ValidacionEvidenciasTests
    {
        private readonly EvidenciaService _servicio = new(
            db: null!, // Validar no usa la base de datos.
            new ConfigurationBuilder().Build(),
            new EntornoFalso());

        private static IFormFile Archivo(string nombre, byte[] contenido) =>
            new FormFile(new MemoryStream(contenido), 0, contenido.Length, "Evidencias", nombre);

        [Theory]
        [InlineData("captura.png")]
        [InlineData("CAPTURA.PNG")]
        public void Acepta_png(string nombre) =>
            Assert.Empty(_servicio.Validar(new[] { Archivo(nombre, Archivos.Png) }));

        [Theory]
        [InlineData("foto.jpg")]
        [InlineData("foto.jpeg")]
        public void Acepta_jpg(string nombre) =>
            Assert.Empty(_servicio.Validar(new[] { Archivo(nombre, Archivos.Jpg) }));

        [Fact]
        public void Acepta_pdf() =>
            Assert.Empty(_servicio.Validar(new[] { Archivo("informe.pdf", Archivos.Pdf) }));

        [Fact]
        public void Acepta_exactamente_10_MB() =>
            Assert.Empty(_servicio.Validar(new[] { Archivo("grande.png", Archivos.PngDeTamano(EvidenciaService.TamanoMaximo)) }));

        [Fact]
        public void Rechaza_mas_de_10_MB()
        {
            var errores = _servicio.Validar(new[] { Archivo("grande.png", Archivos.PngDeTamano(EvidenciaService.TamanoMaximo + 1)) });
            Assert.Contains(errores, e => e.Contains("supera el máximo de 10 MB"));
        }

        [Theory]
        [InlineData("programa.exe")]
        [InlineData("documento.docx")]
        [InlineData("notas.txt")]
        [InlineData("sin_extension")]
        public void Rechaza_otros_tipos(string nombre)
        {
            var errores = _servicio.Validar(new[] { Archivo(nombre, Archivos.Png) });
            Assert.Contains(errores, e => e.Contains("solo se permiten archivos PDF, JPG o PNG"));
        }

        [Fact]
        public void Rechaza_extension_falsa()
        {
            // Un texto renombrado a .png no tiene la firma de PNG.
            var errores = _servicio.Validar(new[] { Archivo("foto.png", Encoding.ASCII.GetBytes("no soy una imagen")) });
            Assert.Contains(errores, e => e.Contains("no corresponde a un archivo PNG"));
        }

        [Fact]
        public void Rechaza_archivo_vacio()
        {
            var errores = _servicio.Validar(new[] { Archivo("vacio.pdf", Array.Empty<byte>()) });
            Assert.Contains(errores, e => e.Contains("está vacío"));
        }

        [Fact]
        public void Rechaza_mas_de_5_archivos()
        {
            var archivos = Enumerable.Range(1, 6).Select(i => Archivo($"f{i}.png", Archivos.Png)).ToList();
            var errores = _servicio.Validar(archivos);
            Assert.Contains(errores, e => e.Contains("máximo 5 archivos"));
        }

        [Fact]
        public void Reporta_cada_archivo_invalido()
        {
            var errores = _servicio.Validar(new[]
            {
                Archivo("bueno.png", Archivos.Png),
                Archivo("malo1.exe", Archivos.Png),
                Archivo("malo2.txt", Archivos.Png)
            });
            Assert.Equal(2, errores.Count);
        }

        private sealed class EntornoFalso : IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = Path.GetTempPath();
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string ApplicationName { get; set; } = "Pruebas";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
            public string ContentRootPath { get; set; } = Path.GetTempPath();
            public string EnvironmentName { get; set; } = "Development";
        }
    }
}
