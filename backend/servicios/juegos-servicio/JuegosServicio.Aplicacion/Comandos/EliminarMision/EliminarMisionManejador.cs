using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarMision;

public sealed class EliminarMisionManejador : IRequestHandler<EliminarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IClienteSesiones _clienteSesiones;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public EliminarMisionManejador(
        IRepositorioMisiones repositorio,
        IClienteSesiones clienteSesiones,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
        _registroLogs = registroLogs;
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

        _registroLogs.Informacion(
            evento: "MisionEliminada",
            descripcion: "Usuario eliminó una misión correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["MisionId"] = comando.MisionId
            });
    }
}
