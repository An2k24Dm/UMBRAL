using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarBusquedaTesoro;

public sealed class ModificarBusquedaTesoroManejador : IRequestHandler<ModificarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IValidador<ModificarBusquedaTesoroComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ModificarBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<ModificarBusquedaTesoroComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
        _registroLogs = registroLogs;
    }

    public async Task Handle(ModificarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.BusquedaTesoro, comando.BusquedaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.Modificar(
            comando.Dto.Nombre,
            comando.Dto.Descripcion,
            Tiempo.CrearParaBusqueda(comando.Dto.Tiempo),
            Puntaje.CrearParaBusqueda(comando.Dto.Puntaje));

        await _repositorio.ActualizarBusquedaAsync(busqueda, cancelacion);

        _registroLogs.Informacion(
            evento: "BusquedaTesoroModificada",
            descripcion: "Usuario modificó una búsqueda del tesoro correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["BusquedaId"] = comando.BusquedaId
            });
    }
}
