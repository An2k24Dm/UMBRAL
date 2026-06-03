using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ModificarPistaManejador : IRequestHandler<ModificarPistaComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ModificarPistaManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ModificarPistaComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.ModificarPista(comando.PistaId, comando.Dto.NuevoContenido);

        var pista = busqueda.Pistas.First(p => p.Id == comando.PistaId);
        await _repositorio.ModificarPistaAsync(pista, cancelacion);
    }
}
