using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Validaciones;

// HU33 — valida exclusivamente el contenido del DTO. Las reglas que
// requieren acceso a sistemas externos (existencia y estado del
// contenido, identidad/rol del usuario actual) viven en el manejador
// porque dependen de puertos inyectados.
//
// HU34 — la regla "FechaProgramada debe ser futura" NO vive aquí. Es
// una política de dominio (PoliticaProgramacionSesion); el manejador
// la invoca con la hora actual obtenida de IProveedorFechaHora. Acá
// sólo verificamos que el campo venga informado (no `default`).
public sealed class ValidadorCrearSesion : ValidadorBase<CrearSesionComando>
{
    private const int LongitudMinimaNombre = 3;
    private const int LongitudMaximaNombre = 150;

    protected override void ValidarSolicitud(
        CrearSesionComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos ?? throw new ExcepcionValidacion(
            "Cuerpo de solicitud vacío.",
            new[] { new ErrorValidacion("solicitud", "El cuerpo de la solicitud es obligatorio.") });

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            resultado.Agregar("nombre", "El nombre de la sesión es obligatorio.");
        }
        else
        {
            var longitud = dto.Nombre.Trim().Length;
            if (longitud < LongitudMinimaNombre || longitud > LongitudMaximaNombre)
                resultado.Agregar(
                    "nombre",
                    $"El nombre debe tener entre {LongitudMinimaNombre} y {LongitudMaximaNombre} caracteres.");
        }

        if (!Enum.TryParse<TipoJuego>(dto.TipoJuego, ignoreCase: true, out var tipoJuegoParseado)
            || !Enum.IsDefined(typeof(TipoJuego), tipoJuegoParseado))
            resultado.Agregar(
                "tipoJuego",
                "El tipo de juego es obligatorio y debe ser Trivia o BusquedaTesoro.");

        if (dto.ContenidoJuegoId == Guid.Empty)
            resultado.Agregar("contenidoJuegoId", "Debe indicarse el contenido del juego.");

        if (!Enum.TryParse<ModoSesion>(dto.Modo, ignoreCase: true, out var modoParseado)
            || !Enum.IsDefined(typeof(ModoSesion), modoParseado))
            resultado.Agregar(
                "modo",
                "El modo es obligatorio y debe ser Individual o Grupo.");

        if (dto.FechaProgramada == default)
            resultado.Agregar(
                "fechaProgramada",
                "La fecha programada es obligatoria.");
    }
}
