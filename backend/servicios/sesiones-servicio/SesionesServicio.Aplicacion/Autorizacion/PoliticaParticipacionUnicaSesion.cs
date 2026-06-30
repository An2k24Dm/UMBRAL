using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Aplicacion.Autorizacion;

public sealed class PoliticaParticipacionUnicaSesion
{
    public const string MensajeOtraSesion =
        "Ya estás participando en otra sesión. Debes esperar a que finalice " +
        "o sea cancelada para ingresar a una nueva.";
    public const string MensajeMismaSesion = "Ya perteneces a esta sesión.";

    private readonly IConsultasSesiones _consultas;

    public PoliticaParticipacionUnicaSesion(IConsultasSesiones consultas)
    {
        _consultas = consultas;
    }

    public async Task ValidarPuedeIngresarASesionAsync(
        Guid participanteIdentidadId,
        Guid sesionDestinoId,
        CancellationToken cancelacion)
    {
        var activa = await _consultas.ObtenerParticipacionActivaDeParticipanteAsync(
            participanteIdentidadId, cancelacion);

        if (activa is null)
            return;

        if (activa.SesionId == sesionDestinoId)
            throw new ParticipanteYaPerteneceASesionExcepcion(MensajeMismaSesion);

        throw new ParticipanteYaEstaEnSesionActivaExcepcion(MensajeOtraSesion);
    }

}
