using HelpDesk.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Services;

namespace HelpDesk.Tests.Servicios
{
    /// <summary>Reglas de negocio de los tiquetes probadas directamente sobre el servicio.</summary>
    public class TiqueteServiceTests : IDisposable
    {
        private readonly BaseConDatos _base = new();
        private readonly int _solicitante, _juan, _laura, _admin, _acceso, _laboratorio;

        public TiqueteServiceTests()
        {
            _solicitante = _base.IdUsuario(Datos.Solicitante);
            _juan = _base.IdUsuario(Datos.SoporteJuan);
            _laura = _base.IdUsuario(Datos.SoporteLaura);
            _admin = _base.IdUsuario(Datos.Admin);
            _acceso = _base.IdCategoria(Datos.CategoriaAcceso);
            _laboratorio = _base.IdCategoria(Datos.CategoriaLaboratorio);
        }

        public void Dispose() => _base.Dispose();

        private TiqueteService Servicio() => new(_base.Db());

        private async Task<int> CrearAsync(string asunto = "Prueba")
        {
            var r = await Servicio().CrearAsync(_solicitante, asunto, _acceso, "Descripción");
            Assert.True(r.Exito, r.Error);
            return r.Valor;
        }

        private Tiquete Leer(int id)
        {
            using var db = _base.Db();
            return db.Tiquetes.Include(t => t.Historial).AsNoTracking().Single(t => t.Id == id);
        }

        /// <summary>Lleva un tiquete nuevo hasta «En progreso» (asignado a Juan).</summary>
        private async Task<int> CrearEnProgresoAsync()
        {
            var id = await CrearAsync();
            Assert.True((await Servicio().ActualizarAsync(id, _juan, _juan, PrioridadTiquete.Media, _acceso)).Exito);
            Assert.True((await Servicio().CambiarEstadoAsync(id, _juan, EstadoTiquete.EnProgreso)).Exito);
            return id;
        }

        // ---------- HU-02 / RF-02 / RF-03 ----------

        [Fact]
        [Trait("RF", "RF-03")]
        public async Task Crear_asigna_estado_Nuevo_fecha_e_historial()
        {
            var antes = DateTime.Now.AddSeconds(-1);

            var id = await CrearAsync("Falla de login");

            var t = Leer(id);
            Assert.Equal(EstadoTiquete.Nuevo, t.Estado);
            Assert.Null(t.Prioridad);
            Assert.Null(t.ResponsableId);
            Assert.InRange(t.FechaCreacion, antes, DateTime.Now);
            var evento = Assert.Single(t.Historial);
            Assert.Equal(TipoEvento.Creacion, evento.Tipo);
        }

        [Fact]
        [Trait("RF", "RF-03")]
        public async Task Cada_tiquete_recibe_un_numero_unico()
        {
            var ids = new[] { await CrearAsync(), await CrearAsync(), await CrearAsync() };
            Assert.Equal(3, ids.Distinct().Count());
        }

        [Theory]
        [Trait("RF", "RF-02")]
        [InlineData("", "descripción")]
        [InlineData("   ", "descripción")]
        [InlineData("asunto", "")]
        public async Task Crear_exige_asunto_y_descripcion(string asunto, string descripcion)
        {
            var r = await Servicio().CrearAsync(_solicitante, asunto, _acceso, descripcion);
            Assert.False(r.Exito);
        }

        [Fact]
        [Trait("RF", "RF-02")]
        public async Task Crear_rechaza_categoria_desactivada()
        {
            using (var db = _base.Db())
            {
                db.Categorias.Single(c => c.Id == _laboratorio).Activa = false;
                db.SaveChanges();
            }

            var r = await Servicio().CrearAsync(_solicitante, "x", _laboratorio, "y");

            Assert.False(r.Exito);
            Assert.Contains("desactivada", r.Error);
        }

        // ---------- HU-07 / RF-09 ----------

