using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarMision;

public sealed class ModificarMisionManejador : IRequestHandler<ModificarMisionComando>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IClienteSesiones _clienteSesiones;
    private readonly IValidador<ModificarMisionComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ModificarMisionManejador(
        IRepositorioMisiones repositorio,
        IClienteSesiones clienteSesiones,
        IValidador<ModificarMisionComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _clienteSesiones = clienteSesiones;
        _validador = validador;
        _registroLogs = registroLogs;
    }

    public async Task Handle(ModificarMisionComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (await _clienteSesiones.ExisteSesionVigentePorMisionAsync(comando.MisionId, cancelacion))
            throw new MisionConSesionesVigentesExcepcion();

        var mision = await _repositorio.ObtenerMisionPorIdAsync(comando.MisionId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la misión con ID '{comando.MisionId}'.");

        mision.Modificar(comando.Dto.Nombre, comando.Dto.Descripcion, (NivelDificultad)comando.Dto.Dificultad);
        await _repositorio.ActualizarMisionAsync(mision, cancelacion);

        _registroLogs.Informacion(
            evento: "MisionModificada",
            descripcion: "Usuario modificó una misión correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["MisionId"] = comando.MisionId
            });
    }
}
