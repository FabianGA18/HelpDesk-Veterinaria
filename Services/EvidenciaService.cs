using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Data;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public class EvidenciaService : IEvidenciaService
    {
        public const long TamanoMaximo = 10 * 1024 * 1024; // 10 MB (RNF-06)
        public const int MaximoArchivosPorEnvio = 5;

        /// <summary>Extensiones permitidas, su tipo MIME y los primeros bytes que debe tener el archivo.</summary>
        private static readonly Dictionary<string, (string Mime, byte[] Firma)> Permitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ("application/pdf", "%PDF"u8.ToArray()),
            [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })
        };

        private readonly AppDbContext _db;
        private readonly string _carpeta;

        public EvidenciaService(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
        {
            _db = db;
            // Fuera de wwwroot para que los archivos solo se puedan bajar con permiso (RNF-03).
            var ruta = config["Evidencias:Ruta"];
            _carpeta = string.IsNullOrWhiteSpace(ruta)
                ? Path.Combine(env.ContentRootPath, "App_Data", "Evidencias")
                : Path.GetFullPath(ruta, env.ContentRootPath);
        }

        public List<string> Validar(IReadOnlyCollection<IFormFile> archivos)
        {
            var errores = new List<string>();
            if (archivos.Count > MaximoArchivosPorEnvio)
                errores.Add($"Puedes adjuntar máximo {MaximoArchivosPorEnvio} archivos a la vez.");

            foreach (var archivo in archivos)
            {
                var nombre = Path.GetFileName(archivo.FileName);
                var extension = Path.GetExtension(nombre);

                if (!Permitidos.TryGetValue(extension, out var tipo))
                    errores.Add($"«{nombre}»: solo se permiten archivos PDF, JPG o PNG.");
                else if (archivo.Length == 0)
                    errores.Add($"«{nombre}»: el archivo está vacío.");
                else if (archivo.Length > TamanoMaximo)
                    errores.Add($"«{nombre}»: supera el máximo de 10 MB.");
                else if (!TieneFirma(archivo, tipo.Firma))
                    errores.Add($"«{nombre}»: el contenido no corresponde a un archivo {extension.TrimStart('.').ToUpper()} válido.");
            }
            return errores;
        }

        public async Task<Resultado> AdjuntarAsync(int tiqueteId, int usuarioId, IReadOnlyCollection<IFormFile> archivos)
        {
            if (archivos.Count == 0)
                return Resultado.Falla("Selecciona al menos un archivo.");

            var errores = Validar(archivos);
            if (errores.Count > 0)
                return Resultado.Falla(string.Join(" ", errores));

            var tiquete = await _db.Tiquetes.FirstOrDefaultAsync(t => t.Id == tiqueteId);
            if (tiquete is null)
                return Resultado.Falla("El tiquete no existe.");
            if (tiquete.Estado == EstadoTiquete.Cerrado)
                return Resultado.Falla("El tiquete está cerrado y ya no admite evidencias.");

            Directory.CreateDirectory(_carpeta);
            var ahora = DateTime.Now;
            var guardados = new List<string>();
            try
            {
                foreach (var archivo in archivos)
                {
                    var nombreOriginal = Path.GetFileName(archivo.FileName);
                    var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
                    var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
                    var ruta = Path.Combine(_carpeta, nombreArchivo);

                    await using (var destino = File.Create(ruta))
                        await archivo.CopyToAsync(destino);
                    guardados.Add(ruta);

                    _db.Evidencias.Add(new Evidencia
                    {
                        TiqueteId = tiquete.Id,
                        SubidoPorId = usuarioId,
                        NombreOriginal = nombreOriginal,
                        NombreArchivo = nombreArchivo,
                        TipoContenido = Permitidos[extension].Mime,
                        TamanoBytes = archivo.Length,
                        FechaSubida = ahora
                    });
                    _db.HistorialTiquetes.Add(new HistorialTiquete
                    {
                        TiqueteId = tiquete.Id,
                        UsuarioId = usuarioId,
                        Tipo = TipoEvento.Evidencia,
                        Descripcion = $"Evidencia adjuntada: {nombreOriginal}.",
                        Fecha = ahora
                    });
                }

                tiquete.FechaActualizacion = ahora;
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Si falla el guardado, no se dejan archivos huérfanos en disco.
                foreach (var ruta in guardados)
                    File.Delete(ruta);
                throw;
            }

            return Resultado.Ok();
        }

        public Task<Evidencia?> ObtenerAsync(int id) =>
            _db.Evidencias.AsNoTracking()
                .Include(v => v.Tiquete)
                .FirstOrDefaultAsync(v => v.Id == id);

        public Stream? AbrirArchivo(Evidencia evidencia)
        {
            var ruta = Path.Combine(_carpeta, Path.GetFileName(evidencia.NombreArchivo));
            return File.Exists(ruta) ? File.OpenRead(ruta) : null;
        }

        private static bool TieneFirma(IFormFile archivo, byte[] firma)
        {
            using var stream = archivo.OpenReadStream();
            var buffer = new byte[firma.Length];
            var leidos = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            return leidos == firma.Length && buffer.AsSpan().SequenceEqual(firma);
        }
    }
}
