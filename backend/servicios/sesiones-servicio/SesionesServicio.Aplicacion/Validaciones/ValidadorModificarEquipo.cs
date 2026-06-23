using SesionesServicio.Aplicacion.Comandos.ModificarEquipo;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Validaciones;

public sealed class ValidadorModificarEquipo : ValidadorBase<ModificarEquipoComando>
{
    public const int LongitudMinimaContrasena = 6;

    protected override void ValidarSolicitud(
        ModificarEquipoComando comando, ResultadoValidacion resultado)
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

        if (dto.Tipo == TipoEquipoDto.Privado
            && !string.IsNullOrWhiteSpace(dto.Contrasena)
            && dto.Contrasena.Trim().Length < LongitudMinimaContrasena)
        {
            resultado.Agregar(
                "contrasena",
                $"La contraseña debe tener al menos {LongitudMinimaContrasena} caracteres.");
        }
    }
}
