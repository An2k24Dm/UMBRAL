using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

namespace IdentidadServicio.Aplicacion.Validaciones;

// HU09 — validador de edición parcial del Operador.
//
// Regla general: solo se validan los campos que llegaron en la solicitud
// (propiedades no nulas del DTO). Un campo nulo significa "sin cambio" y por
// definición no se revisa ni se reescribe en base de datos. Un campo presente
// se valida con la misma regla común que usaría la creación (HU02), para
// mantener consistencia.
//
// Este validador NO comprueba:
//  * Estado, FechaRegistro, Rol, IdKeycloak — no forman parte del DTO y no se
//    pueden modificar mediante este caso de uso.
//  * Duplicados (correo / nombre de usuario / teléfono): se hacen en el
//    manejador, donde sí hay repositorio y se puede excluir al propio usuario.
//
// La regla "no había cambios para aplicar" se sigue tratando en el manejador:
// devuelve HuboCambios = false sin persistir. El validador solo se encarga
// del formato.
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
        // Delegamos en el helper común — las reglas de edición parcial del
        // perfil son idénticas a las de HU10. Aquí no hay nada específico
        // del Operador.
        ValidadorReglasModificacionPerfilUsuario.Validar(
            comando.Datos, _reglas, resultado);
    }
}
