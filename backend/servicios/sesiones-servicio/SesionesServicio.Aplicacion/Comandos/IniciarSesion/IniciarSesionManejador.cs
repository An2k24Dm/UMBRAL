using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.IniciarSesion;

// Manejador delgado: solo delega en la fachada (patrón Facade).
public sealed class IniciarSesionManejador
    : IRequestHandler<IniciarSesionComando, OperacionSesionRespuestaDto>
{
    private readonly IFachadaOperacionSesion _fachada;

    public IniciarSesionManejador(IFachadaOperacionSesion fachada)
    {
        _fachada = fachada;
    }

    public Task<OperacionSesionRespuestaDto> Handle(
        IniciarSesionComando comando, CancellationToken cancelacion)
        => _fachada.IniciarAsync(comando.SesionId, cancelacion);
}
