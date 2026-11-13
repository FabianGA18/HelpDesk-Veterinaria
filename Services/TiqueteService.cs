using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Data;
using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Services
{
    public class TiqueteService : ITiqueteService
    {
        private readonly AppDbContext _db;

        public TiqueteService(AppDbContext db) => _db = db;

        public Task<List<Categoria>> ObtenerCategoriasActivasAsync() =>
            _db.Categorias.AsNoTracking()
                .Where(c => c.Activa)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

        public async Task<Resultado<int>> CrearAsync(int solicitanteId, string asunto, int categoriaId, string descripcion)
        {
            asunto = asunto.Trim();
            descripcion = descripcion.Trim();

            if (string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(descripcion))
                return Resultado<int>.Falla("Completa asunto, categoría y descripción.");

            var categoriaValida = await _db.Categorias.AnyAsync(c => c.Id == categoriaId && c.Activa);
            if (!categoriaValida)
                return Resultado<int>.Falla("La categoría seleccionada no existe o está desactivada.");

            var ahora = DateTime.Now;
            var tiquete = new Tiquete
            {
                Asunto = asunto,
                Descripcion = descripcion,
                CategoriaId = categoriaId,
                SolicitanteId = solicitanteId,
                Estado = EstadoTiquete.Nuevo,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };
            tiquete.Historial.Add(new HistorialTiquete
            {
                UsuarioId = solicitanteId,
                Tipo = TipoEvento.Creacion,
                Descripcion = "Tiquete creado con estado «Nuevo».",
                Fecha = ahora
            });

            _db.Tiquetes.Add(tiquete);
            await _db.SaveChangesAsync();
            return Resultado<int>.Ok(tiquete.Id);
        }

        public Task<List<Tiquete>> ListarDelSolicitanteAsync(int solicitanteId) =>
            _db.Tiquetes.AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Responsable)
                .Where(t => t.SolicitanteId == solicitanteId)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

        public Task<Tiquete?> ObtenerDetalleAsync(int id, int? soloDelSolicitanteId = null)
        {
            var consulta = _db.Tiquetes.AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Solicitante)
                .Include(t => t.Responsable)
                .Include(t => t.Historial).ThenInclude(h => h.Usuario)
                .Include(t => t.Evidencias)
                .AsSplitQuery()
                .Where(t => t.Id == id);

            if (soloDelSolicitanteId is int sid)
                consulta = consulta.Where(t => t.SolicitanteId == sid);

            return consulta.FirstOrDefaultAsync();
        }

        // ----------------------------------------------------------------------
        // Soporte (iteración 2)
        // ----------------------------------------------------------------------

        public Task<List<Tiquete>> BuscarAsync(FiltroBandeja filtro)
        {
            var consulta = _db.Tiquetes.AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Responsable)
                .Include(t => t.Solicitante)
                .AsQueryable();

            consulta = filtro.Estado is EstadoTiquete estado
                ? consulta.Where(t => t.Estado == estado)
                : consulta.Where(t => t.Estado != EstadoTiquete.Cerrado);

            if (filtro.Prioridad is PrioridadTiquete prioridad)
                consulta = consulta.Where(t => t.Prioridad == prioridad);

            if (filtro.CategoriaId is int categoriaId)
                consulta = consulta.Where(t => t.CategoriaId == categoriaId);

            var texto = filtro.Busqueda?.Trim().TrimStart('#');
            if (!string.IsNullOrEmpty(texto))
            {
                // LIKE sin distinguir mayúsculas; se escapan los comodines que escriba el usuario.
                var patron = "%" + texto.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
                consulta = int.TryParse(texto, out var numero)
                    ? consulta.Where(t => t.Id == numero || EF.Functions.Like(t.Asunto, patron))
                    : consulta.Where(t => EF.Functions.Like(t.Asunto, patron));
            }

            // Alta primero, luego Media, Baja y al final los que no tienen prioridad; después los más recientes.
            return consulta
                .OrderBy(t => t.Prioridad == PrioridadTiquete.Alta ? 0
                            : t.Prioridad == PrioridadTiquete.Media ? 1
                            : t.Prioridad == PrioridadTiquete.Baja ? 2 : 3)
                .ThenByDescending(t => t.FechaCreacion)
                .ToListAsync();
        }

        public Task<List<Usuario>> ObtenerAgentesAsync() =>
            _db.Usuarios.AsNoTracking()
                .Where(u => u.Activo && (u.Rol.Nombre == Roles.Soporte || u.Rol.Nombre == Roles.Administrador))
                .OrderBy(u => u.Nombre)
                .ToListAsync();

        public Task<List<Categoria>> ObtenerCategoriasAsync() =>
            _db.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();

        public async Task<Resultado> ActualizarAsync(int id, int actorId, int? responsableId, PrioridadTiquete? prioridad, int categoriaId)
        {
            var tiquete = await _db.Tiquetes
                .Include(t => t.Responsable)
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tiquete is null)
                return Resultado.Falla("El tiquete no existe.");
            if (tiquete.Estado == EstadoTiquete.Cerrado)
                return Resultado.Falla("El tiquete está cerrado y ya no se puede modificar.");

            var eventos = new List<(TipoEvento Tipo, string Texto)>();

            // --- Responsable (RF-09) ---
            if (responsableId != tiquete.ResponsableId)
            {
                if (responsableId is int nuevoId)
                {
                    var agente = await _db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == nuevoId);
                    if (agente is null || !agente.Activo ||
                        (agente.Rol.Nombre != Roles.Soporte && agente.Rol.Nombre != Roles.Administrador))
                        return Resultado.Falla("El responsable seleccionado no es un agente de soporte activo.");

                    tiquete.ResponsableId = agente.Id;
                    tiquete.Responsable = agente;
                    if (tiquete.Estado == EstadoTiquete.Nuevo)
                    {
                        tiquete.Estado = EstadoTiquete.Asignado;
                        eventos.Add((TipoEvento.Asignacion, $"Asignado a {agente.Nombre}. Estado: Asignado."));
                    }
                    else
                    {
                        eventos.Add((TipoEvento.Asignacion, $"Responsable cambiado a {agente.Nombre}."));
                    }
                }
                else
                {
                    // Solo se puede dejar sin responsable mientras nadie ha empezado a trabajarlo.
                    if (tiquete.Estado != EstadoTiquete.Asignado)
                        return Resultado.Falla("Solo se puede quitar el responsable de un tiquete en estado «Asignado».");

                    tiquete.ResponsableId = null;
                    tiquete.Responsable = null;
                    tiquete.Estado = EstadoTiquete.Nuevo;
                    eventos.Add((TipoEvento.Asignacion, "Se quitó el responsable. Estado: Nuevo."));
                }
            }

            // --- Prioridad (RF-10) ---
            if (prioridad != tiquete.Prioridad)
            {
                if (prioridad is null)
                    return Resultado.Falla("Selecciona una prioridad: Alta, Media o Baja.");
                if (!Enum.IsDefined(prioridad.Value))
                    return Resultado.Falla("La prioridad no es válida.");

                eventos.Add((TipoEvento.CambioPrioridad,
                    $"Prioridad cambiada de {tiquete.Prioridad.Texto()} a {prioridad.Value.Texto()}."));
                tiquete.Prioridad = prioridad;
            }

            // --- Categoría (RF-11) ---
            if (categoriaId != tiquete.CategoriaId)
            {
                var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == categoriaId && c.Activa);
                if (categoria is null)
                    return Resultado.Falla("La categoría seleccionada no existe o está desactivada.");

                eventos.Add((TipoEvento.CambioCategoria,
                    $"Categoría cambiada de {tiquete.Categoria.Nombre} a {categoria.Nombre}."));
                tiquete.CategoriaId = categoria.Id;
            }

            if (eventos.Count == 0)
                return Resultado.Falla("No hay cambios que guardar.");

            await GuardarConEventosAsync(tiquete, actorId, eventos);
            return Resultado.Ok();
        }

        public async Task<Resultado> CambiarEstadoAsync(int id, int actorId, EstadoTiquete nuevoEstado)
        {
            var tiquete = await _db.Tiquetes.FirstOrDefaultAsync(t => t.Id == id);
            if (tiquete is null)
                return Resultado.Falla("El tiquete no existe.");

            if (!FlujoEstados.TransicionesManuales(tiquete.Estado).Contains(nuevoEstado))
                return Resultado.Falla(
                    $"No se puede pasar de «{tiquete.Estado.Texto()}» a «{nuevoEstado.Texto()}».");

            var anterior = tiquete.Estado;
            tiquete.Estado = nuevoEstado;
            var texto = anterior == EstadoTiquete.Resuelto
                ? $"Tiquete reabierto. Estado cambiado de {anterior.Texto()} a {nuevoEstado.Texto()}."
                : $"Estado cambiado de {anterior.Texto()} a {nuevoEstado.Texto()}.";

            await GuardarConEventosAsync(tiquete, actorId, new() { (TipoEvento.CambioEstado, texto) });
            return Resultado.Ok();
        }

        public async Task<Resultado> RegistrarSolucionAsync(int id, int actorId, string solucion)
        {
            solucion = solucion?.Trim() ?? string.Empty;
            if (solucion.Length == 0)
                return Resultado.Falla("Describe la solución aplicada.");
            if (solucion.Length > 4000)
                return Resultado.Falla("La solución admite máximo 4000 caracteres.");

            var tiquete = await _db.Tiquetes.FirstOrDefaultAsync(t => t.Id == id);
            if (tiquete is null)
                return Resultado.Falla("El tiquete no existe.");
            if (!FlujoEstados.EsValida(tiquete.Estado, EstadoTiquete.Resuelto))
                return Resultado.Falla("Solo se puede registrar la solución de un tiquete «En progreso».");

            tiquete.Solucion = solucion;
            tiquete.Estado = EstadoTiquete.Resuelto;

            await GuardarConEventosAsync(tiquete, actorId, new()
            {
                (TipoEvento.Solucion, "Solución registrada: " + solucion),
                (TipoEvento.CambioEstado, "Estado cambiado de En progreso a Resuelto.")
            });
            return Resultado.Ok();
        }

        public async Task<Resultado> ComentarAsync(int id, int actorId, string comentario, int? soloDelSolicitanteId = null)
        {
            comentario = comentario?.Trim() ?? string.Empty;
            if (comentario.Length == 0)
                return Resultado.Falla("Escribe un comentario.");
            if (comentario.Length > 4000)
                return Resultado.Falla("El comentario admite máximo 4000 caracteres.");

            var tiquete = await _db.Tiquetes.FirstOrDefaultAsync(t => t.Id == id);
            if (tiquete is null || (soloDelSolicitanteId is int sid && tiquete.SolicitanteId != sid))
                return Resultado.Falla("El tiquete no existe.");
            if (tiquete.Estado == EstadoTiquete.Cerrado)
                return Resultado.Falla("El tiquete está cerrado y ya no admite comentarios.");

            await GuardarConEventosAsync(tiquete, actorId, new() { (TipoEvento.Comentario, comentario) });
            return Resultado.Ok();
        }

        // ----------------------------------------------------------------------
        // Solicitante y administración (iteración 3)
        // ----------------------------------------------------------------------

        public async Task<Resultado> CerrarAsync(int id, int solicitanteId)
        {
            var tiquete = await _db.Tiquetes.FirstOrDefaultAsync(t => t.Id == id && t.SolicitanteId == solicitanteId);
            if (tiquete is null)
                return Resultado.Falla("El tiquete no existe.");
            if (!FlujoEstados.EsValida(tiquete.Estado, EstadoTiquete.Cerrado))
                return Resultado.Falla("Solo puedes cerrar un tiquete que esté en estado «Resuelto».");

            tiquete.Estado = EstadoTiquete.Cerrado;
            tiquete.FechaCierre = DateTime.Now;
            await GuardarConEventosAsync(tiquete, solicitanteId, new()
            {
                (TipoEvento.CambioEstado, "El solicitante confirmó la solución. Estado cambiado de Resuelto a Cerrado.")
            });
            return Resultado.Ok();
        }

        public async Task<Dictionary<PrioridadTiquete, int>> ContarAbiertosPorPrioridadAsync()
        {
            var conteos = await _db.Tiquetes.AsNoTracking()
                .Where(t => t.Estado != EstadoTiquete.Cerrado && t.Prioridad != null)
                .GroupBy(t => t.Prioridad!.Value)
                .Select(g => new { Prioridad = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            return Enum.GetValues<PrioridadTiquete>().ToDictionary(
                p => p,
                p => conteos.FirstOrDefault(c => c.Prioridad == p)?.Cantidad ?? 0);
        }

        public Task<int> ContarAbiertosAsync() =>
            _db.Tiquetes.CountAsync(t => t.Estado != EstadoTiquete.Cerrado);

        /// <summary>Agrega los eventos al historial, actualiza la fecha de modificación y guarda.</summary>
        private async Task GuardarConEventosAsync(Tiquete tiquete, int actorId, List<(TipoEvento Tipo, string Texto)> eventos)
        {
            var ahora = DateTime.Now;
            tiquete.FechaActualizacion = ahora;
            foreach (var (tipo, texto) in eventos)
            {
                _db.HistorialTiquetes.Add(new HistorialTiquete
                {
                    TiqueteId = tiquete.Id,
                    UsuarioId = actorId,
                    Tipo = tipo,
                    Descripcion = texto,
                    Fecha = ahora
                });
            }
            await _db.SaveChangesAsync();
        }
    }
}
