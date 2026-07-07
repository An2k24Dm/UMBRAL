using Microsoft.EntityFrameworkCore;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Infraestructura.Persistencia.Modelos;

namespace PartidasServicio.Infraestructura.Persistencia;

public sealed class ContextoPartidas : DbContext, IUnidadTrabajoPartidas
{
    public ContextoPartidas(DbContextOptions<ContextoPartidas> opciones) : base(opciones) { }

    public Task GuardarCambiosAsync(CancellationToken cancelacion) =>
        SaveChangesAsync(cancelacion);

    public DbSet<RespuestaTriviaModelo> RespuestasTrivia => Set<RespuestaTriviaModelo>();
    public DbSet<PartidaModelo> Partidas => Set<PartidaModelo>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.HasDefaultSchema("partidas");

        constructor.Entity<PartidaModelo>(e =>
        {
            e.ToTable("Partida");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsRequired();
            e.Property(x => x.FechaCreacionUtc).HasColumnName("fecha_creacion_utc").IsRequired();
            e.Property(x => x.FechaInicioUtc).HasColumnName("fecha_inicio_utc");
            e.Property(x => x.FechaFinUtc).HasColumnName("fecha_fin_utc");

            e.HasIndex(x => x.SesionId).IsUnique();
        });

        constructor.Entity<RespuestaTriviaModelo>(e =>
        {
            e.ToTable("RespuestaTrivia");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.MisionId).HasColumnName("mision_id").IsRequired();
            e.Property(x => x.EtapaId).HasColumnName("etapa_id").IsRequired();
            e.Property(x => x.PreguntaId).HasColumnName("pregunta_id").IsRequired();
            e.Property(x => x.OpcionSeleccionadaId).HasColumnName("opcion_seleccionada_id").IsRequired();
            e.Property(x => x.ParticipanteId).HasColumnName("participante_id").IsRequired();
            e.Property(x => x.EquipoId).HasColumnName("equipo_id");
            e.Property(x => x.EsCorrecta).HasColumnName("es_correcta").IsRequired();
            e.Property(x => x.PuntosGanados).HasColumnName("puntos_ganados").IsRequired();
            e.Property(x => x.TiempoTardadoMs).HasColumnName("tiempo_tardado_ms").IsRequired();
            e.Property(x => x.FechaRespuestaUtc).HasColumnName("fecha_respuesta_utc").IsRequired();

            e.HasIndex(x => x.SesionId);
            e.HasIndex(x => x.EquipoId);
            e.HasIndex(x => x.ParticipanteId);

            // Garantiza que un equipo solo responde una vez por pregunta (modo grupal)
            e.HasIndex(x => new { x.SesionId, x.PreguntaId, x.EquipoId })
                .IsUnique()
                .HasFilter("equipo_id IS NOT NULL");

            // Garantiza que un participante solo responde una vez por pregunta (modo individual)
            e.HasIndex(x => new { x.SesionId, x.PreguntaId, x.ParticipanteId })
                .IsUnique()
                .HasFilter("equipo_id IS NULL");
        });
    }
}
