using Microsoft.EntityFrameworkCore;
using RankingServicio.Infraestructura.Persistencia.Modelos;

namespace RankingServicio.Infraestructura.Persistencia;

public sealed class ContextoRanking : DbContext
{
    public ContextoRanking(DbContextOptions<ContextoRanking> opciones) : base(opciones) { }

    public DbSet<EntradaRankingParticipanteModelo> EntradasParticipante => Set<EntradaRankingParticipanteModelo>();
    public DbSet<EntradaRankingEquipoModelo> EntradasEquipo => Set<EntradaRankingEquipoModelo>();
    public DbSet<RankingGlobalParticipanteModelo> RankingGlobal => Set<RankingGlobalParticipanteModelo>();
    public DbSet<EventoProcesadoModelo> EventosProcesados => Set<EventoProcesadoModelo>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.HasDefaultSchema("ranking");

        modelo.Entity<EntradaRankingParticipanteModelo>(e =>
        {
            e.ToTable("entradas_ranking_participante");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.ParticipanteIdentidadId).HasColumnName("participante_identidad_id").IsRequired();
            e.Property(x => x.NombreParticipante).HasColumnName("nombre_participante").HasMaxLength(200).IsRequired();
            e.Property(x => x.PuntajeTotal).HasColumnName("puntaje_total");
            e.Property(x => x.RespuestasCorrectas).HasColumnName("respuestas_correctas");
            e.Property(x => x.RespuestasTotales).HasColumnName("respuestas_totales");
            e.Property(x => x.EtapasCompletadas).HasColumnName("etapas_completadas");
            e.Property(x => x.Posicion).HasColumnName("posicion");
            e.Property(x => x.UltimaActualizacionUtc).HasColumnName("ultima_actualizacion_utc");
            e.HasIndex(x => new { x.SesionId, x.ParticipanteIdentidadId }).IsUnique();
            e.HasIndex(x => new { x.SesionId, x.Posicion });
        });

        modelo.Entity<EntradaRankingEquipoModelo>(e =>
        {
            e.ToTable("entradas_ranking_equipo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.EquipoId).HasColumnName("equipo_id").IsRequired();
            e.Property(x => x.NombreEquipo).HasColumnName("nombre_equipo").HasMaxLength(200).IsRequired();
            e.Property(x => x.PuntajeTotal).HasColumnName("puntaje_total");
            e.Property(x => x.EtapasCompletadas).HasColumnName("etapas_completadas");
            e.Property(x => x.Posicion).HasColumnName("posicion");
            e.Property(x => x.UltimaActualizacionUtc).HasColumnName("ultima_actualizacion_utc");
            e.HasIndex(x => new { x.SesionId, x.EquipoId }).IsUnique();
            e.HasIndex(x => new { x.SesionId, x.Posicion });
        });

        modelo.Entity<RankingGlobalParticipanteModelo>(e =>
        {
            e.ToTable("ranking_global_participante");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ParticipanteIdentidadId).HasColumnName("participante_identidad_id").IsRequired();
            e.Property(x => x.NombreParticipante).HasColumnName("nombre_participante").HasMaxLength(200).IsRequired();
            e.Property(x => x.PuntajeAcumulado).HasColumnName("puntaje_acumulado");
            e.Property(x => x.SesionesJugadas).HasColumnName("sesiones_jugadas");
            e.Property(x => x.EtapasCompletadasTotal).HasColumnName("etapas_completadas_total");
            e.Property(x => x.UltimaActualizacionUtc).HasColumnName("ultima_actualizacion_utc");
            e.HasIndex(x => x.ParticipanteIdentidadId).IsUnique();
            e.HasIndex(x => x.PuntajeAcumulado);
        });

        modelo.Entity<EventoProcesadoModelo>(e =>
        {
            e.ToTable("eventos_procesados");
            e.HasKey(x => new { x.Id, x.TipoEvento });
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TipoEvento).HasColumnName("tipo_evento").HasMaxLength(100);
            e.Property(x => x.ProcesadoEnUtc).HasColumnName("procesado_en_utc");
        });
    }
}
