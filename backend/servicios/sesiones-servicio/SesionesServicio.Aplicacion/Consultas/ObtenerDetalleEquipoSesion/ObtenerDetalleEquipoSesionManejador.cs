using MediatR;
using SesionesServicio.Aplicacion.Consultas.Equipos;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerDetalleEquipoSesion;

public sealed class ObtenerDetalleEquipoSesionManejador
    : IRequestHandler<ObtenerDetalleEquipoSesionConsulta, EquipoSesionDetalleDto>
{
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IClienteIdentidadParticipantes _clienteIdentidad;

    public ObtenerDetalleEquipoSesionManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual,
        IClienteIdentidadParticipantes clienteIdentidad)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _clienteIdentidad = clienteIdentidad;
    }

    public async Task<EquipoSesionDetalleDto> Handle(
        ObtenerDetalleEquipoSesionConsulta consulta, CancellationToken cancelacion)
    {
        var (sesion, usuarioId) = await AccesoConsultaEquipos.ResolverSesionAutorizadaAsync(
            consulta.SesionId, _repositorio, _usuarioActual, cancelacion);

        var equipo = sesion.Equipos.FirstOrDefault(e => e.Id == consulta.EquipoId)
            ?? throw new EquipoNoEncontradoExcepcion(
                "El equipo solicitado no existe en esta sesión.");

        var idsIdentidad = equipo.Participantes
            .Select(p => p.ParticipanteIdentidadId)
            .ToList();
        var datosIdentidad = await _clienteIdentidad.ObtenerParticipantesPorIdsAsync(
            idsIdentidad, cancelacion);

        var integrantes = equipo.Participantes
            .Select(p => MapearIntegrante(p, equipo, datosIdentidad))
            // Líder primero; luego por alias.
            .OrderByDescending(i => i.EsLider)
            .ThenBy(i => i.Alias, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new EquipoSesionDetalleDto
        {
            Id = equipo.Id,
            SesionId = equipo.SesionId,
            Nombre = equipo.Nombre.Valor,
            Tipo = equipo.Tipo.ToString(),
            Puntaje = equipo.Puntaje,
            CantidadParticipantes = equipo.Participantes.Count,
            CapacidadMaxima = equipo.CapacidadMaxima,
            FechaCreacion = equipo.FechaCreacion,
            EstaLleno = equipo.EstaLleno(),
            LiderParticipanteId = equipo.LiderParticipanteId,
            EsMiEquipo = AccesoConsultaEquipos.EsMiEquipo(equipo, usuarioId),
            SoyLider = AccesoConsultaEquipos.SoyLider(equipo, usuarioId),
            Participantes = integrantes
        };
    }

    private static IntegranteEquipoDto MapearIntegrante(
        Participante participante,
        Equipo equipo,
        IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto> datosIdentidad)
    {
        datosIdentidad.TryGetValue(participante.ParticipanteIdentidadId, out var datos);

        // Fallback controlado: nunca convertimos un identificador técnico en
        // el texto principal que ve el usuario.
        return new IntegranteEquipoDto
        {
            ParticipanteSesionId = participante.Id,
            ParticipanteIdentidadId = participante.ParticipanteIdentidadId,
            Nombre = datos?.Nombre ?? "Participante",
            Apellido = datos?.Apellido ?? string.Empty,
            Alias = string.IsNullOrWhiteSpace(datos?.Alias) ? "Participante" : datos!.Alias,
            Puntaje = participante.Puntaje,
            FechaUnion = participante.FechaUnionEquipo ?? participante.FechaUnionSesion,
            EsLider = equipo.LiderParticipanteId == participante.Id
        };
    }
}
