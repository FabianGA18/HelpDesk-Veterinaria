using System.ComponentModel.DataAnnotations;

namespace Sistema_Gestión_Citas.Models.Entidades
{
    /// <summary>Estados del flujo del tiquete: Nuevo → Asignado → En progreso → En espera → Resuelto → Cerrado.</summary>
    public enum EstadoTiquete
    {
        Nuevo = 1,
        Asignado = 2,
        [Display(Name = "En progreso")] EnProgreso = 3,
        [Display(Name = "En espera")] EnEspera = 4,
        Resuelto = 5,
        Cerrado = 6
    }

    /// <summary>Niveles de prioridad (RF-10). La define soporte; al crear el tiquete no tiene.</summary>
    public enum PrioridadTiquete
    {
        Baja = 1,
        Media = 2,
        Alta = 3
    }

    /// <summary>Tipo de evento registrado en el historial del tiquete.</summary>
    public enum TipoEvento
    {
        Creacion = 1,
        CambioEstado = 2,
        Asignacion = 3,
        CambioPrioridad = 4,
        CambioCategoria = 5,
        Comentario = 6,
        Solucion = 7,
        Evidencia = 8
    }
}
