using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerDetalleSesionDisponibleParticipante;

public sealed class ObtenerDetalleSesionDisponibleParticipanteManejador
    : IRequestHandler<ObtenerDetalleSesionDisponibleParticipanteConsulta,
        SesionDetalleMovilDto>
{
    private static readonly EstadoSesion[] EstadosDisponibles =
    {
        EstadoSesion.Programada,
        EstadoSesion.EnPreparacion,
        EstadoSesion.Activa
    };

    private readonly IRepositorioSesiones _repositorio;
    private readonly IClienteJuegosMisiones _clienteMisiones;

    public ObtenerDetalleSesionDisponibleParticipanteManejador(
        IRepositorioSesiones repositorio,
        IClienteJuegosMisiones clienteMisiones)
    {
        _repositorio = repositorio;
        _clienteMisiones = clienteMisiones;
    }

    public async Task<SesionDetalleMovilDto> Handle(
        ObtenerDetalleSesionDisponibleParticipanteConsulta consulta,
        CancellationToken cancelacion)
    {
        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.SesionId, cancelacion);
        if (sesion is null || !EstadosDisponibles.Contains(sesion.Estado))
            throw new SesionNoEncontradaExcepcion(
                "La sesión no está disponible para consulta.");

        var detalle = new SesionDetalleMovilDto
        {
            Id = sesion.Id,
            Nombre = sesion.Nombre,
            Descripcion = sesion.Descripcion,
            Modo = sesion.TipoSesion,
            Estado = sesion.Estado.ToString(),
            FechaProgramada = sesion.FechaProgramada,
            CodigoAcceso = sesion.CodigoAcceso
        };

        var misionesEnOrden = sesion.Misiones.OrderBy(m => m.Orden).ToList();
        var tareas = misionesEnOrden
            .Select(m => _clienteMisiones.ObtenerMisionConEtapasAsync(m.MisionId, cancelacion))
            .ToList();
        var resultados = await Task.WhenAll(tareas);

        for (var i = 0; i < misionesEnOrden.Count; i++)
        {
            var asociacion = misionesEnOrden[i];
            var misionRemota = resultados[i];

            detalle.Misiones.Add(new MisionSesionMovilDto
            {
                Id = asociacion.MisionId,
                Orden = asociacion.Orden,
                Nombre = misionRemota?.Nombre ?? string.Empty,
                Descripcion = misionRemota?.Descripcion ?? string.Empty,
                Dificultad = misionRemota?.Dificultad,
                TotalEtapas = misionRemota?.Etapas.Count ?? 0,
                Etapas = (misionRemota?.Etapas ?? new List<EtapaJuegosDto>())
                    .OrderBy(e => e.Orden)
                    .Select(e => new EtapaSesionMovilDto
                    {
                        Id = e.Id,
                        Orden = e.Orden,
                        TipoModoDeJuego = e.TipoModoDeJuego,
                        NombreModoDeJuego = e.NombreModoDeJuego,
                        TiempoEstimadoSegundos = e.TiempoEstimado
                    }).ToList()
            });
        }

        return detalle;
    }
}