        [Fact]
        [Trait("RF", "RF-09")]
        public async Task Asignar_un_tiquete_Nuevo_lo_pasa_a_Asignado()
        {
            var id = await CrearAsync();

            var r = await Servicio().ActualizarAsync(id, _juan, _laura, null, _acceso);

            Assert.True(r.Exito, r.Error);
            var t = Leer(id);
            Assert.Equal(EstadoTiquete.Asignado, t.Estado);
            Assert.Equal(_laura, t.ResponsableId);
            Assert.Contains(t.Historial, h => h.Tipo == TipoEvento.Asignacion && h.Descripcion.Contains("Laura Vega"));
        }

        [Fact]
        [Trait("RF", "RF-09")]
        public async Task Solo_se_asigna_a_personal_de_soporte_o_admin()
        {
            var id = await CrearAsync();

            var r = await Servicio().ActualizarAsync(id, _juan, _solicitante, null, _acceso);

            Assert.False(r.Exito);
            Assert.Equal(EstadoTiquete.Nuevo, Leer(id).Estado);
        }

        [Fact]
        [Trait("RF", "RF-09")]
        public async Task El_administrador_tambien_puede_ser_responsable()
        {
            var id = await CrearAsync();
            Assert.True((await Servicio().ActualizarAsync(id, _juan, _admin, null, _acceso)).Exito);
        }

        [Fact]
        [Trait("RF", "RF-09")]
        public async Task Quitar_responsable_de_Asignado_vuelve_a_Nuevo()
        {
            var id = await CrearAsync();
            await Servicio().ActualizarAsync(id, _juan, _juan, null, _acceso);

            var r = await Servicio().ActualizarAsync(id, _juan, null, null, _acceso);

            Assert.True(r.Exito, r.Error);
            Assert.Equal(EstadoTiquete.Nuevo, Leer(id).Estado);
            Assert.Null(Leer(id).ResponsableId);
        }

        [Fact]
        [Trait("RF", "RF-09")]
        public async Task No_se_quita_el_responsable_en_progreso()
        {
            var id = await CrearEnProgresoAsync();

            var r = await Servicio().ActualizarAsync(id, _juan, null, PrioridadTiquete.Media, _acceso);

            Assert.False(r.Exito);
            Assert.Equal(_juan, Leer(id).ResponsableId);
        }

        // ---------- HU-08 / RF-10 / RF-11 ----------

        [Fact]
        [Trait("RF", "RF-10")]
        [Trait("RF", "RF-11")]
        public async Task Cambiar_prioridad_y_categoria_queda_en_el_historial()
        {
            var id = await CrearAsync();

            var r = await Servicio().ActualizarAsync(id, _juan, null, PrioridadTiquete.Alta, _laboratorio);

            Assert.True(r.Exito, r.Error);
            var t = Leer(id);
            Assert.Equal(PrioridadTiquete.Alta, t.Prioridad);
            Assert.Equal(_laboratorio, t.CategoriaId);
            Assert.Contains(t.Historial, h => h.Tipo == TipoEvento.CambioPrioridad);
            Assert.Contains(t.Historial, h => h.Tipo == TipoEvento.CambioCategoria);
        }

        [Fact]
        public async Task Guardar_sin_cambios_avisa()
        {
            var id = await CrearAsync();
            var r = await Servicio().ActualizarAsync(id, _juan, null, null, _acceso);
            Assert.False(r.Exito);
            Assert.Contains("No hay cambios", r.Error);
        }

        // ---------- HU-08 / RF-12 ----------

        [Fact]
        [Trait("RF", "RF-12")]
        public async Task Cambiar_estado_rechaza_saltos_del_flujo()
        {
            var id = await CrearAsync();

            var r = await Servicio().CambiarEstadoAsync(id, _juan, EstadoTiquete.Resuelto);

            Assert.False(r.Exito);
            Assert.Equal(EstadoTiquete.Nuevo, Leer(id).Estado);
        }

        [Fact]
        [Trait("RF", "RF-12")]
        public async Task En_progreso_y_en_espera_se_alternan()
        {
            var id = await CrearEnProgresoAsync();

            Assert.True((await Servicio().CambiarEstadoAsync(id, _juan, EstadoTiquete.EnEspera)).Exito);
            Assert.Equal(EstadoTiquete.EnEspera, Leer(id).Estado);
            Assert.True((await Servicio().CambiarEstadoAsync(id, _juan, EstadoTiquete.EnProgreso)).Exito);
            Assert.Equal(EstadoTiquete.EnProgreso, Leer(id).Estado);
        }

