using JuegosServicio.Aplicacion.CasosDeUso.Comandos;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorModificarBusquedaTesoro : ValidadorBase<ModificarBusquedaTesoroComando>
{
    private const int LongitudMaximaNombre = 200;
    private const int LongitudMaximaDescripcion = 1000;
    private const int TiempoMinimo = 5;
    private const int PuntajeMinimo = 5;

    protected override void ValidarSolicitud(ModificarBusquedaTesoroComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Dto;

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            resultado.Agregar("nombre", "El nombre de la búsqueda del tesoro es obligatorio.");
        else if (dto.Nombre.Trim().Length > LongitudMaximaNombre)
            resultado.Agregar("nombre", $"El nombre no puede superar {LongitudMaximaNombre} caracteres.");

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            resultado.Agregar("descripcion", "La descripción es obligatoria.");
        else if (dto.Descripcion.Trim().Length > LongitudMaximaDescripcion)
            resultado.Agregar("descripcion", $"La descripción no puede superar {LongitudMaximaDescripcion} caracteres.");

        if (dto.Tiempo < TiempoMinimo)
            resultado.Agregar("tiempo", $"El tiempo debe ser al menos {TiempoMinimo} segundos.");

        if (dto.Puntaje < PuntajeMinimo)
            resultado.Agregar("puntaje", $"El puntaje debe ser al menos {PuntajeMinimo} puntos.");
    }
}
