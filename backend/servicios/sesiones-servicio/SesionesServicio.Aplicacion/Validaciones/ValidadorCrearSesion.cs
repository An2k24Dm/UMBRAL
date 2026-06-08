using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Aplicacion.Validaciones;

// Valida el DTO de creación de sesión. Reglas que dependen de sistemas
// externos (existencia de misión, rol del usuario, fecha futura via
// política) viven en el manejador.
public sealed class ValidadorCrearSesion : ValidadorBase<CrearSesionComando>
{
    private const int LongitudMinimaNombre = 3;
    private const int LongitudMaximaNombre = 150;
    private const int LongitudMaximaDescripcion = 1000;

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

        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            resultado.Agregar("descripcion", "La descripción es obligatoria.");
        else if (dto.Descripcion.Trim().Length > LongitudMaximaDescripcion)
            resultado.Agregar(
                "descripcion",
                $"La descripción no puede superar los {LongitudMaximaDescripcion} caracteres.");

        if (!Enum.TryParse<ModoSesion>(dto.Modo, ignoreCase: true, out var modoParseado)
            || !Enum.IsDefined(typeof(ModoSesion), modoParseado))
            resultado.Agregar(
                "modo",
                "El modo es obligatorio y debe ser Individual o Grupal.");

        if (dto.FechaProgramada == default)
            resultado.Agregar(
                "fechaProgramada",
                "La fecha programada es obligatoria.");

        var misiones = dto.MisionesIds ?? new List<Guid>();
        if (misiones.Count < PoliticaCapacidadSesion.MinimoMisionesPorSesion)
        {
            resultado.Agregar(
                "misionesIds",
                $"Debe seleccionar al menos {PoliticaCapacidadSesion.MinimoMisionesPorSesion} misión.");
        }
        else if (misiones.Count > PoliticaCapacidadSesion.MaximoMisionesPorSesion)
        {
            resultado.Agregar(
                "misionesIds",
                $"No puede seleccionar más de {PoliticaCapacidadSesion.MaximoMisionesPorSesion} misiones.");
        }
        else
        {
            if (misiones.Any(id => id == Guid.Empty))
                resultado.Agregar("misionesIds", "Hay misiones con identificador vacío.");
            if (misiones.Distinct().Count() != misiones.Count)
                resultado.Agregar("misionesIds", "No se pueden repetir misiones.");
        }
    }
}
