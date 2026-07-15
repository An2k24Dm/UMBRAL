using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.ObjetosValor;

public sealed class EjecucionActualSesion : IEquatable<EjecucionActualSesion>
{
    public Guid MisionId { get; }
    public Guid EtapaId { get; }
    public Guid ModoDeJuegoId { get; }
    public string TipoEtapa { get; }
    public int OrdenGlobal { get; }
    public int OrdenMision { get; }
    public int OrdenEtapa { get; }
    public FaseEjecucionEtapaSesion Fase { get; }
    public DateTime? FechaInicioUtc { get; }
    public int DuracionSegundos { get; }
    public int DuracionPreparacionSegundos { get; }
    public long DuracionPausasAcumuladaMs { get; }
    public DateTime? FechaInicioPausaUtc { get; }
    public bool EstaPlanificada => Fase == FaseEjecucionEtapaSesion.Planificada;
    public bool EstaEnPreparacion => Fase == FaseEjecucionEtapaSesion.Preparacion;
    public bool EstaActiva => Fase == FaseEjecucionEtapaSesion.Activa;
    public bool EstaEnCierrePendiente => Fase == FaseEjecucionEtapaSesion.CierrePendiente;
    public bool EsNuevaMision => OrdenEtapa <= 1;

    private EjecucionActualSesion(
        Guid misionId,
        Guid etapaId,
        Guid modoDeJuegoId,
        string tipoEtapa,
        int ordenGlobal,
        int ordenMision,
        int ordenEtapa,
        FaseEjecucionEtapaSesion fase,
        DateTime? fechaInicioUtc,
        int duracionSegundos,
        int duracionPreparacionSegundos,
        long duracionPausasAcumuladaMs,
        DateTime? fechaInicioPausaUtc)
    {
        MisionId = misionId;
        EtapaId = etapaId;
        ModoDeJuegoId = modoDeJuegoId;
        TipoEtapa = tipoEtapa;
        OrdenGlobal = ordenGlobal;
        OrdenMision = ordenMision;
        OrdenEtapa = ordenEtapa;
        Fase = fase;
        FechaInicioUtc = fechaInicioUtc;
        DuracionSegundos = duracionSegundos;
        DuracionPreparacionSegundos = duracionPreparacionSegundos;
        DuracionPausasAcumuladaMs = duracionPausasAcumuladaMs;
        FechaInicioPausaUtc = fechaInicioPausaUtc;

        GarantizarInvariantesDeFase();
    }

    public static EjecucionActualSesion Planificar(
        Guid misionId,
        Guid etapaId,
        Guid modoDeJuegoId,
        string tipoEtapa,
        int ordenGlobal,
        int ordenMision,
        int ordenEtapa,
        int duracionSegundos)
    {
        Validar(misionId, etapaId, modoDeJuegoId, tipoEtapa, ordenGlobal, duracionSegundos);
        if (ordenMision <= 0)
            throw new SesionInvalidaExcepcion("El orden de misión debe ser mayor a cero.");
        if (ordenEtapa <= 0)
            throw new SesionInvalidaExcepcion("El orden de etapa debe ser mayor a cero.");

        return new EjecucionActualSesion(
            misionId,
            etapaId,
            modoDeJuegoId,
            tipoEtapa.Trim(),
            ordenGlobal,
            ordenMision,
            ordenEtapa,
            FaseEjecucionEtapaSesion.Planificada,
            null,
            duracionSegundos,
            0,
            0,
            null);
    }

    public EjecucionActualSesion Iniciar(DateTime ahoraUtc)
    {
        if (!EstaPlanificada)
            throw new SesionInvalidaExcepcion(
                "Solo una etapa planificada puede iniciarse directamente.");

        return new EjecucionActualSesion(
            MisionId,
            EtapaId,
            ModoDeJuegoId,
            TipoEtapa,
            OrdenGlobal,
            OrdenMision,
            OrdenEtapa,
            FaseEjecucionEtapaSesion.Activa,
            NormalizarUtc(ahoraUtc),
            DuracionSegundos,
            0,
            0,
            null);
    }

