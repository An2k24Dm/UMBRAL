using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.DesactivarBusquedaTesoro;

public sealed class DesactivarBusquedaTesoroManejador
    : IRequestHandler<DesactivarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public DesactivarBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _registroLogs = registroLogs;
    }

    public async Task Handle(
        DesactivarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.Desactivar();
        await _repositorio.DesactivarBusquedaTesoroAsync(busqueda, cancelacion);

        _registroLogs.Informacion(
            evento: "BusquedaTesoroDesactivada",
            descripcion: "Usuario desactivó una búsqueda del tesoro correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["BusquedaId"] = comando.BusquedaId
            });
    }
}
