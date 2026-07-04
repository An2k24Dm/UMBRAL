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

    public ModificarBusquedaTesoroManejador(
        IRepositorioBusquedas repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<ModificarBusquedaTesoroComando> validador)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
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
    }
}
