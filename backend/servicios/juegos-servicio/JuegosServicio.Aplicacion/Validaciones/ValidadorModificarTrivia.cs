using JuegosServicio.Aplicacion.Comandos.ModificarTrivia;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorModificarTrivia : ValidadorBase<ModificarTriviaComando>
{
    private const int LongitudMaximaNombre = 200;
    private const int LongitudMaximaDescripcion = 1000;
    private const int TiempoMaximoPorPreguntaSegundos = 60;

    protected override void ValidarSolicitud(ModificarTriviaComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Dto;

        if (string.IsNullOrWhiteSpace(dto.NuevoNombre))
            resultado.Agregar("nuevoNombre", "El nombre de la trivia es obligatorio.");
        else if (dto.NuevoNombre.Trim().Length > LongitudMaximaNombre)
            resultado.Agregar("nuevoNombre", $"El nombre no puede superar {LongitudMaximaNombre} caracteres.");

        if (string.IsNullOrWhiteSpace(dto.NuevaDescripcion))
            resultado.Agregar("nuevaDescripcion", "La descripción es obligatoria.");
        else if (dto.NuevaDescripcion.Trim().Length > LongitudMaximaDescripcion)
            resultado.Agregar("nuevaDescripcion", $"La descripción no puede superar {LongitudMaximaDescripcion} caracteres.");

        if (dto.NuevoTiempoLimitePorPregunta <= 0)
            resultado.Agregar("nuevoTiempoLimitePorPregunta", "El tiempo límite por pregunta debe ser mayor a cero.");
        else if (dto.NuevoTiempoLimitePorPregunta > TiempoMaximoPorPreguntaSegundos)
            resultado.Agregar("nuevoTiempoLimitePorPregunta",
                $"El tiempo límite por pregunta no puede superar {TiempoMaximoPorPreguntaSegundos} segundos.");
    }
}
