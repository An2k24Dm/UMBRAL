using JuegosServicio.Aplicacion.Comandos.AgregarPista;

namespace JuegosServicio.Aplicacion.Validaciones;

public sealed class ValidadorAgregarPista : ValidadorBase<AgregarPistaComando>
{
    private const int LongitudMaximaContenido = 1000;

    protected override void ValidarSolicitud(AgregarPistaComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Dto;

        if (dto.Tipo == "CoordenadaGps")
        {
            if (dto.Latitud == null || dto.Longitud == null)
                resultado.Agregar("coordenadas", "Las coordenadas GPS son obligatorias.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Contenido))
                resultado.Agregar("contenido", "El contenido de la pista es obligatorio.");
            else if (dto.Contenido.Trim().Length > LongitudMaximaContenido)
                resultado.Agregar("contenido", $"El contenido no puede superar {LongitudMaximaContenido} caracteres.");
        }
    }
}