    public EjecucionActualSesion Programar(
        DateTime fechaInicioPreparacionUtc, int duracionPreparacionSegundos)
    {
        if (!EstaPlanificada)
            throw new SesionInvalidaExcepcion(
                "Solo una etapa planificada puede programarse en preparación.");
        if (duracionPreparacionSegundos <= 0)
            throw new SesionInvalidaExcepcion(
                "La duracion de la preparacion debe ser mayor a cero segundos.");

        return new EjecucionActualSesion(
            MisionId,
            EtapaId,
            ModoDeJuegoId,
            TipoEtapa,
            OrdenGlobal,
            OrdenMision,
            OrdenEtapa,
            FaseEjecucionEtapaSesion.Preparacion,
            NormalizarUtc(fechaInicioPreparacionUtc),
            DuracionSegundos,
            duracionPreparacionSegundos,
            0,
            null);
    }

    public static EjecucionActualSesion Crear(
        Guid misionId,
        Guid etapaId,
        Guid modoDeJuegoId,
        string tipoEtapa,
        int ordenGlobal,
        DateTime fechaInicioUtc,
        int duracionSegundos,
        int ordenMision = 1,
        int ordenEtapa = 1)
    {
        Validar(misionId, etapaId, modoDeJuegoId, tipoEtapa, ordenGlobal, duracionSegundos);

        return new EjecucionActualSesion(
            misionId,
            etapaId,
            modoDeJuegoId,
            tipoEtapa.Trim(),
            ordenGlobal,
            Math.Max(1, ordenMision),
            Math.Max(1, ordenEtapa),
            FaseEjecucionEtapaSesion.Activa,
            NormalizarUtc(fechaInicioUtc),
            duracionSegundos,
            0,
            0,
            null);
    }

    public EjecucionActualSesion ProgramarCierrePendiente(
        DateTime ahoraUtc, int duracionFeedbackSegundos)
    {
        if (EstaEnCierrePendiente)
            return this;
        if (!EstaActiva)
            throw new SesionInvalidaExcepcion(
                "Solo una etapa activa puede entrar en cierre pendiente.");
        if (duracionFeedbackSegundos <= 0)
            throw new SesionInvalidaExcepcion(
                "La duracion del feedback final debe ser mayor a cero segundos.");

        return new EjecucionActualSesion(
            MisionId,
            EtapaId,
            ModoDeJuegoId,
            TipoEtapa,
            OrdenGlobal,
            OrdenMision,
            OrdenEtapa,
            FaseEjecucionEtapaSesion.CierrePendiente,
            NormalizarUtc(ahoraUtc),
            DuracionSegundos,
            duracionFeedbackSegundos,
            0,
            null);
    }

    public EjecucionActualSesion Activar(DateTime ahoraUtc)
    {
        if (EstaActiva)
            return this;
        if (!EstaEnPreparacion)
            throw new SesionInvalidaExcepcion(
                "Solo una etapa en preparación puede activarse.");

        return new EjecucionActualSesion(
            MisionId,
            EtapaId,
            ModoDeJuegoId,
            TipoEtapa,
            OrdenGlobal,
            OrdenMision,
            OrdenEtapa,
            FaseEjecucionEtapaSesion.Activa,
            NormalizarUtc(ahoraUtc),
            DuracionSegundos,
            0,
            0,
            null);
    }

    public static EjecucionActualSesion Rehidratar(
        Guid misionId,
        Guid etapaId,
        Guid modoDeJuegoId,
        string tipoEtapa,
        int ordenGlobal,
        DateTime? fechaInicioUtc,
        int duracionSegundos,
        long duracionPausasAcumuladaMs,
        DateTime? fechaInicioPausaUtc,
        FaseEjecucionEtapaSesion fase = FaseEjecucionEtapaSesion.Activa,
        int ordenMision = 1,
        int ordenEtapa = 1,
        int duracionPreparacionSegundos = 0)
    {
        Validar(misionId, etapaId, modoDeJuegoId, tipoEtapa, ordenGlobal, duracionSegundos);
        if (duracionPausasAcumuladaMs < 0)
            throw new SesionInvalidaExcepcion("La duracion acumulada de pausas no puede ser negativa.");

        return new EjecucionActualSesion(
            misionId,
            etapaId,
            modoDeJuegoId,
            tipoEtapa.Trim(),
            ordenGlobal,
            Math.Max(1, ordenMision),
            Math.Max(1, ordenEtapa),
            fase,
            fechaInicioUtc.HasValue ? NormalizarUtc(fechaInicioUtc.Value) : null,
            duracionSegundos,
            Math.Max(0, duracionPreparacionSegundos),
            duracionPausasAcumuladaMs,
            fechaInicioPausaUtc.HasValue ? NormalizarUtc(fechaInicioPausaUtc.Value) : null);
    }

