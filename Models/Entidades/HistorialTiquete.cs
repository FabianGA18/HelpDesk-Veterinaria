namespace Sistema_Gestión_Citas.Models.Entidades
{
    /// <summary>
    /// Evento del historial de un tiquete: creación, cambios de estado, asignaciones,
    /// comentarios, solución, etc. Los comentarios también se guardan aquí (Tipo = Comentario).
    /// </summary>
    public class HistorialTiquete
    {
        public int Id { get; set; }

        public int TiqueteId { get; set; }
        public Tiquete Tiquete { get; set; } = null!;

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public TipoEvento Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
