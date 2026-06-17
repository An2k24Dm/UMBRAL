using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Aplicacion.Validaciones;

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

        var modoValido =
            Enum.TryParse<ModoSesion>(dto.Modo, ignoreCase: true, out var modoParseado)
            && Enum.IsDefined(typeof(ModoSesion), modoParseado);
        if (!modoValido)
            resultado.Agregar(
                "modo",
                "El modo es obligatorio y debe ser Individual o Grupal.");
        else
            ValidarCapacidad(modoParseado, dto, resultado);

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
    
    // Solo valida presencia y el mínimo de negocio de cada campo. La capacidad
    // máxima la decide el operador; no hay topes superiores arbitrarios.
    private static void ValidarCapacidad(
        ModoSesion modo, CrearSesionSolicitudDto dto, ResultadoValidacion resultado)
    {
        if (modo == ModoSesion.Individual)
        {
            ValidarMinimo(
                dto.MaximoParticipantes, "maximoParticipantes", "el máximo de participantes",
                PoliticaCapacidadSesion.MinimoParticipantesIndividual, resultado);
        }
        else if (modo == ModoSesion.Grupal)
        {
            ValidarMinimo(
                dto.MaximoEquipos, "maximoEquipos", "el máximo de equipos",
                PoliticaCapacidadSesion.MinimoEquiposPorSesion, resultado);
            ValidarMinimo(
                dto.MaximoParticipantesPorEquipo, "maximoParticipantesPorEquipo",
                "el máximo de participantes por equipo",
                PoliticaCapacidadSesion.MinimoParticipantesPorEquipo, resultado);
        }
    }

    private static void ValidarMinimo(
        int? valor, string campo, string descripcion,
        int minimo, ResultadoValidacion resultado)
    {
        if (valor is null)
            resultado.Agregar(campo, $"Debe indicar {descripcion}.");
        else if (valor < minimo)
            resultado.Agregar(campo, $"El valor de {descripcion} debe ser al menos {minimo}.");
    }
}
