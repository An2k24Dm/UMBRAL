using JuegosServicio.Aplicacion.Comandos.CrearTrivia;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorCrearTrivia : ValidadorBase<CrearTriviaComando>
{
    private const int LongitudMaximaNombre = 200;
    private const int LongitudMaximaDescripcion = 1000;
    private const int TiempoMaximoPorPreguntaSegundos = 60;

    protected override void ValidarSolicitud(CrearTriviaComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos;

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            resultado.Agregar("nombre", "El nombre de la trivia es obligatorio.");
        else if (dto.Nombre.Trim().Length > LongitudMaximaNombre)
            resultado.Agregar("nombre", $"El nombre no puede superar {LongitudMaximaNombre} caracteres.");

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            resultado.Agregar("descripcion", "La descripción es obligatoria.");
        else if (dto.Descripcion.Trim().Length > LongitudMaximaDescripcion)
            resultado.Agregar("descripcion", $"La descripción no puede superar {LongitudMaximaDescripcion} caracteres.");

        if (dto.TiempoLimitePorPregunta <= 0)
            resultado.Agregar("tiempoLimitePorPregunta", "El tiempo límite por pregunta debe ser mayor a cero.");
        else if (dto.TiempoLimitePorPregunta > TiempoMaximoPorPreguntaSegundos)
            resultado.Agregar("tiempoLimitePorPregunta",
                $"El tiempo límite por pregunta no puede superar {TiempoMaximoPorPreguntaSegundos} segundos.");
    }
}