        // ---------- HU-09 / RF-13 ----------

        [Fact]
        [Trait("RF", "RF-13")]
        public async Task Registrar_solucion_pasa_a_Resuelto()
        {
            var id = await CrearEnProgresoAsync();

            var r = await Servicio().RegistrarSolucionAsync(id, _juan, "Se reinició el servidor.");

            Assert.True(r.Exito, r.Error);
            var t = Leer(id);
            Assert.Equal(EstadoTiquete.Resuelto, t.Estado);
            Assert.Equal("Se reinició el servidor.", t.Solucion);
            Assert.Contains(t.Historial, h => h.Tipo == TipoEvento.Solucion);
        }

        [Fact]
        [Trait("RF", "RF-13")]
        public async Task Solucion_solo_en_progreso()
        {
            var id = await CrearAsync();
            var r = await Servicio().RegistrarSolucionAsync(id, _juan, "x");
            Assert.False(r.Exito);
        }

        [Fact]
        [Trait("RF", "RF-13")]
        public async Task Solucion_no_puede_estar_vacia()
        {
            var id = await CrearEnProgresoAsync();
            var r = await Servicio().RegistrarSolucionAsync(id, _juan, "   ");
            Assert.False(r.Exito);
            Assert.Equal(EstadoTiquete.EnProgreso, Leer(id).Estado);
        }

        // ---------- HU-05 / RF-06 / RF-07 ----------

        [Fact]
        [Trait("RF", "RF-07")]
        public async Task Solicitante_cierra_tiquete_resuelto()
        {
            var id = await CrearEnProgresoAsync();
            await Servicio().RegistrarSolucionAsync(id, _juan, "Listo");

            var r = await Servicio().CerrarAsync(id, _solicitante);

            Assert.True(r.Exito, r.Error);
            var t = Leer(id);
            Assert.Equal(EstadoTiquete.Cerrado, t.Estado);
            Assert.NotNull(t.FechaCierre);
        }

        [Fact]
        [Trait("RF", "RF-07")]
        public async Task No_se_cierra_si_no_esta_resuelto()
        {
            var id = await CrearEnProgresoAsync();
            Assert.False((await Servicio().CerrarAsync(id, _solicitante)).Exito);
        }

        [Fact]
        [Trait("RNF", "RNF-03")]
        public async Task Nadie_mas_puede_cerrar_el_tiquete_del_solicitante()
        {
            var id = await CrearEnProgresoAsync();
            await Servicio().RegistrarSolucionAsync(id, _juan, "Listo");

            Assert.False((await Servicio().CerrarAsync(id, _juan)).Exito);
            Assert.Equal(EstadoTiquete.Resuelto, Leer(id).Estado);
        }

        [Fact]
        [Trait("RF", "RF-06")]
        public async Task Un_tiquete_cerrado_no_admite_comentarios_ni_cambios()
        {
            var id = await CrearEnProgresoAsync();
            await Servicio().RegistrarSolucionAsync(id, _juan, "Listo");
            await Servicio().CerrarAsync(id, _solicitante);

            Assert.False((await Servicio().ComentarAsync(id, _solicitante, "hola")).Exito);
            Assert.False((await Servicio().ActualizarAsync(id, _juan, _laura, PrioridadTiquete.Alta, _acceso)).Exito);
            Assert.False((await Servicio().CambiarEstadoAsync(id, _juan, EstadoTiquete.EnProgreso)).Exito);
        }

        [Fact]
        [Trait("RNF", "RNF-03")]
        public async Task Comentar_como_solicitante_exige_ser_el_dueno()
        {
            var id = await CrearAsync();
            var r = await Servicio().ComentarAsync(id, _juan, "intruso", soloDelSolicitanteId: _juan);
            Assert.False(r.Exito);
        }

