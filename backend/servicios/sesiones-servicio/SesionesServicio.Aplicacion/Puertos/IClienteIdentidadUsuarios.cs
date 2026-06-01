namespace SesionesServicio.Aplicacion.Puertos;

// HU34 — Puerto saliente hacia identidad-servicio.
//
// sesiones-servicio NO modela el rol del creador como dato propio de
// la entidad Sesion. Cuando la regla de visibilidad necesita saber si
// el creador es Administrador, se le pregunta a identidad-servicio
// a través de este puerto. Nunca se consulta la base de identidad
// directamente.
public interface IClienteIdentidadUsuarios
{
    // Devuelve true sólo si el usuario indicado existe como interno y
    // su rol es Administrador. Se usa en el detalle, donde la decisión
    // sólo depende del creador de UNA sesión.
    Task<bool> EsAdministradorAsync(
        Guid usuarioId, CancellationToken cancelacion);

    // Devuelve, de la lista enviada, los identificadores que
    // corresponden a usuarios con rol Administrador. Se usa en el
    // listado del Operador para filtrar sesiones creadas por
    // Administrador con una sola llamada HTTP (en vez de N).
    Task<IReadOnlyCollection<Guid>> FiltrarAdministradoresAsync(
        IReadOnlyCollection<Guid> usuariosIds, CancellationToken cancelacion);
}
