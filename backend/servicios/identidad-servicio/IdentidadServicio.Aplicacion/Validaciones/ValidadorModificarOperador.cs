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
        var dto = comando.Datos;

        // Si llegó DatosContacto con teléfono, normalizamos a la forma canónica.
        if (dto.DatosContacto is not null)
            dto.DatosContacto.Telefono = _reglas.NormalizarTelefono(dto.DatosContacto.Telefono);

        // Solo validamos cada campo si el cliente lo envió.
        if (dto.NombreUsuario is not null)
            _reglas.ValidarNombreUsuario(dto.NombreUsuario, resultado);

        if (dto.Correo is not null)
            _reglas.ValidarCorreo(dto.Correo, resultado);

        if (dto.Nombre is not null)
            _reglas.ValidarNombre(dto.Nombre, resultado);

        if (dto.Apellido is not null)
            _reglas.ValidarApellido(dto.Apellido, resultado);

        if (dto.Sexo is not null)
            _reglas.ValidarSexo(dto.Sexo, resultado);

        if (dto.FechaNacimiento is not null)
            _reglas.ValidarFechaNacimiento(dto.FechaNacimiento, resultado);

        if (dto.DatosContacto?.Telefono is not null)
            _reglas.ValidarTelefono(dto.DatosContacto.Telefono, resultado);

        if (dto.DatosContacto?.Direccion is not null)
            _reglas.ValidarDireccion(dto.DatosContacto.Direccion, resultado);
    }
}
