using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ActivarBusquedaTesoro;

public sealed class ActivarBusquedaTesoroManejador : IRequestHandler<ActivarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ActivarBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _registroLogs = registroLogs;
    }

    public async Task Handle(ActivarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.Activar();

        await _repositorio.ActivarBusquedaTesoroAsync(busqueda, cancelacion);

        _registroLogs.Informacion(
            evento: "BusquedaTesoroActivada",
            descripcion: "Usuario activó una búsqueda del tesoro correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["BusquedaId"] = comando.BusquedaId
            });
    }
}
