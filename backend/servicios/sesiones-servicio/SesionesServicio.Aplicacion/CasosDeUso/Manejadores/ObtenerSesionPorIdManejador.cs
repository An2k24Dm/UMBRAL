using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerSesionPorIdManejador
    : IRequestHandler<ObtenerSesionPorIdConsulta, SesionDetalleDto?>
{
    private readonly IRepositorioSesiones _repositorio;

    public ObtenerSesionPorIdManejador(IRepositorioSesiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<SesionDetalleDto?> Handle(
        ObtenerSesionPorIdConsulta consulta, CancellationToken cancelacion)
    {
        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.Id, cancelacion);
        if (sesion is null) return null;

        return new SesionDetalleDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            TipoJuego = sesion.TipoJuego.ToString(),
            ContenidoJuegoId = sesion.ContenidoJuegoId,
            Modo = sesion.Modo.ToString(),
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CreadaPorUsuarioId = sesion.CreadaPorUsuarioId,
            FechaCreacion = sesion.FechaCreacion
        };
    }
}
