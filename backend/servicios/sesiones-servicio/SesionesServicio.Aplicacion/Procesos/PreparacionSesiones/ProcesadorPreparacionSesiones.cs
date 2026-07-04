using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Procesos.PreparacionSesiones;

public sealed class ProcesadorPreparacionSesiones
{
    private readonly IConsultasSesiones _consultas;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ProcesadorPreparacionSesiones(
        IConsultasSesiones consultas,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IProveedorFechaHora reloj,
        IRegistroLogsAplicacion registroLogs)
    {
        _consultas = consultas;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _registroLogs = registroLogs;
    }

    public async Task<ResultadoCiclo> EjecutarCicloAsync(CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var candidatas = await _consultas.ListarProgramadasVencidasAsync(ahoraUtc, cancelacion);

        if (candidatas.Count == 0)
        {
            _registroLogs.Depuracion(
                evento: "PreparacionSesionesSinCandidatas",
                descripcion: "No hay sesiones Programadas vencidas en este ciclo.",
                propiedades: new Dictionary<string, object?>
                {
                    ["AhoraUtc"] = ahoraUtc
                });
            return new ResultadoCiclo(0, 0);
        }

        _registroLogs.Informacion(
            evento: "PreparacionSesionesDetectadas",
            descripcion: "Detectadas sesiones Programadas vencidas para preparar.",
            propiedades: new Dictionary<string, object?>
            {
                ["Cantidad"] = candidatas.Count
            });

        var preparadas = 0;
        foreach (var sesion in candidatas)
        {
            if (sesion.Estado != EstadoSesion.Programada)
                continue;

            try
            {
                sesion.Preparar();
                await _repositorio.ActualizarAsync(sesion, cancelacion);
                preparadas++;
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "PreparacionSesionFallida",
                    descripcion: "No se pudo preparar la sesión.",
                    propiedades: new Dictionary<string, object?>
                    {
                        ["SesionId"] = sesion.Id
                    });
            }
        }

        if (preparadas > 0)
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "PreparacionSesionesCicloCompletado",
            descripcion: "Ciclo de preparación completado: sesiones pasaron a EnPreparacion.",
            propiedades: new Dictionary<string, object?>
            {
                ["Preparadas"] = preparadas,
                ["Total"] = candidatas.Count
            });

        return new ResultadoCiclo(candidatas.Count, preparadas);
    }

    public readonly record struct ResultadoCiclo(int Encontradas, int Preparadas);
}
