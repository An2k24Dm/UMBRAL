using Microsoft.EntityFrameworkCore;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Dominio.ObjetosValor;
using RankingServicio.Infraestructura.Persistencia.Modelos;
using RankingServicio.Infraestructura.RabbitMq;

namespace RankingServicio.Infraestructura.Persistencia;

public sealed class ContextoRanking : DbContext
{
    public ContextoRanking(DbContextOptions<ContextoRanking> opciones) : base(opciones) { }

    // Aggregate Root Ranking (por sesión). Los hijos se acceden a través del
    // agregado; no se exponen como DbSet independientes.
    public DbSet<Ranking> Rankings => Set<Ranking>();

    // Detalle técnico de idempotencia de integración (no es dominio de ranking).
    public DbSet<EventoProcesadoModelo> EventosProcesados => Set<EventoProcesadoModelo>();
    public DbSet<OutboxMensajeRankingModelo> OutboxRanking => Set<OutboxMensajeRankingModelo>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema("ranking");

        modelo.Entity<Ranking>(e =>
        {
            e.ToTable("rankings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.HasIndex(x => x.SesionId).IsUnique();

            e.HasMany(x => x.Participantes)
                .WithOne()
                .HasForeignKey("RankingId")
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Equipos)
                .WithOne()
                .HasForeignKey("RankingId")
                .OnDelete(DeleteBehavior.Cascade);

            // Las colecciones son de solo lectura hacia afuera; EF trabaja sobre
            // los campos de respaldo del agregado.
            e.Navigation(x => x.Participantes).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Equipos).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelo.Entity<RankingParticipante>(e =>
        {
            e.ToTable("ranking_participantes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property<Guid>("RankingId").HasColumnName("ranking_id");
            e.Property(x => x.ParticipanteSesionId)
                .HasColumnName("participante_sesion_id").IsRequired();
            e.Property(x => x.ParticipanteIdentidadId)
                .HasColumnName("participante_identidad_id").IsRequired();
            e.Property(x => x.EquipoId).HasColumnName("equipo_id");
            e.Property(x => x.Puntaje)
                .HasColumnName("puntaje")
                .HasConversion(p => p.Valor, v => Puntaje.DesdePersistencia(v));
            e.Property(x => x.PuntosPenalizados)
                .HasColumnName("puntos_penalizados")
                .IsRequired()
                .HasDefaultValue(0);
            e.HasIndex("RankingId", nameof(RankingParticipante.ParticipanteSesionId)).IsUnique();
        });

        modelo.Entity<RankingEquipo>(e =>
        {
            e.ToTable("ranking_equipos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property<Guid>("RankingId").HasColumnName("ranking_id");
            e.Property(x => x.EquipoId).HasColumnName("equipo_id").IsRequired();
            e.Property(x => x.Puntaje)
                .HasColumnName("puntaje")
                .HasConversion(p => p.Valor, v => Puntaje.DesdePersistencia(v));
            e.Property(x => x.PuntosPenalizados)
                .HasColumnName("puntos_penalizados")
                .IsRequired()
                .HasDefaultValue(0);
            e.HasIndex("RankingId", nameof(RankingEquipo.EquipoId)).IsUnique();
        });

        modelo.Entity<EventoProcesadoModelo>(e =>
        {
            e.ToTable("eventos_procesados");
            e.HasKey(x => new { x.Id, x.TipoEvento });
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.TipoEvento).HasColumnName("tipo_evento").HasMaxLength(100);
            e.Property(x => x.ProcesadoEnUtc).HasColumnName("procesado_en_utc");
        });

        modelo.Entity<OutboxMensajeRankingModelo>(e =>
        {
            e.ToTable("outbox_ranking");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(120).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnName("payload_json").IsRequired();
            e.Property(x => x.CreadoEnUtc).HasColumnName("creado_en_utc").IsRequired();
            e.Property(x => x.EnviadoEnUtc).HasColumnName("enviado_en_utc");
            e.Property(x => x.Intentos).HasColumnName("intentos").IsRequired();
            e.Property(x => x.ProximoIntentoUtc).HasColumnName("proximo_intento_utc");
            e.Property(x => x.UltimoError).HasColumnName("ultimo_error").HasMaxLength(1000);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(40).IsRequired();
            e.HasIndex(x => new { x.Estado, x.ProximoIntentoUtc, x.CreadoEnUtc });
        });
    }
}
