namespace SesionesServicio.Aplicacion.Puertos;

// Valida contra juegos-servicio que las misiones de una sesión existan, estén
// activas y tengan etapas. Reutilizado por la creación y la modificación de
// sesiones para no duplicar la lógica.
public interface IValidadorMisionesSesion
{
    Task ValidarAsync(IReadOnlyList<Guid> misionesIds, CancellationToken cancelacion);
}
