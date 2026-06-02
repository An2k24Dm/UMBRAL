using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarBusquedaTesoroManejador : IRequestHandler<EliminarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public EliminarBusquedaTesoroManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(EliminarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        if (busqueda.Estado != EstadoBusqueda.Inactiva)
            throw new ExcepcionDominio("Solo se pueden eliminar búsquedas del tesoro en estado Inactiva.");

        await _repositorio.EliminarBusquedaTesoroAsync(comando.BusquedaId, cancelacion);
    }
}
