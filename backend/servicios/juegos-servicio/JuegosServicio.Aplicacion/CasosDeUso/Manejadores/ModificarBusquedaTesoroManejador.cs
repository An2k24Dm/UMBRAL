using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ModificarBusquedaTesoroManejador : IRequestHandler<ModificarBusquedaTesoroComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ModificarBusquedaTesoroManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ModificarBusquedaTesoroComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.Modificar(
            comando.Dto.Nombre,
            comando.Dto.Descripcion,
            comando.Dto.Tiempo,
            comando.Dto.Puntaje);

        await _repositorio.ActualizarBusquedaAsync(busqueda, cancelacion);
    }
}
