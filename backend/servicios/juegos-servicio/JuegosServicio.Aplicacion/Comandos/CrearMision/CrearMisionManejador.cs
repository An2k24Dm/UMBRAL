using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.CrearMision;

public sealed class CrearMisionManejador : IRequestHandler<CrearMisionComando, Guid>
{
    private readonly IRepositorioMisiones _repositorio;
    private readonly IProveedorFechaHora _fechaHora;
    private readonly IValidador<CrearMisionComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public CrearMisionManejador(
        IRepositorioMisiones repositorio,
        IProveedorFechaHora fechaHora,
        IValidador<CrearMisionComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _fechaHora = fechaHora;
        _validador = validador;
        _registroLogs = registroLogs;
    }

    public async Task<Guid> Handle(CrearMisionComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (await _repositorio.ExisteMisionConNombreAsync(comando.Dto.Nombre, cancelacion))
            throw new ExcepcionDominio($"Ya existe una misión con el nombre '{comando.Dto.Nombre}'.");

        var mision = Mision.Crear(
            comando.Dto.Nombre,
            comando.Dto.Descripcion,
            comando.CreadorId,
            _fechaHora.ObtenerFechaHoraUtc(),
            (NivelDificultad)comando.Dto.Dificultad);

        await _repositorio.CrearMisionAsync(mision, cancelacion);

        _registroLogs.Informacion(
            evento: "MisionCreada",
            descripcion: "Usuario creó una misión correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["MisionId"] = mision.Id
            });

        return mision.Id;
    }
}
