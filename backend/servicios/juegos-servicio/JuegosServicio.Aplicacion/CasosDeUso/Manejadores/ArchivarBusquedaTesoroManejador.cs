using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ArchivarBusquedaTesoroManejador : IRequestHandler<ArchivarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IClienteSesiones _clienteSesiones;

    public ArchivarBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IClienteSesiones clienteSesiones)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
    }

    public async Task Handle(ArchivarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        // 1. Resolver el agregado. Si no existe, 404 vía middleware.
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        // 2. Antes de tocar el estado de dominio, consultar a sesiones-
        //    servicio: si hay sesión vigente apuntando a esta búsqueda
        //    del tesoro, no podemos archivarla. La regla se evalúa
        //    ANTES de Archivar() para no persistir cambios si falla.
        var tieneSesionesVigentes = await _clienteSesiones
            .ExisteSesionVigentePorContenidoAsync(
                TipoJuego.BusquedaTesoro, busqueda.Id, cancelacion);
        if (tieneSesionesVigentes)
            throw new ContenidoConSesionesVigentesExcepcion(
                TipoJuego.BusquedaTesoro, busqueda.Id);

        // 3. Transición de estado y persistencia.
        busqueda.Desactivar();
        await _repositorio.ArchivarBusquedaTesoroAsync(busqueda, cancelacion);
    }
}
