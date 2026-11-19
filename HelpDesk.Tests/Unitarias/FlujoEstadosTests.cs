using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Services;

namespace HelpDesk.Tests.Unitarias
{
    /// <summary>RF-12: máquina de estados Nuevo → Asignado → En progreso ⇄ En espera → Resuelto → Cerrado.</summary>
    [Trait("RF", "RF-12")]
    public class FlujoEstadosTests
    {
        [Theory]
        [InlineData(EstadoTiquete.Nuevo, EstadoTiquete.Asignado)]
        [InlineData(EstadoTiquete.Asignado, EstadoTiquete.EnProgreso)]
        [InlineData(EstadoTiquete.Asignado, EstadoTiquete.Nuevo)]
        [InlineData(EstadoTiquete.EnProgreso, EstadoTiquete.EnEspera)]
        [InlineData(EstadoTiquete.EnProgreso, EstadoTiquete.Resuelto)]
        [InlineData(EstadoTiquete.EnEspera, EstadoTiquete.EnProgreso)]
        [InlineData(EstadoTiquete.Resuelto, EstadoTiquete.Cerrado)]
        [InlineData(EstadoTiquete.Resuelto, EstadoTiquete.EnProgreso)]
        public void Transiciones_validas(EstadoTiquete desde, EstadoTiquete hacia) =>
            Assert.True(FlujoEstados.EsValida(desde, hacia));

        [Theory]
        [InlineData(EstadoTiquete.Nuevo, EstadoTiquete.EnProgreso)]
        [InlineData(EstadoTiquete.Nuevo, EstadoTiquete.Resuelto)]
        [InlineData(EstadoTiquete.Nuevo, EstadoTiquete.Cerrado)]
        [InlineData(EstadoTiquete.Asignado, EstadoTiquete.Resuelto)]
        [InlineData(EstadoTiquete.EnProgreso, EstadoTiquete.Cerrado)]
        [InlineData(EstadoTiquete.EnEspera, EstadoTiquete.Resuelto)]
        [InlineData(EstadoTiquete.Cerrado, EstadoTiquete.EnProgreso)]
        [InlineData(EstadoTiquete.Cerrado, EstadoTiquete.Nuevo)]
        public void Transiciones_invalidas(EstadoTiquete desde, EstadoTiquete hacia) =>
            Assert.False(FlujoEstados.EsValida(desde, hacia));

        [Fact]
        public void Un_tiquete_cerrado_no_tiene_transiciones() =>
            Assert.Empty(FlujoEstados.TransicionesManuales(EstadoTiquete.Cerrado));

        /// <summary>
        /// Los botones de «Cambiar estado» no ofrecen Asignado/Nuevo (se hace al asignar),
        /// Resuelto (se hace al registrar la solución) ni Cerrado (lo hace el solicitante).
        /// </summary>
        [Theory]
        [InlineData(EstadoTiquete.Nuevo, new EstadoTiquete[0])]
        [InlineData(EstadoTiquete.Asignado, new[] { EstadoTiquete.EnProgreso })]
        [InlineData(EstadoTiquete.EnProgreso, new[] { EstadoTiquete.EnEspera })]
        [InlineData(EstadoTiquete.EnEspera, new[] { EstadoTiquete.EnProgreso })]
        [InlineData(EstadoTiquete.Resuelto, new[] { EstadoTiquete.EnProgreso })]
        public void Transiciones_manuales_por_estado(EstadoTiquete desde, EstadoTiquete[] esperadas) =>
            Assert.Equal(esperadas, FlujoEstados.TransicionesManuales(desde));
    }
}
