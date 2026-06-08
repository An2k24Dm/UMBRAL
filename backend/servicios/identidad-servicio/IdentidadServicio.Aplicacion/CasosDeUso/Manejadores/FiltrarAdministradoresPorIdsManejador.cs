using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU34 — Manejador de la consulta de filtrado por rol Administrador.
// Devuelve únicamente identificadores; ningún dato personal sale del
// microservicio. Si la lista llega vacía, se evita el roundtrip a BD.
public sealed class FiltrarAdministradoresPorIdsManejador
    : IRequestHandler<FiltrarAdministradoresPorIdsConsulta, AdministradoresPorIdsRespuestaDto>
{
    private readonly IRepositorioUsuariosLectura _repositorio;

    public FiltrarAdministradoresPorIdsManejador(IRepositorioUsuariosLectura repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<AdministradoresPorIdsRespuestaDto> Handle(
        FiltrarAdministradoresPorIdsConsulta consulta, CancellationToken cancelacion)
    {
        if (consulta.UsuariosIds is null || consulta.UsuariosIds.Count == 0)
            return new AdministradoresPorIdsRespuestaDto
            {
                AdministradoresIds = Array.Empty<Guid>()
            };

        var ids = await _repositorio.FiltrarAdministradoresPorIdsAsync(
            consulta.UsuariosIds, cancelacion);

        return new AdministradoresPorIdsRespuestaDto
        {
            AdministradoresIds = ids
        };
    }
}
