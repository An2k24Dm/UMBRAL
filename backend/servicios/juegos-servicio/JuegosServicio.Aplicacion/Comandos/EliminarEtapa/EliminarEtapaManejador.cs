using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarEtapa;

public sealed class EliminarEtapaManejador : IRequestHandler<EliminarEtapaComando>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public EliminarEtapaManejador(
        IRepositorioMisiones repositorio,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _registroLogs = registroLogs;
    }

    public async Task Handle(EliminarEtapaComando comando, CancellationToken cancelacion)
    {
        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        mision.EliminarEtapa(comando.EtapaId);

        await _repositorio.EliminarEtapaAsync(comando.EtapaId, cancelacion);
        await _repositorio.ActualizarOrdenesEtapasAsync(mision.Etapas, cancelacion);

        _registroLogs.Informacion(
            evento: "EtapaEliminada",
            descripcion: "Usuario eliminó una etapa correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["MisionId"] = comando.MisionId,
                ["EtapaId"] = comando.EtapaId
            });
    }
}
