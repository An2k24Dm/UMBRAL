using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSecuencialSesion;

public sealed class ObtenerProgresoSecuencialSesionManejador
    : IRequestHandler<ObtenerProgresoSecuencialSesionConsulta, ProgresoSecuencialSesionDto>
{
    private readonly IServicioProgresoSecuencialSesion _servicioProgreso;

    public ObtenerProgresoSecuencialSesionManejador(
        IServicioProgresoSecuencialSesion servicioProgreso)
    {
        _servicioProgreso = servicioProgreso;
    }

    public Task<ProgresoSecuencialSesionDto> Handle(
        ObtenerProgresoSecuencialSesionConsulta consulta,
        CancellationToken cancelacion)
        => _servicioProgreso.ObtenerParaParticipanteActualAsync(
            consulta.SesionId,
            cancelacion);
}