        // ---------- HU-06 / RF-08 / RF-16 ----------

        [Fact]
        [Trait("RF", "RF-08")]
        public async Task La_bandeja_ordena_por_prioridad_y_excluye_cerrados()
        {
            var baja = await CrearAsync("baja");
            var alta = await CrearAsync("alta");
            var sin = await CrearAsync("sin prioridad");
            await Servicio().ActualizarAsync(baja, _juan, null, PrioridadTiquete.Baja, _acceso);
            await Servicio().ActualizarAsync(alta, _juan, null, PrioridadTiquete.Alta, _acceso);
            var cerrado = await CrearEnProgresoAsync();
            await Servicio().RegistrarSolucionAsync(cerrado, _juan, "x");
            await Servicio().CerrarAsync(cerrado, _solicitante);

            var ids = (await Servicio().BuscarAsync(new FiltroBandeja())).Select(t => t.Id).ToList();

            Assert.DoesNotContain(cerrado, ids);
            Assert.True(ids.IndexOf(alta) < ids.IndexOf(baja));
            Assert.True(ids.IndexOf(baja) < ids.IndexOf(sin));
        }

        [Fact]
        [Trait("RF", "RF-08")]
        public async Task Filtros_por_estado_prioridad_categoria_y_texto()
        {
            var id = await CrearAsync("Impresora de recepción");
            await Servicio().ActualizarAsync(id, _juan, _juan, PrioridadTiquete.Media, _laboratorio);

            async Task<bool> Incluye(FiltroBandeja f) => (await Servicio().BuscarAsync(f)).Any(t => t.Id == id);

            Assert.True(await Incluye(new() { Estado = EstadoTiquete.Asignado }));
            Assert.False(await Incluye(new() { Estado = EstadoTiquete.Nuevo }));
            Assert.True(await Incluye(new() { Prioridad = PrioridadTiquete.Media }));
            Assert.False(await Incluye(new() { Prioridad = PrioridadTiquete.Alta }));
            Assert.True(await Incluye(new() { CategoriaId = _laboratorio }));
            Assert.False(await Incluye(new() { CategoriaId = _acceso }));
            Assert.True(await Incluye(new() { Busqueda = "impresora" }));
            Assert.True(await Incluye(new() { Busqueda = $"#{id}" }));
            Assert.False(await Incluye(new() { Busqueda = "no existe" }));
        }

        [Fact]
        [Trait("RF", "RF-16")]
        public async Task Conteo_de_abiertos_por_prioridad()
        {
            var a1 = await CrearAsync();
            var a2 = await CrearAsync();
            var m = await CrearAsync();
            await CrearAsync(); // sin prioridad
            await Servicio().ActualizarAsync(a1, _juan, null, PrioridadTiquete.Alta, _acceso);
            await Servicio().ActualizarAsync(a2, _juan, null, PrioridadTiquete.Alta, _acceso);
            await Servicio().ActualizarAsync(m, _juan, null, PrioridadTiquete.Media, _acceso);

            var conteos = await Servicio().ContarAbiertosPorPrioridadAsync();

            Assert.Equal(2, conteos[PrioridadTiquete.Alta]);
            Assert.Equal(1, conteos[PrioridadTiquete.Media]);
            Assert.Equal(0, conteos[PrioridadTiquete.Baja]);
            Assert.Equal(4, await Servicio().ContarAbiertosAsync());
        }

        // ---------- HU-04 / RF-05 / RNF-03 ----------

        [Fact]
        [Trait("RNF", "RNF-03")]
        public async Task El_solicitante_solo_obtiene_sus_tiquetes()
        {
            var id = await CrearAsync();

            Assert.NotNull(await Servicio().ObtenerDetalleAsync(id, _solicitante));
            Assert.Null(await Servicio().ObtenerDetalleAsync(id, _juan));
            Assert.All(await Servicio().ListarDelSolicitanteAsync(_solicitante), t => Assert.Equal(_solicitante, t.SolicitanteId));
            Assert.Empty(await Servicio().ListarDelSolicitanteAsync(_juan));
        }
    }
}
