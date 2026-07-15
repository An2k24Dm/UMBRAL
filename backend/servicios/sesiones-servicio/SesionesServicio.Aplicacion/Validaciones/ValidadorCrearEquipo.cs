using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Validaciones;

// Validación de formato rápida en la capa de aplicación. Las invariantes de
// negocio (unicidad, capacidad, pertenencia) las protege el dominio.
public sealed class ValidadorCrearEquipo : ValidadorBase<CrearEquipoComando>
{
    public const int LongitudMinimaContrasena = 6;

    protected override void ValidarSolicitud(
        CrearEquipoComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos ?? throw new ExcepcionValidacion(
            "Cuerpo de solicitud vacío.",
            new[] { new ErrorValidacion("solicitud", "El cuerpo de la solicitud es obligatorio.") });

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            resultado.Agregar("nombre", "El nombre del equipo es obligatorio.");
        }
        else if (dto.Nombre.Trim().Length > NombreEquipo.LongitudMaxima)
        {
            resultado.Agregar(
                "nombre",
                $"El nombre del equipo no puede superar {NombreEquipo.LongitudMaxima} caracteres.");
        }

        if (!Enum.IsDefined(typeof(TipoEquipoDto), dto.Tipo))
        {
            resultado.Agregar("tipo", "El tipo de equipo debe ser Publico o Privado.");
            return;
        }

        if (dto.Tipo == TipoEquipoDto.Privado)
        {
            if (string.IsNullOrWhiteSpace(dto.Contrasena))
                resultado.Agregar(
                    "contrasena", "Un equipo privado requiere una contraseña.");
            else if (dto.Contrasena.Trim().Length < LongitudMinimaContrasena)
                resultado.Agregar(
                    "contrasena",
                    $"La contraseña debe tener al menos {LongitudMinimaContrasena} caracteres.");
        }
    }
}
