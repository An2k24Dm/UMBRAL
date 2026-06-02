using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ModificarMisionManejador : IRequestHandler<ModificarMisionComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ModificarMisionManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ModificarMisionComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.ModificarMision(
            comando.Dto.NuevoTitulo,
            comando.Dto.NuevaDescripcion,
            (TipoMision)comando.Dto.NuevoTipo,
            comando.Dto.NuevaPistaClave);

        await _repositorio.ModificarMisionAsync(busqueda.Mision!, cancelacion);
    }
}
