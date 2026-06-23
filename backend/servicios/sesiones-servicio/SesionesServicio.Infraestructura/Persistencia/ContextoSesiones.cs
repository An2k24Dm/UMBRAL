using Microsoft.EntityFrameworkCore;

namespace SesionesServicio.Infraestructura.Persistencia;

public sealed class ContextoSesiones : DbContext
{
    public ContextoSesiones(DbContextOptions<ContextoSesiones> opciones) : base(opciones) { }

    public DbSet<SesionModelo> Sesiones => Set<SesionModelo>();
    public DbSet<SesionMisionModelo> SesionMisiones => Set<SesionMisionModelo>();
    public DbSet<EquipoModelo> Equipos => Set<EquipoModelo>();
    public DbSet<ParticipanteModelo> Participantes => Set<ParticipanteModelo>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.HasDefaultSchema("sesiones");

        constructor.Entity<SesionModelo>(e =>
        {
            e.ToTable("Sesion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TipoSesion).HasColumnName("tipo_sesion").HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(150).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(1000).IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            e.Property(x => x.FechaProgramada).HasColumnName("fecha_programada").IsRequired();
            e.Property(x => x.CodigoAcceso).HasColumnName("codigo_acceso").HasMaxLength(32).IsRequired();
            e.Property(x => x.OperadorCreadorId).HasColumnName("operador_creador_id").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").IsRequired();
            e.Property(x => x.FechaInicioUtc).HasColumnName("fecha_inicio_utc");
            e.Property(x => x.FechaFinalizacionUtc).HasColumnName("fecha_finalizacion_utc");
            e.Property(x => x.MaximoParticipantes).HasColumnName("maximo_participantes");
            e.Property(x => x.MaximoEquipos).HasColumnName("maximo_equipos");
            e.Property(x => x.MaximoParticipantesPorEquipo).HasColumnName("maximo_participantes_por_equipo");

            e.HasIndex(x => x.Estado);
            e.HasIndex(x => x.FechaProgramada);
            e.HasIndex(x => x.OperadorCreadorId);
            e.HasIndex(x => x.TipoSesion);
            e.HasIndex(x => x.CodigoAcceso).IsUnique();

            e.HasMany(x => x.Misiones)
                .WithOne()
                .HasForeignKey(m => m.SesionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Equipos)
                .WithOne()
                .HasForeignKey(eq => eq.SesionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Participantes)
                .WithOne()
                .HasForeignKey(p => p.SesionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        constructor.Entity<SesionMisionModelo>(e =>
        {
            e.ToTable("SesionMision");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.MisionId).HasColumnName("mision_id").IsRequired();
            e.Property(x => x.Orden).HasColumnName("orden").IsRequired();

            e.HasIndex(x => new { x.SesionId, x.MisionId }).IsUnique();
            e.HasIndex(x => x.MisionId);
        });

        constructor.Entity<EquipoModelo>(e =>
        {
            e.ToTable("Equipo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
            e.Property(x => x.LiderParticipanteId).HasColumnName("lider_participante_id").IsRequired();
            e.Property(x => x.Puntaje).HasColumnName("puntaje").IsRequired();
            e.Property(x => x.Tipo).HasColumnName("tipo_equipo").IsRequired();
            e.Property(x => x.ContrasenaHash).HasColumnName("contrasena_hash").HasMaxLength(256);
            e.Property(x => x.CapacidadMaxima).HasColumnName("capacidad_maxima").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").IsRequired();

            e.HasIndex(x => new { x.SesionId, x.Nombre }).IsUnique();
        });

        constructor.Entity<ParticipanteModelo>(e =>
        {
            e.ToTable("Participante");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.ParticipanteIdentidadId).HasColumnName("participante_identidad_id").IsRequired();
            e.Property(x => x.EquipoId).HasColumnName("equipo_id");
            e.Property(x => x.Puntaje).HasColumnName("puntaje").IsRequired();
            e.Property(x => x.FechaUnionSesion).HasColumnName("fecha_union_sesion").IsRequired();
            e.Property(x => x.FechaUnionEquipo).HasColumnName("fecha_union_equipo");

            e.HasIndex(x => new { x.SesionId, x.ParticipanteIdentidadId }).IsUnique();
            e.HasIndex(x => x.EquipoId);
        });
    }
}
