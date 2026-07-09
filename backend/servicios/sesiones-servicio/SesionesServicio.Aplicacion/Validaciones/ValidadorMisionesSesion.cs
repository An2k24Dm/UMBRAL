using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Validaciones;

public sealed class ValidadorMisionesSesion : IValidadorMisionesSesion
{
    private const string EstadoMisionActiva = "Activa";

    private readonly IClienteJuegosMisiones _clienteMisiones;

    public ValidadorMisionesSesion(IClienteJuegosMisiones clienteMisiones)
    {
        _clienteMisiones = clienteMisiones;
    }

    public async Task<ResultadoValidacionMisionesSesion> ValidarYObtenerAsync(
        IReadOnlyList<Guid> misionesIds, CancellationToken cancelacion)
    {
        var misiones = new List<MisionResumenJuegosDto>();
        var duracionTotalSegundos = 0;

        foreach (var misionId in misionesIds)
        {
            var mision = await _clienteMisiones.ObtenerMisionAsync(misionId, cancelacion);
            if (mision is null)
                throw new MisionNoEncontradaExcepcion(
                    $"La mision {misionId} no existe.");

            if (!string.Equals(mision.Estado, EstadoMisionActiva, StringComparison.OrdinalIgnoreCase))
                throw new MisionNoActivaExcepcion(
                    $"La mision '{mision.Nombre}' no esta activa.");

            if (mision.TotalEtapas <= 0)
                throw new MisionSinEtapasExcepcion(
                    $"La mision '{mision.Nombre}' no tiene etapas.");

            if (mision.TiempoTotalSegundos <= 0)
                throw new MisionSinEtapasExcepcion(
                    $"La mision '{mision.Nombre}' no tiene una duracion valida.");

            misiones.Add(mision);
            duracionTotalSegundos += mision.TiempoTotalSegundos;
        }

        return new ResultadoValidacionMisionesSesion(
            misiones.AsReadOnly(),
            duracionTotalSegundos);
    }
}
