using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarPistaManejador : IRequestHandler<EliminarPistaComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;

    public EliminarPistaManejador(
        IRepositorioBusquedas repositorio,
        IRepositorioMisiones repositorioMisiones)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
    }

    public async Task Handle(EliminarPistaComando comando, CancellationToken cancelacion)
    {
        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.BusquedaTesoro, comando.BusquedaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.EliminarPista(comando.PistaId);

        await _repositorio.EliminarPistaAsync(comando.PistaId, cancelacion);
    }
}
