using System.Text.Json;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

public sealed class MapeadorSesionesPersistencia
{
    private static readonly JsonSerializerOptions OpcionesJson = new();

    private readonly IEnumerable<IMapeadorPersistenciaSesion> _mapeadores;

    public MapeadorSesionesPersistencia(IEnumerable<IMapeadorPersistenciaSesion> mapeadores)
    {
        _mapeadores = mapeadores;
    }

    public SesionModelo HaciaModelo(Sesion sesion)
    {
        var modelo = new SesionModelo
        {
            Id = sesion.Id,
            TipoSesion = sesion.TipoSesion,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Estado = sesion.Estado,
            FechaProgramada = sesion.FechaProgramada,
            CodigoAcceso = sesion.CodigoAcceso,
            OperadorCreadorId = sesion.OperadorCreadorId,
            FechaCreacion = sesion.FechaCreacion,
            FechaInicioUtc = sesion.FechaInicioUtc,
            FechaFinalizacionUtc = sesion.FechaFinalizacionUtc,
            DuracionSegundosLimite = sesion.DuracionSegundosLimite,
            EjecucionActualMisionId = sesion.EjecucionActual?.MisionId,
            EjecucionActualEtapaId = sesion.EjecucionActual?.EtapaId,
            EjecucionActualModoDeJuegoId = sesion.EjecucionActual?.ModoDeJuegoId,
            EjecucionActualTipoEtapa = sesion.EjecucionActual?.TipoEtapa,
            EjecucionActualOrdenGlobal = sesion.EjecucionActual?.OrdenGlobal,
            EjecucionActualOrdenMision = sesion.EjecucionActual?.OrdenMision,
            EjecucionActualOrdenEtapa = sesion.EjecucionActual?.OrdenEtapa,
            EjecucionActualFase = sesion.EjecucionActual is null
                ? null
                : (int)sesion.EjecucionActual.Fase,
            EjecucionActualDuracionPreparacionSegundos = sesion.EjecucionActual?.DuracionPreparacionSegundos,
            EjecucionActualFechaInicioUtc = sesion.EjecucionActual?.FechaInicioUtc,
            EjecucionActualDuracionSegundos = sesion.EjecucionActual?.DuracionSegundos,
            EjecucionActualDuracionPausasAcumuladaMs = sesion.EjecucionActual?.DuracionPausasAcumuladaMs,
            EjecucionActualFechaInicioPausaUtc = sesion.EjecucionActual?.FechaInicioPausaUtc,
            SecuenciaEtapasJson = sesion.SecuenciaEtapas.Count == 0
                ? null
                : SerializarSecuencia(sesion.SecuenciaEtapas),
            Misiones = sesion.Misiones.Select(m => new SesionMisionModelo
            {
                Id = m.Id,
                SesionId = m.SesionId,
                MisionId = m.MisionId,
                Orden = m.Orden
            }).ToList()
        };

        Seleccionar(sesion.TipoSesion).CompletarModelo(sesion, modelo);
        return modelo;
    }

    public Sesion HaciaDominio(SesionModelo modelo)
    {
        var misiones = modelo.Misiones
            .Select(m => SesionMision.Rehidratar(m.Id, m.SesionId, m.MisionId, m.Orden))
            .ToList();

        var secuencia = MapearSecuenciaEtapas(modelo);

        return Seleccionar(modelo.TipoSesion).HaciaDominio(modelo, misiones, secuencia);
    }

    // Serializa el plan como una lista con SOLO los 8 campos canónicos (los mismos
    // que producía el antiguo EtapaPlanificadaSesion). Se usa una proyección
    // anónima —no un tipo con nombre— para no reintroducir un duplicado y para
    // mantener el JSON compatible con lo ya persistido.
    private static string SerializarSecuencia(IReadOnlyList<EjecucionActualSesion> secuencia)
        => JsonSerializer.Serialize(
            secuencia.Select(e => new
            {
                e.MisionId,
                e.EtapaId,
                e.TipoEtapa,
                e.ModoDeJuegoId,
                e.OrdenMision,
                e.OrdenEtapa,
                e.OrdenGlobal,
                e.DuracionSegundos
            }),
            OpcionesJson);

