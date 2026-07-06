using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Aplicacion.Cadena;

// Eslabón 1: valida que la sesión esté Activa (el operador la inició).
public sealed class EslabonEstadoSesion : IEslabonValidacion
{
    private const string EstadoActiva = "Activa";
    private readonly IClienteSesiones _clienteSesiones;

    public EslabonEstadoSesion(IClienteSesiones clienteSesiones)
    {
        _clienteSesiones = clienteSesiones;
    }

    public async Task ValidarAsync(ContextoValidacionRespuesta contexto, CancellationToken cancelacion)
    {
        var info = await _clienteSesiones.ObtenerInfoPartidaAsync(contexto.SesionId, cancelacion);

        if (info is null)
            throw new SesionNoActivaExcepcion("no encontrada");

        contexto.EstadoSesion = info.Estado;
        contexto.ParticipanteInscrito = info.ParticipanteInscrito;
        contexto.EquipoId = info.EquipoId;

        if (!string.Equals(info.Estado, EstadoActiva, StringComparison.OrdinalIgnoreCase))
            throw new SesionNoActivaExcepcion(info.Estado);
    }
}
