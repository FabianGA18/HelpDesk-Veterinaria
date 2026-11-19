using Sistema_Gestión_Citas.Data;

namespace HelpDesk.Tests.Infraestructura
{
    /// <summary>Usuarios y categorías que crea SeedData.</summary>
    public static class Datos
    {
        public const string Password = SeedData.PasswordPrueba;

        public const string Admin = "amora@empresa.com";
        public const string SoporteJuan = "jguillermo@empresa.com";
        public const string SoporteLaura = "lvega@empresa.com";
        public const string Solicitante = "msolis@empresa.com";

        public const string CategoriaAcceso = "Acceso";
        public const string CategoriaLaboratorio = "Laboratorio";
        public const string CategoriaEquipos = "Equipos médicos";
    }

    /// <summary>Contenido mínimo válido de cada tipo de archivo (los primeros bytes que lo identifican).</summary>
    public static class Archivos
    {
        public static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };
        public static readonly byte[] Jpg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        public static readonly byte[] Pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 prueba");

        /// <summary>PNG válido con el tamaño indicado.</summary>
        public static byte[] PngDeTamano(long bytes)
        {
            var datos = new byte[bytes];
            Png.CopyTo(datos, 0);
            return datos;
        }
    }
}
