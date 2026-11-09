using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Citas.Models.Entidades;

namespace Sistema_Gestión_Citas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Tiquete> Tiquetes => Set<Tiquete>();
        public DbSet<HistorialTiquete> HistorialTiquetes => Set<HistorialTiquete>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rol>(e =>
            {
                e.Property(r => r.Nombre).HasMaxLength(50).IsRequired();
                e.Property(r => r.Descripcion).HasMaxLength(200);
                e.HasIndex(r => r.Nombre).IsUnique();
            });

            modelBuilder.Entity<Usuario>(e =>
            {
                e.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
                e.Property(u => u.Correo).HasMaxLength(150).IsRequired();
                e.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
                e.HasIndex(u => u.Correo).IsUnique();
                e.HasOne(u => u.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(u => u.RolId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Categoria>(e =>
            {
                e.Property(c => c.Nombre).HasMaxLength(80).IsRequired();
                e.Property(c => c.Descripcion).HasMaxLength(250);
                e.HasIndex(c => c.Nombre).IsUnique();
            });

            modelBuilder.Entity<Tiquete>(e =>
            {
                // Número de tiquete único y legible: #1000, #1001, ...
                e.Property(t => t.Id).UseIdentityColumn(1000, 1);
                e.Property(t => t.Asunto).HasMaxLength(120).IsRequired();
                e.Property(t => t.Descripcion).HasMaxLength(4000).IsRequired();
                e.Property(t => t.Solucion).HasMaxLength(4000);
                e.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20);
                e.Property(t => t.Prioridad).HasConversion<string>().HasMaxLength(10);

                e.HasOne(t => t.Categoria)
                    .WithMany(c => c.Tiquetes)
                    .HasForeignKey(t => t.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.Solicitante)
                    .WithMany(u => u.TiquetesCreados)
                    .HasForeignKey(t => t.SolicitanteId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.Responsable)
                    .WithMany(u => u.TiquetesAsignados)
                    .HasForeignKey(t => t.ResponsableId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índices para los filtros de la bandeja (RNF-01).
                e.HasIndex(t => t.Estado);
                e.HasIndex(t => t.Prioridad);
                e.HasIndex(t => t.SolicitanteId);
            });

            modelBuilder.Entity<HistorialTiquete>(e =>
            {
                e.Property(h => h.Descripcion).HasMaxLength(4000).IsRequired();
                e.Property(h => h.Tipo).HasConversion<string>().HasMaxLength(20);
                e.HasOne(h => h.Tiquete)
                    .WithMany(t => t.Historial)
                    .HasForeignKey(h => h.TiqueteId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(h => h.Usuario)
                    .WithMany()
                    .HasForeignKey(h => h.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
