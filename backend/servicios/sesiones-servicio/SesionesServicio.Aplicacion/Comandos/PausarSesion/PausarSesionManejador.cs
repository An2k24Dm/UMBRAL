using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.PausarSesion;

// Manejador delgado: solo delega en la fachada (patrón Facade).
public sealed class PausarSesionManejador
    : IRequestHandler<PausarSesionComando, OperacionSesionRespuestaDto>
{
    private readonly IFachadaOperacionSesion _fachada;

    public PausarSesionManejador(IFachadaOperacionSesion fachada)
    {
        _fachada = fachada;
    }

    public Task<OperacionSesionRespuestaDto> Handle(
        PausarSesionComando comando, CancellationToken cancelacion)
        => _fachada.PausarAsync(comando.SesionId, cancelacion);
}
