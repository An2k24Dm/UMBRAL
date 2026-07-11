using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerBusquedaTesoroConPistas;

public sealed class ObtenerBusquedaTesoroConPistasManejador
    : IRequestHandler<ObtenerBusquedaTesoroConPistasConsulta, BusquedaTesoroConPistasDto?>
{
    private const string TipoTesoro = "BusquedaTesoro";

    private readonly IClienteBusquedaTesoro _clienteTesoro;
    private readonly IRepositorioPistasLiberadas _repositorioPistas;
    private readonly IRepositorioEvidenciasTesoro _repositorioEvidencias;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IServicioProgresoSecuencialSesion _servicioProgresoSecuencial;

    public ObtenerBusquedaTesoroConPistasManejador(
        IClienteBusquedaTesoro clienteTesoro,
        IRepositorioPistasLiberadas repositorioPistas,
        IRepositorioEvidenciasTesoro repositorioEvidencias,
        IRepositorioSesiones repositorioSesiones,
        IServicioProgresoSecuencialSesion servicioProgresoSecuencial)
    {
        _clienteTesoro = clienteTesoro;
        _repositorioPistas = repositorioPistas;
        _repositorioEvidencias = repositorioEvidencias;
        _repositorioSesiones = repositorioSesiones;
        _servicioProgresoSecuencial = servicioProgresoSecuencial;
    }

    public async Task<BusquedaTesoroConPistasDto?> Handle(
        ObtenerBusquedaTesoroConPistasConsulta consulta, CancellationToken cancelacion)
    {
        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(consulta.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesion solicitada no existe.");

        // Autoridad de lectura: solo puede consultarse el contenido de la etapa
        // actual permitida para el participante/equipo. Esto valida además que el
        // participante esté inscrito (grupal sin equipo => ParticipacionInvalida),
        // que la misión pertenezca a la sesión, que la etapa sea de tipo Tesoro y
        // que el BusquedaId corresponda a esa etapa. No se consulta contenido futuro.
        await _servicioProgresoSecuencial.ValidarEtapaActualAsync(
            sesion,
            consulta.ParticipanteIdentidadId,
            consulta.MisionId,
            consulta.EtapaId,
            TipoTesoro,
            consulta.BusquedaId,
            cancelacion);

        var busqueda = await _clienteTesoro.ObtenerBusquedaParticipanteAsync(
            consulta.BusquedaId, cancelacion);
        if (busqueda is null) return null;

        var pistasLiberadas = await _repositorioPistas.ObtenerPorEtapaAsync(
            consulta.SesionId, consulta.EtapaId, cancelacion);

        // "Ya completada" refleja evidencia VÁLIDA del jugador: en grupal, la del
        // equipo (cualquier integrante); en individual, la del participante.
        var equipoId = ObtenerEquipoId(sesion, consulta.ParticipanteIdentidadId);

        var yaCompletado = equipoId.HasValue
            ? await _repositorioEvidencias.ExisteEvidenciaValidaEquipoAsync(
                consulta.SesionId, consulta.EtapaId, equipoId.Value, cancelacion)
            : await _repositorioEvidencias.ExisteEvidenciaValidaIndividualAsync(
                consulta.SesionId, consulta.EtapaId, consulta.ParticipanteIdentidadId, cancelacion);

        return new BusquedaTesoroConPistasDto
        {
            Id = busqueda.Id,
            Nombre = busqueda.Nombre,
            Descripcion = busqueda.Descripcion,
            TiempoSegundos = busqueda.Tiempo * 60,
            PuntajeBase = busqueda.Puntaje,
            PistasLiberadas = pistasLiberadas.Select(p => new PistaLiberadaSesionDto
            {
                PistaId = p.PistaId,
                Contenido = p.Contenido,
                FechaLiberacionUtc = p.FechaLiberacionUtc
            }).ToList(),
            YaEnvioEvidencia = yaCompletado
        };
    }

    // Identidad lógica del jugador. Nunca convierte silenciosamente la falta de
    // participación grupal en comportamiento individual: exige inscripción real.
    private static Guid? ObtenerEquipoId(Sesion sesion, Guid participanteIdentidadId)
    {
        if (sesion is SesionIndividual individual)
        {
            if (!individual.Participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId))
                throw new ParticipacionInvalidaExcepcion(
                    "El participante no esta inscrito en esta sesion.");
            return null;
        }

        if (sesion is SesionGrupal grupal)
        {
            var equipo = grupal.Equipos.FirstOrDefault(e =>
                e.Participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId));
            if (equipo is null)
                throw new ParticipacionInvalidaExcepcion(
                    "El participante no esta inscrito en esta sesion.");
            return equipo.Id;
        }

        throw new SesionInvalidaExcepcion("Tipo de sesion no soportado.");
    }
}
