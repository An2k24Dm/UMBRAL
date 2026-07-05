using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarBusquedaTesoro;

public sealed class EliminarBusquedaTesoroManejador : IRequestHandler<EliminarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public EliminarBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IRepositorioMisiones repositorioMisiones,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _registroLogs = registroLogs;
    }

    public async Task Handle(EliminarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        if (busqueda.Estado != EstadoBusqueda.Inactiva)
            throw new ExcepcionDominio("Solo se pueden eliminar búsquedas del tesoro en estado Inactiva.");

        if (await _repositorioMisiones.EsContenidoUsadoEnEtapaAsync(TipoModoDeJuego.BusquedaTesoro, comando.BusquedaId, cancelacion))
            throw new ExcepcionDominio("No se puede eliminar la búsqueda del tesoro porque está asignada a una o más misiones.");

        await _repositorio.EliminarBusquedaTesoroAsync(comando.BusquedaId, cancelacion);

        _registroLogs.Informacion(
            evento: "BusquedaTesoroEliminada",
            descripcion: "Usuario eliminó una búsqueda del tesoro correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["BusquedaId"] = comando.BusquedaId
            });
    }
}
