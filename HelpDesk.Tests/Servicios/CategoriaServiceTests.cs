using HelpDesk.Tests.Infraestructura;
using Sistema_Gestión_Citas.Services;

namespace HelpDesk.Tests.Servicios
{
    /// <summary>HU-10 / RF-14: crear, editar y desactivar categorías.</summary>
    [Trait("RF", "RF-14")]
    public class CategoriaServiceTests : IDisposable
    {
        private readonly BaseConDatos _base = new();

        public void Dispose() => _base.Dispose();

        private CategoriaService Servicio() => new(_base.Db());

        [Fact]
        public async Task Crear_categoria_activa()
        {
            var r = await Servicio().CrearAsync("  Facturación  ", "Cobros y pagos");

            Assert.True(r.Exito, r.Error);
            var c = await Servicio().ObtenerAsync(r.Valor);
            Assert.Equal("Facturación", c!.Nombre);
            Assert.True(c.Activa);
        }

        [Theory]
        [InlineData("acceso")]
        [InlineData("ACCESO")]
        public async Task No_se_repiten_nombres(string nombre)
        {
            var r = await Servicio().CrearAsync(nombre, null);
            Assert.False(r.Exito);
            Assert.Contains("Ya existe", r.Error);
        }

        [Fact]
        public async Task El_nombre_es_obligatorio() =>
            Assert.False((await Servicio().CrearAsync("   ", null)).Exito);

        [Fact]
        public async Task Editar_y_desactivar()
        {
            var id = _base.IdCategoria(Datos.CategoriaLaboratorio);

            var r = await Servicio().ActualizarAsync(id, "Laboratorio clínico", null, activa: false);

            Assert.True(r.Exito, r.Error);
            var c = await Servicio().ObtenerAsync(id);
            Assert.Equal("Laboratorio clínico", c!.Nombre);
            Assert.False(c.Activa);
        }

        [Fact]
        public async Task Editar_sin_cambiar_el_nombre_no_choca_consigo_misma()
        {
            var id = _base.IdCategoria(Datos.CategoriaAcceso);
            Assert.True((await Servicio().ActualizarAsync(id, "Acceso", "Nueva descripción", true)).Exito);
        }

        [Fact]
        public async Task Cuenta_solo_tiquetes_abiertos()
        {
            var acceso = _base.IdCategoria(Datos.CategoriaAcceso);
            var solicitante = _base.IdUsuario(Datos.Solicitante);
            var tiquetes = new TiqueteService(_base.Db());
            await tiquetes.CrearAsync(solicitante, "uno", acceso, "x");
            var dos = await tiquetes.CrearAsync(solicitante, "dos", acceso, "x");
            using (var db = _base.Db())
            {
                db.Tiquetes.Single(t => t.Id == dos.Valor).Estado = Sistema_Gestión_Citas.Models.Entidades.EstadoTiquete.Cerrado;
                db.SaveChanges();
            }

            var filas = await Servicio().ListarConConteoAsync();

            Assert.Equal(1, filas.Single(f => f.Categoria.Id == acceso).TiquetesActivos);
        }
    }
}
