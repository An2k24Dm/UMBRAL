using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarMisionManejador : IRequestHandler<DesactivarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IClienteSesiones _clienteSesiones;

    public DesactivarMisionManejador(
        IRepositorioMisiones repositorio,
        IClienteSesiones clienteSesiones)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
    }

    public async Task Handle(DesactivarMisionComando comando, CancellationToken cancelacion)
    {
        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        if (await _clienteSesiones.ExisteSesionVigentePorMisionAsync(comando.MisionId, cancelacion))
            throw new MisionConSesionesVigentesExcepcion();

        mision.Desactivar();
        await _repositorio.DesactivarMisionAsync(mision, cancelacion);
    }
}
