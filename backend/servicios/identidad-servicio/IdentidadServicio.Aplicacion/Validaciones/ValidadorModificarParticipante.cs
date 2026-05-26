using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorModificarParticipante
    : ValidadorBase<ModificarParticipanteComando>
{
    private readonly IReglasValidacionUsuario _reglas;

    public ValidadorModificarParticipante(IReglasValidacionUsuario reglas)
    {
        _reglas = reglas;
    }

    protected override void ValidarSolicitud(
        ModificarParticipanteComando comando, ResultadoValidacion resultado)
    {
        ValidadorReglasModificacionPerfilUsuario.Validar(
            comando.Datos, _reglas, resultado);
        if (comando.Datos.Alias is not null)
            _reglas.ValidarAlias(comando.Datos.Alias, resultado);
    }
}
