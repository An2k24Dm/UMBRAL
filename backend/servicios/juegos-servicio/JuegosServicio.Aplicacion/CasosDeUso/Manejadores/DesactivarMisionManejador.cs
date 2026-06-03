using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarMisionManejador : IRequestHandler<DesactivarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;

    public DesactivarMisionManejador(IRepositorioMisiones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(DesactivarMisionComando comando, CancellationToken cancelacion)
    {
        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        mision.Desactivar();
        await _repositorio.DesactivarMisionAsync(mision, cancelacion);
    }
}
