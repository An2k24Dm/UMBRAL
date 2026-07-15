using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.DesactivarMision;

public sealed class DesactivarMisionManejador : IRequestHandler<DesactivarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IClienteSesiones _clienteSesiones;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public DesactivarMisionManejador(
        IRepositorioMisiones repositorio,
        IClienteSesiones clienteSesiones,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
        _registroLogs = registroLogs;
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

        _registroLogs.Informacion(
            evento: "MisionDesactivada",
            descripcion: "Usuario desactivó una misión correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["MisionId"] = comando.MisionId
            });
    }
}
