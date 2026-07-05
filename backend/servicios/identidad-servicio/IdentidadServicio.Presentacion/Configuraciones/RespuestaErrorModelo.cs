using System.Text.Json;
using IdentidadServicio.Aplicacion.Validaciones;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IdentidadServicio.Presentacion.Configuraciones;

public static class RespuestaErrorModelo
{
    private static readonly Dictionary<string, (string Campo, string Mensaje)> CamposConocidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["fechaNacimiento"] = (
                MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.FechaNacimientoFormato),
            ["$.fechaNacimiento"] = (
                MensajesValidacionUsuario.CampoFechaNacimiento,
                MensajesValidacionUsuario.FechaNacimientoFormato)
        };

    public const string MensajeCuerpoInvalido =
        "El cuerpo de la solicitud no tiene un formato válido.";

    public static IActionResult ConstruirDesdeModelState(ModelStateDictionary modelState)
    {
        var errores = ExtraerErrores(modelState);
        return new BadRequestObjectResult(new
        {
            codigo = "VALIDACION",
            mensaje = "Hay errores de validación.",
            errores
        });
    }

    public static IReadOnlyList<object> ExtraerErrores(ModelStateDictionary modelState)
    {
        var lista = new List<object>();
        foreach (var (clave, entrada) in modelState)
        {
            if (entrada.Errors.Count == 0) continue;
            var (campo, _) = ResolverCampoYMensaje(clave);
            foreach (var error in entrada.Errors)
            {
                var mensaje = ResolverMensajeError(clave, error);
                lista.Add(new { campo, mensaje });
            }
        }

        if (lista.Count == 0)
        {
            lista.Add(new
            {
                campo = "cuerpo",
                mensaje = MensajeCuerpoInvalido
            });
        }
        return lista;
    }

    public static object ConstruirDesdeJsonException(JsonException json)
    {
        var ruta = json.Path ?? string.Empty;
        var (campo, mensaje) = ResolverCampoYMensaje(ruta);
        return new
        {
            codigo = "VALIDACION",
            mensaje = "Hay errores de validación.",
            errores = new[] { new { campo, mensaje } }
        };
    }

    public static object ConstruirDesdeJsonException(
        JsonException json, string? correlationId)
    {
        var ruta = json.Path ?? string.Empty;
        var (campo, mensaje) = ResolverCampoYMensaje(ruta);
        return new
        {
            codigo = "VALIDACION",
            mensaje = "Hay errores de validación.",
            errores = new[] { new { campo, mensaje } },
            correlationId
        };
    }

    private static (string Campo, string Mensaje) ResolverCampoYMensaje(string clavePath)
    {
        if (string.IsNullOrWhiteSpace(clavePath))
            return ("cuerpo", MensajeCuerpoInvalido);

        var normalizado = NormalizarRuta(clavePath);
        if (CamposConocidos.TryGetValue(normalizado, out var conocido))
            return conocido;

        return (normalizado, MensajeCuerpoInvalido);
    }

    private static string ResolverMensajeError(string clave, ModelError error)
    {
        var (_, mensajeCatalogo) = ResolverCampoYMensaje(clave);
        return mensajeCatalogo;
    }

    private static string NormalizarRuta(string ruta)
    {
        var sinPrefijo = ruta.StartsWith("$.", StringComparison.Ordinal)
            ? ruta[2..]
            : ruta;
        if (sinPrefijo.Length == 0) return sinPrefijo;

        var partes = sinPrefijo.Split('.');
        for (var i = 0; i < partes.Length; i++)
        {
            partes[i] = ACamelCase(partes[i]);
        }
        return string.Join('.', partes);
    }

    private static string ACamelCase(string segmento)
    {
        if (string.IsNullOrEmpty(segmento)) return segmento;
        if (char.IsLower(segmento[0])) return segmento;
        return char.ToLowerInvariant(segmento[0]) + segmento[1..];
    }
}
