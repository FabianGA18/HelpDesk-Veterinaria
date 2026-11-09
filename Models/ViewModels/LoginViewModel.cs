using System.ComponentModel.DataAnnotations;

namespace Sistema_Gestión_Citas.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingresa tu correo.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa tu contraseña.")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
