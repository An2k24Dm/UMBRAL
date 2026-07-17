using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Validaciones;

internal static class ValidacionesPenalizacion
{
    public static void ValidarPuntos(int puntos, ResultadoValidacion resultado)
    {
        if (puntos < CantidadPenalizacion.Minimo || puntos > CantidadPenalizacion.Maximo)
            resultado.Agregar(
                "puntos",
                $"Los puntos deben ser un entero entre {CantidadPenalizacion.Minimo} " +
                $"y {CantidadPenalizacion.Maximo}.");
    }

    public static void ValidarMotivo(string? motivo, ResultadoValidacion resultado)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            resultado.Agregar("motivo", "El motivo de la penalización es obligatorio.");
            return;
        }

        if (motivo.Trim().Length > PenalizacionSesion.MotivoMaximoCaracteres)
            resultado.Agregar(
                "motivo",
                $"El motivo no puede superar {PenalizacionSesion.MotivoMaximoCaracteres} caracteres.");
    }
}
