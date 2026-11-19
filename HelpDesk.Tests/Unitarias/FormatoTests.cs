using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;

namespace HelpDesk.Tests.Unitarias
{
    /// <summary>Conversión de textos de filtros y presentación de estados y prioridades.</summary>
    public class FormatoTests
    {
        [Theory]
        [InlineData("En progreso", EstadoTiquete.EnProgreso)]
        [InlineData("EnProgreso", EstadoTiquete.EnProgreso)]
        [InlineData("en espera", EstadoTiquete.EnEspera)]
        [InlineData("Nuevo", EstadoTiquete.Nuevo)]
        public void Parsea_estado_por_texto_o_nombre(string texto, EstadoTiquete esperado) =>
            Assert.Equal(esperado, Formato.ParsearEnum<EstadoTiquete>(texto));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Todos")]
        [InlineData("Inventado")]
        public void Texto_sin_coincidencia_devuelve_null(string? texto) =>
            Assert.Null(Formato.ParsearEnum<EstadoTiquete>(texto));

        [Theory]
        [InlineData(EstadoTiquete.EnProgreso, "En progreso")]
        [InlineData(EstadoTiquete.EnEspera, "En espera")]
        [InlineData(EstadoTiquete.Resuelto, "Resuelto")]
        public void Texto_de_estado_para_mostrar(EstadoTiquete estado, string esperado) =>
            Assert.Equal(esperado, estado.Texto());

        [Fact]
        public void Prioridad_nula_se_muestra_como_sin_prioridad()
        {
            PrioridadTiquete? prioridad = null;
            Assert.Equal("Sin prioridad", prioridad.Texto());
            Assert.Equal("", prioridad.ClaseCss());
        }

        [Theory]
        [InlineData(PrioridadTiquete.Alta, "p-alta")]
        [InlineData(PrioridadTiquete.Media, "p-media")]
        [InlineData(PrioridadTiquete.Baja, "p-baja")]
        public void Clase_css_de_prioridad(PrioridadTiquete valor, string clase)
        {
            PrioridadTiquete? prioridad = valor;
            Assert.Equal(clase, prioridad.ClaseCss());
        }

        [Fact]
        public void Cada_estado_tiene_clase_css() =>
            Assert.All(Enum.GetValues<EstadoTiquete>(), e => Assert.StartsWith("s-", e.ClaseCss()));
    }
}
