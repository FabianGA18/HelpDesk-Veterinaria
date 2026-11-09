namespace Sistema_Gestión_Citas.Models.Entidades
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Correo electrónico; se usa como nombre de usuario para iniciar sesión.</summary>
        public string Correo { get; set; } = string.Empty;

        /// <summary>Hash de la contraseña (nunca se guarda la contraseña en texto plano).</summary>
        public string PasswordHash { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int RolId { get; set; }
        public Rol Rol { get; set; } = null!;

        public ICollection<Tiquete> TiquetesCreados { get; set; } = new List<Tiquete>();
        public ICollection<Tiquete> TiquetesAsignados { get; set; } = new List<Tiquete>();
    }
}
