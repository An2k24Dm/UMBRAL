using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ModificarEtapaManejador : IRequestHandler<ModificarEtapaComando>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ModificarEtapaManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ModificarEtapaComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        busqueda.ModificarEtapa(comando.EtapaId, comando.Dto.NuevoTitulo, comando.Dto.NuevaDescripcion);

        var etapa = busqueda.Etapas.First(e => e.Id == comando.EtapaId);
        await _repositorio.ModificarEtapaAsync(comando.BusquedaId, etapa, cancelacion);
    }
}
