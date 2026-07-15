using JuegosServicio.Aplicacion.Comandos.ModificarMision;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorModificarMision : ValidadorBase<ModificarMisionComando>
{
    private const int LongitudMaximaNombre = 200;
    private const int LongitudMaximaDescripcion = 1000;

    protected override void ValidarSolicitud(ModificarMisionComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Dto;

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            resultado.Agregar("nombre", "El nombre de la misión es obligatorio.");
        else if (dto.Nombre.Trim().Length > LongitudMaximaNombre)
            resultado.Agregar("nombre", $"El nombre no puede superar {LongitudMaximaNombre} caracteres.");

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            resultado.Agregar("descripcion", "La descripción es obligatoria.");
        else if (dto.Descripcion.Trim().Length > LongitudMaximaDescripcion)
            resultado.Agregar("descripcion", $"La descripción no puede superar {LongitudMaximaDescripcion} caracteres.");

        if (dto.Dificultad < 0 || dto.Dificultad > 2)
            resultado.Agregar("dificultad", "La dificultad debe ser 0 (Baja), 1 (Media) o 2 (Difícil).");
    }
}
