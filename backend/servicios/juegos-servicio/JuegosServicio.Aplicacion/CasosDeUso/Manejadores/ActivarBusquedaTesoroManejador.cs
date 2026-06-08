using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ActivarBusquedaTesoroManejador : IRequestHandler<ActivarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ActivarBusquedaTesoroManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ActivarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.Activar();

        await _repositorio.ActivarBusquedaTesoroAsync(busqueda, cancelacion);
    }
}
