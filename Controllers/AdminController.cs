using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_Gestión_Citas.Infraestructura;
using Sistema_Gestión_Citas.Models.Entidades;
using Sistema_Gestión_Citas.Models.ViewModels;
using Sistema_Gestión_Citas.Services;

namespace Sistema_Gestión_Citas.Controllers
{
    /// <summary>
    /// Administración: HU-10 (categorías), HU-11 (usuarios y roles) y HU-12 (tiquetes por prioridad).
    /// RNF-02: solo administradores.
    /// </summary>
    [Authorize(Roles = Roles.Administrador)]
    public class AdminController : Controller
    {
        private readonly ICategoriaService _categorias;
        private readonly IUsuarioService _usuarios;
        private readonly ITiqueteService _tiquetes;

        public AdminController(ICategoriaService categorias, IUsuarioService usuarios, ITiqueteService tiquetes)
        {
            _categorias = categorias;
            _usuarios = usuarios;
            _tiquetes = tiquetes;
        }

        // ------------------------------------------------------------------
        // Categorías (HU-10, RF-14)
        // ------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Categorias()
        {
            var filas = await _categorias.ListarConConteoAsync();
            return View(filas.Select(f => new CategoriaFilaViewModel
            {
                Id = f.Categoria.Id,
                Nombre = f.Categoria.Nombre,
                TiquetesActivos = f.TiquetesActivos,
                Activa = f.Categoria.Activa
            }).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> CategoriaForm(int? id)
        {
            if (id is null)
                return View(new CategoriaFormViewModel());

            var categoria = await _categorias.ObtenerAsync(id.Value);
            if (categoria is null)
                return NotFound();

            return View(new CategoriaFormViewModel
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activa = categoria.Activa
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarCategoria(int? id, CategoriaFormViewModel model)
        {
            model.Id = id;
            if (!ModelState.IsValid)
                return View(nameof(CategoriaForm), model);

            Resultado resultado;
            if (id is null)
            {
                var creada = await _categorias.CrearAsync(model.Nombre, model.Descripcion);
                resultado = new Resultado(creada.Exito, creada.Error);
            }
            else
            {
                resultado = await _categorias.ActualizarAsync(id.Value, model.Nombre, model.Descripcion, model.Activa);
            }

            if (!resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, resultado.Error!);
                return View(nameof(CategoriaForm), model);
            }

            TempData["Ok"] = id is null ? $"Categoría «{model.Nombre.Trim()}» creada." : "Categoría actualizada.";
            return RedirectToAction(nameof(Categorias));
        }

        // ------------------------------------------------------------------
        // Usuarios y roles (HU-11, RF-15)
        // ------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _usuarios.ListarAsync();
            return View(usuarios.Select(u => new UsuarioFilaViewModel
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Correo = u.Correo,
                Rol = u.Rol.Nombre,
                Activo = u.Activo
            }).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> UsuarioForm(int? id)
        {
            var model = new UsuarioFormViewModel { Roles = await _usuarios.ListarRolesAsync() };
            if (id is null)
                return View(model);

            var usuario = await _usuarios.ObtenerAsync(id.Value);
            if (usuario is null)
                return NotFound();

            model.Id = usuario.Id;
            model.Nombre = usuario.Nombre;
            model.Correo = usuario.Correo;
            model.Rol = usuario.Rol.Nombre;
            model.Activo = usuario.Activo;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarUsuario(int? id, UsuarioFormViewModel model)
        {
            model.Id = id;
            if (id is null && string.IsNullOrEmpty(model.Password))
                ModelState.AddModelError(nameof(model.Password), "La contraseña inicial es obligatoria.");

            if (ModelState.IsValid)
            {
                Resultado resultado;
                if (id is null)
                {
                    var creado = await _usuarios.CrearAsync(model.Nombre, model.Correo, model.Rol, model.Password!);
                    resultado = new Resultado(creado.Exito, creado.Error);
                }
                else
                {
                    resultado = await _usuarios.ActualizarAsync(
                        id.Value, User.ObtenerUsuarioId(), model.Nombre, model.Correo, model.Rol, model.Activo, model.Password);
                }

                if (resultado.Exito)
                {
                    TempData["Ok"] = id is null ? $"Usuario «{model.Nombre.Trim()}» creado." : "Usuario actualizado.";
                    return RedirectToAction(nameof(Usuarios));
                }
                ModelState.AddModelError(string.Empty, resultado.Error!);
            }

            model.Password = null;
            model.Roles = await _usuarios.ListarRolesAsync();
            return View(nameof(UsuarioForm), model);
        }

        // ------------------------------------------------------------------
        // Tiquetes por prioridad (HU-12, RF-16)
        // ------------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Prioridad(string? prioridad)
        {
            var filtro = new FiltroBandeja { Prioridad = Formato.ParsearEnum<PrioridadTiquete>(prioridad) };
            var tiquetes = await _tiquetes.BuscarAsync(filtro);

            return View(new PrioridadAdminViewModel
            {
                Conteos = await _tiquetes.ContarAbiertosPorPrioridadAsync(),
                TotalAbiertos = await _tiquetes.ContarAbiertosAsync(),
                Prioridad = filtro.Prioridad,
                Tiquetes = tiquetes.Select(TiqueteFilaViewModel.Desde).ToList()
            });
        }
    }
}
