using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarEtapaManejador : IRequestHandler<EliminarEtapaComando>
{
    private readonly IRepositorioMisiones _repositorio;

    public EliminarEtapaManejador(IRepositorioMisiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(EliminarEtapaComando comando, CancellationToken cancelacion)
    {
        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        mision.EliminarEtapa(comando.EtapaId);

        await _repositorio.EliminarEtapaAsync(comando.EtapaId, cancelacion);
        await _repositorio.ActualizarOrdenesEtapasAsync(mision.Etapas, cancelacion);
    }
}
