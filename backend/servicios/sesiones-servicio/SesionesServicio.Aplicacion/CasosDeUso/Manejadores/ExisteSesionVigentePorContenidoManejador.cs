using MediatR;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Manejadores;

// HU33 — Delega en el repositorio la verificación de existencia. La
// regla de "qué estados son vigentes" la conoce sesiones-servicio
// (dueño del agregado Sesion); juegos-servicio sólo consume el
// booleano resultado.
public sealed class ExisteSesionVigentePorContenidoManejador
    : IRequestHandler<ExisteSesionVigentePorContenidoConsulta, ExisteSesionVigenteRespuestaDto>
{
    private readonly IRepositorioSesiones _repositorio;

    public ExisteSesionVigentePorContenidoManejador(IRepositorioSesiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ExisteSesionVigenteRespuestaDto> Handle(
        ExisteSesionVigentePorContenidoConsulta consulta, CancellationToken cancelacion)
    {
        var existe = await _repositorio.ExisteSesionVigentePorContenidoAsync(
            consulta.TipoJuego, consulta.ContenidoJuegoId, cancelacion);

        return new ExisteSesionVigenteRespuestaDto { Existe = existe };
    }
}
