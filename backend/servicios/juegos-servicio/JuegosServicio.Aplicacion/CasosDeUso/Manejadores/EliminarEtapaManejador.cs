using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarEtapaManejador : IRequestHandler<EliminarEtapaComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public EliminarEtapaManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(EliminarEtapaComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.EliminarEtapa(comando.EtapaId);

        await _repositorio.EliminarEtapaAsync(comando.BusquedaId, comando.EtapaId, cancelacion);
    }
}
