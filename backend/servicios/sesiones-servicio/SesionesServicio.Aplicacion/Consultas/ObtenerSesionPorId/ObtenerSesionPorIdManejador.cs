using MediatR;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerSesionPorId;

public sealed class ObtenerSesionPorIdManejador
    : IRequestHandler<ObtenerSesionPorIdConsulta, SesionDetalleDto?>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;
    private readonly FabricaMapeadorDetalleSesion _fabricaMapeador;
    private readonly IClienteIdentidadParticipantes _clienteIdentidadParticipantes;
    private readonly IServicioFinalizacionSesion _finalizacion;

    public ObtenerSesionPorIdManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual,
        FabricaMapeadorDetalleSesion fabricaMapeador,
        IClienteIdentidadParticipantes clienteIdentidadParticipantes,
        IServicioFinalizacionSesion finalizacion)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
        _fabricaMapeador = fabricaMapeador;
        _clienteIdentidadParticipantes = clienteIdentidadParticipantes;
        _finalizacion = finalizacion;
    }

    public async Task<SesionDetalleDto?> Handle(
        ObtenerSesionPorIdConsulta consulta, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para consultar sesiones.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No tiene permiso para consultar sesiones.");

        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.Id, cancelacion);
        if (sesion is null) return null;

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador))
        {
            var operadorId = _usuarioActual.ObtenerId() ?? Guid.Empty;
            if (sesion.OperadorCreadorId != operadorId)
                throw new AccesoSesionNoPermitidoExcepcion(
                    "No tiene permiso para ver esta sesión.");
        }

        if (await _finalizacion.FinalizarSesionSiDuracionVencidaAsync(
                sesion.Id, cancelacion))
        {
            sesion = await _repositorio.ObtenerPorIdAsync(consulta.Id, cancelacion);
            if (sesion is null) return null;
        }

        // El mapeo del detalle (incluida la parte específica del tipo de
        // sesión) lo resuelve la estrategia compatible. El manejador no
        // conoce SesionIndividual ni SesionGrupal.
        var dto = _fabricaMapeador.Mapear(sesion);
        await CompletarDatosParticipantesIndividualesAsync(dto, cancelacion);

        return dto;
    }

    private async Task CompletarDatosParticipantesIndividualesAsync(
        SesionDetalleDto dto,
        CancellationToken cancelacion)
    {
        if (dto.ParticipantesIndividuales.Count == 0)
            return;

        var ids = dto.ParticipantesIndividuales
            .Select(p => p.ParticipanteIdentidadId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return;

        var datosIdentidad = await _clienteIdentidadParticipantes
            .ObtenerParticipantesPorIdsAsync(ids, cancelacion);

        foreach (var participante in dto.ParticipantesIndividuales)
        {
            if (!datosIdentidad.TryGetValue(
                    participante.ParticipanteIdentidadId,
                    out var datos))
                continue;

            participante.Alias = datos.Alias;
            participante.Nombre = datos.Nombre;
            participante.Apellido = datos.Apellido;
        }

        dto.ParticipantesIndividuales = dto.ParticipantesIndividuales
            .OrderBy(p => p.FechaUnion)
            .ToList();
    }
}
