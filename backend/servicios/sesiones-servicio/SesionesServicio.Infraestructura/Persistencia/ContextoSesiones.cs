using Microsoft.EntityFrameworkCore;
using SesionesServicio.Infraestructura.ServiciosExternos;

namespace SesionesServicio.Infraestructura.Persistencia;

public sealed class ContextoSesiones : DbContext
{
    public ContextoSesiones(DbContextOptions<ContextoSesiones> opciones) : base(opciones) { }

    public DbSet<SesionModelo> Sesiones => Set<SesionModelo>();
    public DbSet<SesionMisionModelo> SesionMisiones => Set<SesionMisionModelo>();
    public DbSet<EquipoModelo> Equipos => Set<EquipoModelo>();
    public DbSet<ParticipanteModelo> Participantes => Set<ParticipanteModelo>();
    public DbSet<RespuestaTriviaModelo> RespuestasTrivia => Set<RespuestaTriviaModelo>();
    public DbSet<EvidenciaTesoroModelo> EvidenciasTesoro => Set<EvidenciaTesoroModelo>();
    public DbSet<PistaLiberadaModelo> PistasLiberadas => Set<PistaLiberadaModelo>();
    public DbSet<EtapaCompletadaModelo> EtapasCompletadas => Set<EtapaCompletadaModelo>();
    public DbSet<OutboxMensajeRankingModelo> OutboxRanking => Set<OutboxMensajeRankingModelo>();
    public DbSet<PenalizacionSesionModelo> Penalizaciones => Set<PenalizacionSesionModelo>();

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
            e.Property(x => x.DuracionSegundosLimite).HasColumnName("duracion_segundos_limite");
            e.Property(x => x.EjecucionActualMisionId).HasColumnName("ejecucion_actual_mision_id");
            e.Property(x => x.EjecucionActualEtapaId).HasColumnName("ejecucion_actual_etapa_id");
            e.Property(x => x.EjecucionActualModoDeJuegoId).HasColumnName("ejecucion_actual_modo_de_juego_id");
            e.Property(x => x.EjecucionActualTipoEtapa).HasColumnName("ejecucion_actual_tipo_etapa").HasMaxLength(40);
            e.Property(x => x.EjecucionActualOrdenGlobal).HasColumnName("ejecucion_actual_orden_global");
            e.Property(x => x.EjecucionActualOrdenMision).HasColumnName("ejecucion_actual_orden_mision");
            e.Property(x => x.EjecucionActualOrdenEtapa).HasColumnName("ejecucion_actual_orden_etapa");
            e.Property(x => x.EjecucionActualFase).HasColumnName("ejecucion_actual_fase");
            e.Property(x => x.EjecucionActualDuracionPreparacionSegundos)
                .HasColumnName("ejecucion_actual_duracion_preparacion_segundos");
            e.Property(x => x.EjecucionActualFechaInicioUtc).HasColumnName("ejecucion_actual_fecha_inicio_utc");
            e.Property(x => x.EjecucionActualDuracionSegundos).HasColumnName("ejecucion_actual_duracion_segundos");
            e.Property(x => x.EjecucionActualDuracionPausasAcumuladaMs).HasColumnName("ejecucion_actual_duracion_pausas_acumulada_ms");
            e.Property(x => x.EjecucionActualFechaInicioPausaUtc).HasColumnName("ejecucion_actual_fecha_inicio_pausa_utc");
            e.Property(x => x.SecuenciaEtapasJson).HasColumnName("secuencia_etapas_json");

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
            e.Property(x => x.PuntosPenalizados)
                .HasColumnName("puntos_penalizados").IsRequired().HasDefaultValue(0);
            e.Property(x => x.SnapshotRankingUtc).HasColumnName("snapshot_ranking_utc");
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
            e.Property(x => x.PuntosPenalizados)
                .HasColumnName("puntos_penalizados").IsRequired().HasDefaultValue(0);
            e.Property(x => x.SnapshotRankingUtc).HasColumnName("snapshot_ranking_utc");
            e.Property(x => x.FechaUnionSesion).HasColumnName("fecha_union_sesion").IsRequired();
            e.Property(x => x.FechaUnionEquipo).HasColumnName("fecha_union_equipo");

