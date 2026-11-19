using HelpDesk.Tests.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Services;

namespace HelpDesk.Tests.Servicios
{
    /// <summary>HU-01 (credenciales) y HU-11 / RF-15 (usuarios y roles).</summary>
    public class UsuarioServiceTests : IDisposable
    {
        private readonly BaseConDatos _base = new();

        public void Dispose() => _base.Dispose();

        private UsuarioService Servicio() => new(_base.Db(), _base.Hasher);

        [Fact]
        [Trait("RF", "RF-01")]
        public async Task Credenciales_validas_devuelven_el_usuario_con_su_rol()
        {
            var u = await Servicio().ValidarCredencialesAsync(Datos.SoporteJuan, Datos.Password);

            Assert.NotNull(u);
            Assert.Equal(Roles.Soporte, u.Rol.Nombre);
        }

        [Fact]
        [Trait("RF", "RF-01")]
        public async Task Contrasena_incorrecta_devuelve_null() =>
            Assert.Null(await Servicio().ValidarCredencialesAsync(Datos.SoporteJuan, "otra"));

        [Fact]
        public void Las_contrasenas_se_guardan_cifradas()
        {
            using var db = _base.Db();
            Assert.All(db.Usuarios, u => Assert.NotEqual(Datos.Password, u.PasswordHash));
        }

        [Fact]
        [Trait("RF", "RF-15")]
        public async Task Crear_usuario_con_rol_y_luego_iniciar_sesion()
        {
            var r = await Servicio().CrearAsync("Carlos Rojas", "CRojas@Empresa.com", Roles.Soporte, "Clave1234");

            Assert.True(r.Exito, r.Error);
            var u = await Servicio().ValidarCredencialesAsync("crojas@empresa.com", "Clave1234");
            Assert.NotNull(u);
            Assert.Equal(Roles.Soporte, u.Rol.Nombre);
        }

        [Theory]
        [Trait("RF", "RF-15")]
        [InlineData("", "a@b.com", "Soporte", "Clave1234", "nombre")]
        [InlineData("Ana", "no-es-correo", "Soporte", "Clave1234", "correo")]
        [InlineData("Ana", "x@empresa.com", "Superusuario", "Clave1234", "rol")]
        [InlineData("Ana", "x@empresa.com", "Soporte", "1234567", "8 caracteres")]
        [InlineData("Ana", "MSOLIS@empresa.com", "Soporte", "Clave1234", "Ya existe")]
        public async Task Crear_valida_los_datos(string nombre, string correo, string rol, string password, string mensaje)
        {
            var r = await Servicio().CrearAsync(nombre, correo, rol, password);

            Assert.False(r.Exito);
            Assert.Contains(mensaje, r.Error);
        }

        [Fact]
        [Trait("RF", "RF-15")]
        public async Task Usuario_desactivado_no_puede_iniciar_sesion()
        {
            var id = _base.IdUsuario(Datos.SoporteLaura);
            var admin = _base.IdUsuario(Datos.Admin);

            var r = await Servicio().ActualizarAsync(id, admin, "Laura Vega", Datos.SoporteLaura, Roles.Soporte, activo: false, null);

            Assert.True(r.Exito, r.Error);
            Assert.Null(await Servicio().ValidarCredencialesAsync(Datos.SoporteLaura, Datos.Password));
            Assert.Null(await Servicio().ObtenerActivoAsync(id));
        }

        [Fact]
        [Trait("RF", "RF-15")]
        public async Task Cambiar_rol_y_restablecer_contrasena()
        {
            var id = _base.IdUsuario(Datos.SoporteJuan);
            var admin = _base.IdUsuario(Datos.Admin);

            var r = await Servicio().ActualizarAsync(id, admin, "Juan Guillermo", Datos.SoporteJuan, Roles.Administrador, true, "NuevaClave9");

            Assert.True(r.Exito, r.Error);
            Assert.Null(await Servicio().ValidarCredencialesAsync(Datos.SoporteJuan, Datos.Password));
            var u = await Servicio().ValidarCredencialesAsync(Datos.SoporteJuan, "NuevaClave9");
            Assert.Equal(Roles.Administrador, u!.Rol.Nombre);
        }

        [Fact]
        public async Task El_admin_no_puede_quitarse_su_propio_rol()
        {
            var admin = _base.IdUsuario(Datos.Admin);

            var r = await Servicio().ActualizarAsync(admin, admin, "Ana Mora", Datos.Admin, Roles.Soporte, true, null);

            Assert.False(r.Exito);
        }

        [Fact]
        public async Task Debe_quedar_al_menos_un_administrador_activo()
        {
            var admin = _base.IdUsuario(Datos.Admin);
            var juan = _base.IdUsuario(Datos.SoporteJuan);

            var r = await Servicio().ActualizarAsync(admin, juan, "Ana Mora", Datos.Admin, Roles.Administrador, activo: false, null);

            Assert.False(r.Exito);
            Assert.Contains("al menos un administrador", r.Error);
        }
    }
}
