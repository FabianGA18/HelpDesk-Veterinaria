namespace Sistema_Gestión_Citas.Models.Entidades
{
    /// <summary>
    /// Rol del sistema. Los nombres se usan como claim de rol en la autenticación,
    /// por eso deben coincidir con las constantes de <see cref="Roles"/>.
    /// </summary>
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }

    /// <summary>Nombres de los roles, para usar en [Authorize(Roles = ...)].</summary>
    public static class Roles
    {
        public const string Solicitante = "Solicitante";
        public const string Soporte = "Soporte";
        public const string Administrador = "Administrador";

        /// <summary>Personal que gestiona tiquetes (soporte y administradores).</summary>
        public const string Gestores = Soporte + "," + Administrador;
    }
}
