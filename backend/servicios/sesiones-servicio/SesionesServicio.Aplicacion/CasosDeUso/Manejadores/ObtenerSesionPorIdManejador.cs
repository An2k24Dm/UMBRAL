using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerSesionPorIdManejador
    : IRequestHandler<ObtenerSesionPorIdConsulta, SesionDetalleDto?>
{
    private const string RolAdministrador = "Administrador";
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUsuarioActual _usuarioActual;

    public ObtenerSesionPorIdManejador(
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual)
    {
        _repositorio = repositorio;
        _usuarioActual = usuarioActual;
    }

    public async Task<SesionDetalleDto?> Handle(
        ObtenerSesionPorIdConsulta consulta, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado)
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para consultar sesiones.");

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador, RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No tiene permiso para consultar sesiones.");

        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.Id, cancelacion);
        if (sesion is null) return null;

        if (!_usuarioActual.TieneAlgunRol(RolAdministrador))
        {
            var operadorId = _usuarioActual.Id ?? Guid.Empty;
            if (sesion.OperadorCreadorId != operadorId)
                throw new AccesoSesionNoPermitidoExcepcion(
                    "No tiene permiso para ver esta sesión.");
        }

        var detalle = new SesionDetalleDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Modo = sesion.TipoSesion,
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CodigoAcceso = sesion.CodigoAcceso,
            OperadorCreadorId = sesion.OperadorCreadorId,
            FechaCreacion = sesion.FechaCreacion,
            FechaInicioUtc = sesion.FechaInicioUtc,
            FechaFinalizacionUtc = sesion.FechaFinalizacionUtc,
            Misiones = sesion.Misiones
                .OrderBy(m => m.Orden)
                .Select(m => new SesionMisionDto
                {
                    Id = m.Id,
                    MisionId = m.MisionId,
                    Orden = m.Orden
                }).ToList()
        };

        // Proyección polimórfica: cada subclase puebla solo su sección.
        switch (sesion)
        {
            case SesionIndividual individual:
                detalle.ParticipantesIndividuales = individual.Participantes
                    .Select(p => new ParticipanteSesionDto
                    {
                        Id = p.Id,
                        ParticipanteId = p.ParticipanteIdentidadId,
                        FechaUnion = p.FechaUnionSesion
                    }).ToList();
                break;

            case SesionGrupal grupal:
                detalle.Equipos = grupal.Equipos
                    .Select(e => new EquipoSesionDto
                    {
                        Id = e.Id,
                        Nombre = e.Nombre,
                        PuntajeActual = e.Puntaje,
                        FechaCreacion = e.FechaCreacion,
                        LiderParticipanteId = e.LiderParticipanteId,
                        Participantes = e.Participantes
                            .Select(p => new ParticipanteEquipoDto
                            {
                                Id = p.Id,
                                ParticipanteId = p.ParticipanteIdentidadId,
                                FechaUnion = p.FechaUnionEquipo ?? p.FechaUnionSesion
                            }).ToList()
                    }).ToList();
                break;
        }

        return detalle;
    }
}
