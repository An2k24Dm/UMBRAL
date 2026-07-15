using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RankingServicio.Presentacion.Configuraciones;

public static class RespuestaErrorModelo
{
    public static Microsoft.AspNetCore.Mvc.BadRequestObjectResult ConstruirDesdeModelState(
        ModelStateDictionary estado)
    {
        var errores = estado
            .Where(e => e.Value is { Errors.Count: > 0 })
            .SelectMany(e => e.Value!.Errors.Select(err => new
            {
                campo = e.Key,
                mensaje = string.IsNullOrWhiteSpace(err.ErrorMessage)
                    ? "Valor inválido."
                    : err.ErrorMessage
            }))
            .ToArray();

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
        {
            codigo = "VALIDACION",
            mensaje = "Existen errores de validación.",
            errores
        });
    }
}
