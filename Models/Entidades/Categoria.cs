namespace Sistema_Gestión_Citas.Models.Entidades
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        /// <summary>Las categorías no se eliminan, se desactivan (RF-14).</summary>
        public bool Activa { get; set; } = true;

        public ICollection<Tiquete> Tiquetes { get; set; } = new List<Tiquete>();
    }
}
