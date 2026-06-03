using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarBusquedaTesoroManejador
    : IRequestHandler<DesactivarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public DesactivarBusquedaTesoroManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(
        DesactivarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.Desactivar();
        await _repositorio.DesactivarBusquedaTesoroAsync(busqueda, cancelacion);
    }
}
