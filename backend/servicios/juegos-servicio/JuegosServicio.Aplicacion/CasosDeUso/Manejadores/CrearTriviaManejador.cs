using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class CrearTriviaManejador : IRequestHandler<CrearTriviaComando, Guid>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IProveedorFechaHora _reloj;
    private readonly IValidador<CrearTriviaComando> _validador;
    private readonly ILogger<CrearTriviaManejador> _registro;

    public CrearTriviaManejador(
        IRepositorioJuegos repositorio,
        IProveedorFechaHora reloj,
        IValidador<CrearTriviaComando> validador,
        ILogger<CrearTriviaManejador> registro)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _validador = validador;
        _registro = registro;
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
            dto.TiempoLimitePorPregunta,
            _reloj.ObtenerFechaHoraUtc());

        await _repositorio.AgregarTriviaAsync(trivia, cancelacion);

        _registro.LogInformation(
            "Trivia '{Nombre}' (ID: {Id}) creada por el operador {CreadorId}.",
            trivia.Nombre, trivia.Id, trivia.CreadorId);

        return trivia.Id;
    }
}
