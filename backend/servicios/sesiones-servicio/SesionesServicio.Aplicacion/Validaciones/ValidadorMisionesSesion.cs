using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Validaciones;

// Validación de aplicación: comprueba que las misiones de una sesión existan,
// estén activas y tengan etapas. Vive en Aplicación (no en Dominio) porque
// consulta otro microservicio a través del puerto IClienteJuegosMisiones; su
// interfaz IValidadorMisionesSesion está en Puertos.
public sealed class ValidadorMisionesSesion : IValidadorMisionesSesion
{
    private const string EstadoMisionActiva = "Activa";

    private readonly IClienteJuegosMisiones _clienteMisiones;

    public ValidadorMisionesSesion(IClienteJuegosMisiones clienteMisiones)
    {
        _clienteMisiones = clienteMisiones;
    }

    public async Task ValidarAsync(
        IReadOnlyList<Guid> misionesIds, CancellationToken cancelacion)
    {
        foreach (var misionId in misionesIds)
        {
            var mision = await _clienteMisiones.ObtenerMisionAsync(misionId, cancelacion);
            if (mision is null)
                throw new MisionNoEncontradaExcepcion(
                    $"La misión {misionId} no existe.");
            if (!string.Equals(mision.Estado, EstadoMisionActiva, StringComparison.OrdinalIgnoreCase))
                throw new MisionNoActivaExcepcion(
                    $"La misión '{mision.Nombre}' no está activa.");
            if (mision.TotalEtapas <= 0)
                throw new MisionSinEtapasExcepcion(
                    $"La misión '{mision.Nombre}' no tiene etapas.");
        }
    }
}
