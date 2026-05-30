using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class AgregarEtapaManejador : IRequestHandler<AgregarEtapaComando, Guid>
{
    private readonly IRepositorioBusquedas _repositorio;

    public AgregarEtapaManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Guid> Handle(AgregarEtapaComando comando, CancellationToken cancelacion)
    {
        var busqueda = await _repositorio.ObtenerBusquedaPorIdAsync(comando.BusquedaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la búsqueda del tesoro con ID '{comando.BusquedaId}'.");

        var etapa = busqueda.AgregarEtapa(comando.Dto.Titulo, comando.Dto.Descripcion, comando.Dto.Orden);

        await _repositorio.AgregarEtapaAsync(comando.BusquedaId, etapa, cancelacion);

        return etapa.Id;
    }
}
