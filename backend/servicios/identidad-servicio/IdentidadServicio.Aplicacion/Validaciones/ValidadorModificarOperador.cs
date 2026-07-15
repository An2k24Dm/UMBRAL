using IdentidadServicio.Aplicacion.Comandos.ModificarOperador;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorModificarOperador
    : ValidadorBase<ModificarOperadorComando>
{
    private readonly IReglasValidacionUsuario _reglas;

    public ValidadorModificarOperador(IReglasValidacionUsuario reglas)
    {
        _reglas = reglas;
    }

    protected override void ValidarSolicitud(
        ModificarOperadorComando comando, ResultadoValidacion resultado)
    {
        ValidadorReglasModificacionPerfilUsuario.Validar(
            comando.Datos, _reglas, resultado);
    }
}