            e.HasIndex(x => new { x.SesionId, x.ParticipanteIdentidadId }).IsUnique();
            e.HasIndex(x => x.EquipoId);
        });

        constructor.Entity<RespuestaTriviaModelo>(e =>
        {
            e.ToTable("RespuestaTrivia");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.MisionId).HasColumnName("mision_id").IsRequired();
            e.Property(x => x.EtapaId).HasColumnName("etapa_id").IsRequired();
            e.Property(x => x.TriviaId).HasColumnName("trivia_id").IsRequired();
            e.Property(x => x.PreguntaId).HasColumnName("pregunta_id").IsRequired();
            e.Property(x => x.OpcionSeleccionadaId).HasColumnName("opcion_seleccionada_id");
            e.Property(x => x.ParticipanteIdentidadId).HasColumnName("participante_identidad_id").IsRequired();
            e.Property(x => x.EquipoId).HasColumnName("equipo_id");
            e.Property(x => x.EsCorrecta).HasColumnName("es_correcta").IsRequired();
            e.Property(x => x.PuntosGanados).HasColumnName("puntos_ganados").IsRequired();
            e.Property(x => x.EventoPuntuacionId).HasColumnName("evento_puntuacion_id").IsRequired();
            e.HasIndex(x => x.EventoPuntuacionId);
            e.Property(x => x.TiempoTardadoMs).HasColumnName("tiempo_tardado_ms").IsRequired();
            e.Property(x => x.FechaRespuestaUtc).HasColumnName("fecha_respuesta_utc").IsRequired();
            e.HasIndex(x => new { x.SesionId, x.EtapaId, x.PreguntaId, x.ParticipanteIdentidadId })
                .IsUnique()
                .HasFilter("equipo_id IS NULL");
            e.HasIndex(x => new { x.SesionId, x.EtapaId, x.PreguntaId, x.EquipoId })
                .IsUnique()
                .HasFilter("equipo_id IS NOT NULL");
            e.HasIndex(x => new { x.SesionId, x.EtapaId, x.ParticipanteIdentidadId });
            e.HasIndex(x => new { x.SesionId, x.EtapaId });
        });

        constructor.Entity<EvidenciaTesoroModelo>(e =>
        {
            e.ToTable("EvidenciaTesoro");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.MisionId).HasColumnName("mision_id").IsRequired();
            e.Property(x => x.EtapaId).HasColumnName("etapa_id").IsRequired();
            e.Property(x => x.BusquedaId).HasColumnName("busqueda_id").IsRequired();
            e.Property(x => x.ParticipanteIdentidadId).HasColumnName("participante_identidad_id").IsRequired();
            e.Property(x => x.EquipoId).HasColumnName("equipo_id");
            e.Property(x => x.CodigoEnviado).HasColumnName("codigo_enviado").HasMaxLength(64).IsRequired();
            e.Property(x => x.EsValida).HasColumnName("es_valida").IsRequired();
            e.Property(x => x.PuntosGanados).HasColumnName("puntos_ganados").IsRequired();
            e.Property(x => x.EventoPuntuacionId).HasColumnName("evento_puntuacion_id").IsRequired();
            e.HasIndex(x => x.EventoPuntuacionId);
            e.Property(x => x.FechaEnvioUtc).HasColumnName("fecha_envio_utc").IsRequired();

            // Unicidad de la evidencia VÁLIDA por jugador mediante índices únicos
            // filtrados (PostgreSQL). Solo aplica a es_valida = true, de modo que
            // un QR incorrecto permite nuevos intentos:
            //  - Individual (equipo_id IS NULL): una evidencia válida por participante.
            //  - Grupal (equipo_id IS NOT NULL): una evidencia válida por equipo;
            //    el primer integrante que la persiste gana la carrera.
            e.HasIndex(x => new { x.SesionId, x.EtapaId, x.ParticipanteIdentidadId })
                .IsUnique()
                .HasFilter("equipo_id IS NULL AND es_valida");
            e.HasIndex(x => new { x.SesionId, x.EtapaId, x.EquipoId })
                .IsUnique()
                .HasFilter("equipo_id IS NOT NULL AND es_valida");
            e.HasIndex(x => new { x.SesionId, x.EtapaId });
        });

        constructor.Entity<PistaLiberadaModelo>(e =>
        {
            e.ToTable("PistaLiberada");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.EtapaId).HasColumnName("etapa_id").IsRequired();
            e.Property(x => x.PistaId).HasColumnName("pista_id");
            e.Property(x => x.Contenido).HasColumnName("contenido").HasMaxLength(1000).IsRequired();
            e.Property(x => x.Tipo).HasColumnName("tipo").IsRequired().HasDefaultValue(0);
            e.Property(x => x.Latitud).HasColumnName("latitud");
            e.Property(x => x.Longitud).HasColumnName("longitud");
            e.Property(x => x.FechaLiberacionUtc).HasColumnName("fecha_liberacion_utc").IsRequired();
            // No se puede liberar la misma pista predefinida dos veces en la misma sesión/etapa
            e.HasIndex(x => new { x.SesionId, x.EtapaId, x.PistaId })
                .IsUnique()
                .HasFilter("pista_id IS NOT NULL");
            e.HasIndex(x => new { x.SesionId, x.EtapaId });
        });

        constructor.Entity<EtapaCompletadaModelo>(e =>
        {
            e.ToTable("EtapaCompletada");
            e.HasKey(x => new { x.SesionId, x.EtapaId });
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.EtapaId).HasColumnName("etapa_id").IsRequired();
            e.Property(x => x.FechaCompletadaUtc).HasColumnName("fecha_completada_utc").IsRequired();
            e.HasIndex(x => x.SesionId);
        });

        constructor.Entity<OutboxMensajeRankingModelo>(e =>
        {
            e.ToTable("OutboxRanking");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
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

        constructor.Entity<PenalizacionSesionModelo>(e =>
        {
            e.ToTable("PenalizacionSesion", tabla =>
            {
                // Puntos siempre 1..100 (la cantidad recibida es positiva).
                tabla.HasCheckConstraint(
                    "ck_penalizacion_puntos_rango",
                    "puntos BETWEEN 1 AND 100");
                // Motivo obligatorio (no vacío tras Trim en el dominio).
                tabla.HasCheckConstraint(
                    "ck_penalizacion_motivo_no_vacio",
                    "length(btrim(motivo)) > 0");
                // Exactamente un tipo de objetivo, coherente con tipo_objetivo:
                //  - Participante (0): participante_* NOT NULL, equipo_id NULL.
                //  - Equipo (1): equipo_id NOT NULL, participante_* NULL.
                tabla.HasCheckConstraint(
                    "ck_penalizacion_objetivo_coherente",
                    "(tipo_objetivo = 0 AND participante_sesion_id IS NOT NULL " +
                    "AND participante_identidad_id IS NOT NULL AND equipo_id IS NULL) " +
                    "OR (tipo_objetivo = 1 AND equipo_id IS NOT NULL " +
                    "AND participante_sesion_id IS NULL AND participante_identidad_id IS NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EventoId).HasColumnName("evento_id").IsRequired();
            e.Property(x => x.SesionId).HasColumnName("sesion_id").IsRequired();
            e.Property(x => x.TipoObjetivo).HasColumnName("tipo_objetivo").IsRequired();
            e.Property(x => x.ParticipanteSesionId).HasColumnName("participante_sesion_id");
            e.Property(x => x.ParticipanteIdentidadId).HasColumnName("participante_identidad_id");
            e.Property(x => x.EquipoId).HasColumnName("equipo_id");
            e.Property(x => x.Puntos).HasColumnName("puntos").IsRequired();
            e.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(500).IsRequired();
            e.Property(x => x.OperadorIdentidadId).HasColumnName("operador_identidad_id").IsRequired();
            e.Property(x => x.AplicadaEnUtc).HasColumnName("aplicada_en_utc").IsRequired();
            e.Property(x => x.ProcesadaEnUtc).HasColumnName("procesada_en_utc");
            e.Property(x => x.PuntajeResultante).HasColumnName("puntaje_resultante");
            e.Property(x => x.EstadoProcesamiento).HasColumnName("estado_procesamiento").IsRequired();

            e.HasIndex(x => x.EventoId).IsUnique();
            e.HasIndex(x => x.SesionId);
            e.HasIndex(x => new { x.SesionId, x.EquipoId });
            e.HasIndex(x => new { x.SesionId, x.ParticipanteSesionId });
        });
    }
}
