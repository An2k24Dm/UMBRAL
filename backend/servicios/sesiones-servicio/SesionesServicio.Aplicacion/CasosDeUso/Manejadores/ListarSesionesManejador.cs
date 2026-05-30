using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ListarSesionesManejador
    : IRequestHandler<ListarSesionesConsulta, List<SesionListadoDto>>
{
    private readonly IRepositorioSesiones _repositorio;

    public ListarSesionesManejador(IRepositorioSesiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<SesionListadoDto>> Handle(
        ListarSesionesConsulta consulta, CancellationToken cancelacion)
    {
        var sesiones = await _repositorio.ListarAsync(cancelacion);

        return sesiones.Select(s => new SesionListadoDto
        {
            Id = s.Id,
            Nombre = s.Nombre,
            TipoJuego = s.TipoJuego.ToString(),
            ContenidoJuegoId = s.ContenidoJuegoId,
            Modo = s.Modo.ToString(),
            Estado = s.Estado.ToString(),
            FechaProgramada = s.FechaProgramada
        }).ToList();
    }
}
