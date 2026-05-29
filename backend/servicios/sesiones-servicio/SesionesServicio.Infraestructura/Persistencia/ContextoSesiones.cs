using Microsoft.EntityFrameworkCore;

namespace SesionesServicio.Infraestructura.Persistencia;

public sealed class ContextoSesiones : DbContext
{
    public ContextoSesiones(DbContextOptions<ContextoSesiones> opciones) : base(opciones) { }

    public DbSet<SesionModelo> Sesiones => Set<SesionModelo>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.HasDefaultSchema("sesiones");

        constructor.Entity<SesionModelo>(e =>
        {
            e.ToTable("Sesion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            e.Property(x => x.TipoJuego).HasColumnName("tipo_juego").IsRequired();
            e.Property(x => x.ContenidoJuegoId).HasColumnName("contenido_juego_id").IsRequired();
            e.Property(x => x.Modo).HasColumnName("modo").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            e.Property(x => x.FechaProgramada).HasColumnName("fecha_programada").IsRequired();
            e.Property(x => x.CreadaPorUsuarioId).HasColumnName("creada_por_usuario_id");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").IsRequired();

            e.HasIndex(x => x.Estado);
            e.HasIndex(x => x.FechaProgramada);
            e.HasIndex(x => x.TipoJuego);
            e.HasIndex(x => x.ContenidoJuegoId);
        });
    }
}
