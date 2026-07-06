using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.CancelarSesion;

// Manejador delgado: solo delega en la fachada (patrón Facade).
public sealed class CancelarSesionManejador
    : IRequestHandler<CancelarSesionComando, OperacionSesionRespuestaDto>
{
    private readonly IFachadaOperacionSesion _fachada;

    public CancelarSesionManejador(IFachadaOperacionSesion fachada)
    {
        _fachada = fachada;
    }

    public Task<OperacionSesionRespuestaDto> Handle(
        CancelarSesionComando comando, CancellationToken cancelacion)
        => _fachada.CancelarAsync(comando.SesionId, cancelacion);
}