    public EjecucionActualSesion Pausar(DateTime ahoraUtc)
    {
        if (FechaInicioPausaUtc.HasValue || EstaPlanificada)
            return this;

        return new EjecucionActualSesion(
            MisionId,
            EtapaId,
            ModoDeJuegoId,
            TipoEtapa,
            OrdenGlobal,
            OrdenMision,
            OrdenEtapa,
            Fase,
            FechaInicioUtc,
            DuracionSegundos,
            DuracionPreparacionSegundos,
            DuracionPausasAcumuladaMs,
            NormalizarUtc(ahoraUtc));
    }

    public EjecucionActualSesion Reanudar(DateTime ahoraUtc)
    {
        if (!FechaInicioPausaUtc.HasValue)
            return this;

        var ahora = NormalizarUtc(ahoraUtc);
        var pausaMs = Math.Max(0, (long)(ahora - FechaInicioPausaUtc.Value).TotalMilliseconds);

        return new EjecucionActualSesion(
            MisionId,
            EtapaId,
            ModoDeJuegoId,
            TipoEtapa,
            OrdenGlobal,
            OrdenMision,
            OrdenEtapa,
            Fase,
            FechaInicioUtc,
            DuracionSegundos,
            DuracionPreparacionSegundos,
            DuracionPausasAcumuladaMs + pausaMs,
            null);
    }

    public int CalcularSegundosRestantes(DateTime ahoraUtc)
    {
        var consumidosMs = CalcularTiempoActivoTranscurridoMs(ahoraUtc);
        return Math.Max(0, DuracionSegundos - (int)(consumidosMs / 1000));
    }

    public long CalcularTiempoActivoTranscurridoMs(DateTime ahoraUtc)
    {
        // Una etapa planificada aún no ha comenzado: no hay tiempo transcurrido.
        if (!FechaInicioUtc.HasValue) return 0;

        var ahora = NormalizarUtc(ahoraUtc);
        var pausaActualMs = FechaInicioPausaUtc.HasValue
            ? Math.Max(0, (long)(ahora - FechaInicioPausaUtc.Value).TotalMilliseconds)
            : 0;
        return Math.Max(
            0,
            (long)(ahora - FechaInicioUtc.Value).TotalMilliseconds
            - DuracionPausasAcumuladaMs
            - pausaActualMs);
    }

    private long TiempoPreparacionTranscurridoMs(DateTime ahoraUtc)
        => EstaEnPreparacion ? CalcularTiempoActivoTranscurridoMs(ahoraUtc) : 0;

    public int CalcularSegundosRestantesPreparacion(DateTime ahoraUtc)
    {
        if (!EstaEnPreparacion) return 0;
        var transcurridos = (int)(TiempoPreparacionTranscurridoMs(ahoraUtc) / 1000);
        return Math.Max(0, DuracionPreparacionSegundos - transcurridos);
    }

    public bool PreparacionVencida(DateTime ahoraUtc)
        => EstaEnPreparacion
           && TiempoPreparacionTranscurridoMs(ahoraUtc) >= (long)DuracionPreparacionSegundos * 1000;

    public int CalcularSegundosRestantesCierrePendiente(DateTime ahoraUtc)
    {
        if (!EstaEnCierrePendiente) return 0;
        var transcurridos = (int)(CalcularTiempoActivoTranscurridoMs(ahoraUtc) / 1000);
        return Math.Max(0, DuracionPreparacionSegundos - transcurridos);
    }

    public bool CierrePendienteVencido(DateTime ahoraUtc)
        => EstaEnCierrePendiente
           && CalcularTiempoActivoTranscurridoMs(ahoraUtc) >= (long)DuracionPreparacionSegundos * 1000;

