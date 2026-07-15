using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IServicioProgresoSecuencialSesion
{
    Task<ProgresoSecuencialSesionDto> ObtenerParaParticipanteActualAsync(
        Guid sesionId,
        CancellationToken cancelacion);

    Task ValidarEtapaActualAsync(
        Sesion sesion,
        Guid participanteIdentidadId,
        Guid misionId,
        Guid etapaId,
        string tipoEtapa,
        Guid modoDeJuegoId,
        CancellationToken cancelacion);
}
