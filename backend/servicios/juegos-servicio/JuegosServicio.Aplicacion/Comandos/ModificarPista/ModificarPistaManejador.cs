using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarPista;

public sealed class ModificarPistaManejador : IRequestHandler<ModificarPistaComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;

    public ModificarPistaManejador(
        IRepositorioBusquedas repositorio,
        IRepositorioMisiones repositorioMisiones)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
    }

    public async Task Handle(ModificarPistaComando comando, CancellationToken cancelacion)
    {
        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.BusquedaTesoro, comando.BusquedaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.ModificarPista(comando.PistaId, comando.Dto.NuevoContenido);

        var pista = busqueda.Pistas.First(p => p.Id == comando.PistaId);
        await _repositorio.ModificarPistaAsync(pista, cancelacion);
    }
}
