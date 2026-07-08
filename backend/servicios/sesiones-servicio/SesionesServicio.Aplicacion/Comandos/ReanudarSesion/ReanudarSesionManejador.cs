using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.ReanudarSesion;

// Manejador delgado: solo delega en la fachada (patrón Facade).
public sealed class ReanudarSesionManejador
    : IRequestHandler<ReanudarSesionComando, OperacionSesionRespuestaDto>
{
    private readonly IFachadaOperacionSesion _fachada;

    public ReanudarSesionManejador(IFachadaOperacionSesion fachada)
    {
        _fachada = fachada;
    }

    public Task<OperacionSesionRespuestaDto> Handle(
        ReanudarSesionComando comando, CancellationToken cancelacion)
        => _fachada.ReanudarAsync(comando.SesionId, cancelacion);
}
