using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class AgregarPistaManejador : IRequestHandler<AgregarPistaComando, Guid>
{
    private readonly IRepositorioBusquedas _repositorio;

    public AgregarPistaManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Guid> Handle(AgregarPistaComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        var pista = busqueda.AgregarPista(comando.Dto.Contenido);

        await _repositorio.AgregarPistaAsync(pista, cancelacion);

        return pista.Id;
    }
}
