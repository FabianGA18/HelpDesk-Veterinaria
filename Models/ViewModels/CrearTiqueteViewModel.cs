using System.ComponentModel.DataAnnotations;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Models.ViewModels
{
    public class CrearTiqueteViewModel
    {
        [Required(ErrorMessage = "El asunto es obligatorio.")]
        [StringLength(120, ErrorMessage = "El asunto admite máximo 120 caracteres.")]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona una categoría.")]
        public int? CategoriaId { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(4000, ErrorMessage = "La descripción admite máximo 4000 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>Opciones del combo de categorías (solo activas).</summary>
        public List<Categoria> Categorias { get; set; } = new();
    }
}
