using System.ComponentModel.DataAnnotations;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Models.ViewModels
{
    public class CategoriaFilaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int TiquetesActivos { get; set; }
        public bool Activa { get; set; }
    }

    /// <summary>Crear o editar categoría (HU-10).</summary>
    public class CategoriaFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, ErrorMessage = "El nombre admite máximo 80 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La descripción admite máximo 250 caracteres.")]
        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;
    }

    public class UsuarioFilaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    /// <summary>Crear o editar usuario y asignar su rol (HU-11).</summary>
    public class UsuarioFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre admite máximo 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        [StringLength(150)]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un rol.")]
        public string Rol { get; set; } = Entidades.Roles.Solicitante;

        public bool Activo { get; set; } = true;

        /// <summary>Obligatoria al crear; al editar, vacía significa «no cambiar».</summary>
        public string? Password { get; set; }

        public List<Rol> Roles { get; set; } = new();
    }

    /// <summary>Tiquetes por prioridad (HU-12, RF-16).</summary>
    public class PrioridadAdminViewModel
    {
        public Dictionary<PrioridadTiquete, int> Conteos { get; set; } = new();
        public int TotalAbiertos { get; set; }
        public PrioridadTiquete? Prioridad { get; set; }
        public List<TiqueteFilaViewModel> Tiquetes { get; set; } = new();
    }
}