    // Deserialización explícita (JsonDocument) porque EjecucionActualSesion tiene
    // constructor privado. Cada elemento del plan (nuevo o antiguo, mismos 8
    // campos) se rehidrata en fase Planificada mediante la factoría canónica.
    public static IReadOnlyList<EjecucionActualSesion> MapearSecuenciaEtapas(SesionModelo modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.SecuenciaEtapasJson))
            return Array.Empty<EjecucionActualSesion>();

        using var documento = JsonDocument.Parse(modelo.SecuenciaEtapasJson);
        if (documento.RootElement.ValueKind != JsonValueKind.Array)
            return Array.Empty<EjecucionActualSesion>();

        var secuencia = new List<EjecucionActualSesion>();
        foreach (var elemento in documento.RootElement.EnumerateArray())
        {
            secuencia.Add(EjecucionActualSesion.Planificar(
                LeerGuid(elemento, "MisionId"),
                LeerGuid(elemento, "EtapaId"),
                LeerGuid(elemento, "ModoDeJuegoId"),
                LeerString(elemento, "TipoEtapa"),
                LeerInt(elemento, "OrdenGlobal"),
                LeerInt(elemento, "OrdenMision"),
                LeerInt(elemento, "OrdenEtapa"),
                LeerInt(elemento, "DuracionSegundos")));
        }

        return secuencia;
    }

    private static JsonElement Propiedad(JsonElement elemento, string nombre)
    {
        if (elemento.TryGetProperty(nombre, out var propiedad))
            return propiedad;
        foreach (var prop in elemento.EnumerateObject())
            if (string.Equals(prop.Name, nombre, StringComparison.OrdinalIgnoreCase))
                return prop.Value;
        throw new SesionInvalidaExcepcion(
            $"La secuencia de etapas persistida no contiene la propiedad '{nombre}'.");
    }

    private static Guid LeerGuid(JsonElement elemento, string nombre)
        => Propiedad(elemento, nombre).GetGuid();

    private static string LeerString(JsonElement elemento, string nombre)
        => Propiedad(elemento, nombre).GetString() ?? string.Empty;

    private static int LeerInt(JsonElement elemento, string nombre)
        => Propiedad(elemento, nombre).GetInt32();

    public static EjecucionActualSesion? MapearEjecucionActual(SesionModelo modelo)
    {
        if (!modelo.EjecucionActualMisionId.HasValue
            || !modelo.EjecucionActualEtapaId.HasValue
            || !modelo.EjecucionActualModoDeJuegoId.HasValue
            || string.IsNullOrWhiteSpace(modelo.EjecucionActualTipoEtapa)
            || !modelo.EjecucionActualOrdenGlobal.HasValue
            || !modelo.EjecucionActualFechaInicioUtc.HasValue
            || !modelo.EjecucionActualDuracionSegundos.HasValue)
        {
            return null;
        }

        return EjecucionActualSesion.Rehidratar(
            modelo.EjecucionActualMisionId.Value,
            modelo.EjecucionActualEtapaId.Value,
            modelo.EjecucionActualModoDeJuegoId.Value,
            modelo.EjecucionActualTipoEtapa,
            modelo.EjecucionActualOrdenGlobal.Value,
            modelo.EjecucionActualFechaInicioUtc.Value,
            modelo.EjecucionActualDuracionSegundos.Value,
            modelo.EjecucionActualDuracionPausasAcumuladaMs ?? 0,
            modelo.EjecucionActualFechaInicioPausaUtc,
            (FaseEjecucionEtapaSesion)(modelo.EjecucionActualFase ?? (int)FaseEjecucionEtapaSesion.Activa),
            modelo.EjecucionActualOrdenMision ?? 1,
            modelo.EjecucionActualOrdenEtapa ?? 1,
            modelo.EjecucionActualDuracionPreparacionSegundos ?? 0);
    }

    private IMapeadorPersistenciaSesion Seleccionar(string tipoSesion)
        => _mapeadores.FirstOrDefault(m => m.Soporta(tipoSesion))
           ?? throw new SesionInvalidaExcepcion(
               $"No existe un mapeador de persistencia para el tipo de sesión '{tipoSesion}'.");
}
