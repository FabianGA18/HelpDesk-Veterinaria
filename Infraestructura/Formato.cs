using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Infraestructura
{
    /// <summary>Ayudas de presentación: textos y clases CSS para estados, prioridades y fechas.</summary>
    public static class Formato
    {
        private static readonly CultureInfo Es = new("es-CR");

        /// <summary>Texto del enum usando [Display(Name)] si existe (p. ej. «En progreso»).</summary>
        public static string Texto(this Enum valor)
        {
            var miembro = valor.GetType().GetMember(valor.ToString()).FirstOrDefault();
            return miembro?.GetCustomAttribute<DisplayAttribute>()?.Name ?? valor.ToString();
        }

        /// <summary>
        /// Convierte el texto de un filtro («En progreso», «EnProgreso», «Alta», ...) al enum.
        /// Devuelve null si está vacío, es «Todos» o no coincide.
        /// </summary>
        public static T? ParsearEnum<T>(string? texto) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            foreach (var valor in Enum.GetValues<T>())
            {
                if (string.Equals(valor.ToString(), texto, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(valor.Texto(), texto, StringComparison.OrdinalIgnoreCase))
                    return valor;
            }
            return null;
        }

        public static string Texto(this PrioridadTiquete? prioridad) => prioridad?.Texto() ?? "Sin prioridad";

        /// <summary>Clase CSS del estado (site.css: s-nuevo, s-asignado, s-progreso, ...).</summary>
        public static string ClaseCss(this EstadoTiquete estado) => estado switch
        {
            EstadoTiquete.Nuevo => "s-nuevo",
            EstadoTiquete.Asignado => "s-asignado",
            EstadoTiquete.EnProgreso => "s-progreso",
            EstadoTiquete.EnEspera => "s-espera",
            EstadoTiquete.Resuelto => "s-resuelto",
            EstadoTiquete.Cerrado => "s-cerrado",
            _ => ""
        };

        /// <summary>Clase CSS de la prioridad (site.css: p-alta, p-media, p-baja).</summary>
        public static string ClaseCss(this PrioridadTiquete? prioridad) => prioridad switch
        {
            PrioridadTiquete.Alta => "p-alta",
            PrioridadTiquete.Media => "p-media",
            PrioridadTiquete.Baja => "p-baja",
            _ => ""
        };

        public static string Fecha(DateTime fecha) => fecha.ToString("d MMM yyyy, h:mm tt", Es);

        /// <summary>«Hace 12 min», «Hace 3 h», «Hace 2 d».</summary>
        public static string Relativo(DateTime fecha)
        {
            var dif = DateTime.Now - fecha;
            if (dif.TotalMinutes < 1) return "Hace un momento";
            if (dif.TotalMinutes < 60) return $"Hace {(int)dif.TotalMinutes} min";
            if (dif.TotalHours < 48) return $"Hace {(int)dif.TotalHours} h";
            return $"Hace {(int)dif.TotalDays} d";
        }
    }
}
