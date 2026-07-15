using JuegosServicio.Aplicacion.Comandos.AgregarPregunta;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorAgregarPregunta : ValidadorBase<AgregarPreguntaComando>
{
    private const int LongitudMaximaEnunciado = 500;
    private const int PuntajeMinimo = Puntaje.MinimoPregunta;
    private const int TiempoMinimoSegundos = Tiempo.MinimoPregunta;
    // Tope de la API, más estricto que el máximo absoluto del dominio.
    private const int TiempoMaximoSegundos = 60;
    private const int MinimoOpciones = 2;
    private const int MaximoOpciones = 4;

    protected override void ValidarSolicitud(AgregarPreguntaComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos;

        if (string.IsNullOrWhiteSpace(dto.Enunciado))
            resultado.Agregar("enunciado", "El enunciado de la pregunta es obligatorio.");
        else if (dto.Enunciado.Trim().Length > LongitudMaximaEnunciado)
            resultado.Agregar("enunciado", $"El enunciado no puede superar {LongitudMaximaEnunciado} caracteres.");

        if (dto.PuntajeAsignado < PuntajeMinimo || dto.PuntajeAsignado % 5 != 0)
            resultado.Agregar("puntajeAsignado", $"El puntaje debe ser múltiplo de 5 y al menos {PuntajeMinimo}.");

        if (dto.TiempoEstimado < TiempoMinimoSegundos || dto.TiempoEstimado > TiempoMaximoSegundos)
            resultado.Agregar("tiempoEstimado", $"El tiempo estimado debe estar entre {TiempoMinimoSegundos} y {TiempoMaximoSegundos} segundos.");

        var opciones = dto.Opciones ?? new();
        if (opciones.Count < MinimoOpciones || opciones.Count > MaximoOpciones)
            resultado.Agregar("opciones", $"La pregunta debe tener entre {MinimoOpciones} y {MaximoOpciones} opciones.");
        else if (!opciones.Any(o => o.EsCorrecta))
            resultado.Agregar("opciones", "Al menos una opción debe ser correcta.");
    }
}
