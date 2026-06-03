using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ActivarMisionManejador : IRequestHandler<ActivarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;

    public ActivarMisionManejador(IRepositorioMisiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ActivarMisionComando comando, CancellationToken cancelacion)
    {
        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        mision.Activar();
        await _repositorio.ActivarMisionAsync(mision, cancelacion);
    }
}
