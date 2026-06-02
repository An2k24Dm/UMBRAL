using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarBusquedaTesoroManejador : IRequestHandler<DesactivarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;
    private readonly IClienteSesiones _clienteSesiones;

    public DesactivarBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IClienteSesiones clienteSesiones)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
    }

    public async Task Handle(DesactivarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        // Consultar sesiones ANTES de cambiar estado: si hay sesión vigente, no se puede desactivar.
        var tieneSesionesVigentes = await _clienteSesiones
            .ExisteSesionVigentePorContenidoAsync(
                TipoJuego.BusquedaTesoro, busqueda.Id, cancelacion);
        if (tieneSesionesVigentes)
            throw new ContenidoConSesionesVigentesExcepcion(
                TipoJuego.BusquedaTesoro, busqueda.Id);

        busqueda.Desactivar();
        await _repositorio.DesactivarBusquedaTesoroAsync(busqueda, cancelacion);
    }
}