    public DateTime FechaInicioProgramadaUtc
    {
        get
        {
            if (!FechaInicioUtc.HasValue)
                throw new SesionInvalidaExcepcion(
                    "Una etapa planificada no tiene fecha de inicio programada.");
            return EstaEnPreparacion
                ? FechaInicioUtc.Value.AddSeconds(DuracionPreparacionSegundos)
                : FechaInicioUtc.Value;
        }
    }

    public bool Equals(EjecucionActualSesion? other)
    {
        if (other is null) return false;

        return MisionId == other.MisionId
               && EtapaId == other.EtapaId
               && ModoDeJuegoId == other.ModoDeJuegoId
               && TipoEtapa == other.TipoEtapa
               && OrdenGlobal == other.OrdenGlobal
               && OrdenMision == other.OrdenMision
               && OrdenEtapa == other.OrdenEtapa
               && Fase == other.Fase
               && FechaInicioUtc == other.FechaInicioUtc
               && DuracionSegundos == other.DuracionSegundos
               && DuracionPreparacionSegundos == other.DuracionPreparacionSegundos
               && DuracionPausasAcumuladaMs == other.DuracionPausasAcumuladaMs
               && FechaInicioPausaUtc == other.FechaInicioPausaUtc;
    }

    public override bool Equals(object? obj) => obj is EjecucionActualSesion other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(MisionId);
        hash.Add(EtapaId);
        hash.Add(ModoDeJuegoId);
        hash.Add(TipoEtapa);
        hash.Add(OrdenGlobal);
        hash.Add(OrdenMision);
        hash.Add(OrdenEtapa);
        hash.Add(Fase);
        hash.Add(FechaInicioUtc);
        hash.Add(DuracionSegundos);
        hash.Add(DuracionPreparacionSegundos);
        hash.Add(DuracionPausasAcumuladaMs);
        hash.Add(FechaInicioPausaUtc);
        return hash.ToHashCode();
    }

    // Invariantes por fase (evitan combinaciones imposibles en cualquier ruta de
    // construcción, incluida la rehidratación).
    private void GarantizarInvariantesDeFase()
    {
        if (EstaPlanificada)
        {
            if (FechaInicioUtc.HasValue)
                throw new SesionInvalidaExcepcion(
                    "Una etapa planificada no puede tener fecha de inicio.");
            if (DuracionPreparacionSegundos != 0
                || DuracionPausasAcumuladaMs != 0
                || FechaInicioPausaUtc.HasValue)
                throw new SesionInvalidaExcepcion(
                    "Una etapa planificada no tiene preparación ni pausas consumidas.");
            return;
        }

        if (!FechaInicioUtc.HasValue)
            throw new SesionInvalidaExcepcion(
                "Una etapa en preparación, activa o en cierre pendiente requiere fecha de inicio.");

        if ((EstaEnPreparacion || EstaEnCierrePendiente) && DuracionPreparacionSegundos <= 0)
            throw new SesionInvalidaExcepcion(
                "La preparación o el cierre pendiente requieren una duración mayor a cero.");
    }

    private static void Validar(
        Guid misionId,
        Guid etapaId,
        Guid modoDeJuegoId,
        string tipoEtapa,
        int ordenGlobal,
        int duracionSegundos)
    {
        if (misionId == Guid.Empty)
            throw new SesionInvalidaExcepcion("La mision actual es obligatoria.");
        if (etapaId == Guid.Empty)
            throw new SesionInvalidaExcepcion("La etapa actual es obligatoria.");
        if (modoDeJuegoId == Guid.Empty)
            throw new SesionInvalidaExcepcion("El modo de juego actual es obligatorio.");
        if (string.IsNullOrWhiteSpace(tipoEtapa))
            throw new SesionInvalidaExcepcion("El tipo de etapa actual es obligatorio.");
        if (ordenGlobal <= 0)
            throw new SesionInvalidaExcepcion("El orden global actual debe ser mayor a cero.");
        if (duracionSegundos <= 0)
            throw new SesionInvalidaExcepcion("La duracion de la etapa actual debe ser mayor a cero segundos.");
    }

    private static DateTime NormalizarUtc(DateTime fecha)
        => fecha.Kind == DateTimeKind.Utc
            ? fecha
            : DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
}
