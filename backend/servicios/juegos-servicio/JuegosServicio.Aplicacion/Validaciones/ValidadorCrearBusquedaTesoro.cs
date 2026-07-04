using JuegosServicio.Aplicacion.Comandos.CrearBusquedaTesoro;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorCrearBusquedaTesoro : ValidadorBase<CrearBusquedaTesoroComando>
{
    private const int LongitudMaximaNombre = 200;
    private const int LongitudMaximaDescripcion = 1000;
    // El tiempo de la búsqueda se expresa en minutos.
    private const int TiempoMinimoMinutos = Tiempo.MinimoBusqueda;
    private const int TiempoMaximoMinutos = 60;
    private const int PuntajeMinimo = Puntaje.MinimoBusqueda;

    protected override void ValidarSolicitud(CrearBusquedaTesoroComando comando, ResultadoValidacion resultado)
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

        if (dto.Tiempo < TiempoMinimoMinutos)
            resultado.Agregar("tiempo", $"El tiempo debe ser al menos {TiempoMinimoMinutos} minutos.");
        else if (dto.Tiempo > TiempoMaximoMinutos)
            resultado.Agregar("tiempo", $"El tiempo no puede superar {TiempoMaximoMinutos} minutos.");

        if (dto.Puntaje < PuntajeMinimo)
            resultado.Agregar("puntaje", $"El puntaje debe ser al menos {PuntajeMinimo} puntos.");
    }
}
