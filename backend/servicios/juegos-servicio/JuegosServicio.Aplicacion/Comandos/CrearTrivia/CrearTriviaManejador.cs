using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.CrearTrivia;

public sealed class CrearTriviaManejador : IRequestHandler<CrearTriviaComando, Guid>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IProveedorFechaHora _reloj;
    private readonly IValidador<CrearTriviaComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public CrearTriviaManejador(
        IRepositorioJuegos repositorio,
        IProveedorFechaHora reloj,
        IValidador<CrearTriviaComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _validador = validador;
        _registroLogs = registroLogs;
    }

    public async Task<Guid> Handle(CrearTriviaComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        var dto = comando.Datos;

        if (await _repositorio.ExisteTriviaConNombreAsync(dto.Nombre, cancelacion))
            throw new ExcepcionDominio($"Ya existe una trivia con el nombre '{dto.Nombre}'.");

        var trivia = Trivia.Crear(
            dto.Nombre,
            dto.Descripcion,
            comando.CreadorId,
            Tiempo.CrearPositivo(dto.TiempoLimitePorPregunta),
            _reloj.ObtenerFechaHoraUtc());

        await _repositorio.AgregarTriviaAsync(trivia, cancelacion);

        _registroLogs.Informacion(
            evento: "TriviaCreada",
            descripcion: "Usuario creó una trivia correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["TriviaId"] = trivia.Id,
                ["Nombre"] = trivia.Nombre,
                ["CreadorId"] = trivia.CreadorId
            });

        return trivia.Id;
    }
}
