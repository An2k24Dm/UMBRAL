using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarMisionManejador : IRequestHandler<EliminarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IClienteSesiones _clienteSesiones;

    public EliminarMisionManejador(
        IRepositorioMisiones repositorio,
        IClienteSesiones clienteSesiones)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
    }

    public async Task Handle(EliminarMisionComando comando, CancellationToken cancelacion)
    {
        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        if (mision.Estado != EstadoMision.Inactiva)
            throw new ExcepcionDominio("Solo se pueden eliminar misiones en estado Inactiva.");

        if (await _clienteSesiones.ExisteSesionVigentePorMisionAsync(comando.MisionId, cancelacion))
            throw new MisionConSesionesVigentesExcepcion();

        await _repositorio.EliminarMisionAsync(comando.MisionId, cancelacion);
    }
}
