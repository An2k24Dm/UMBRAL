using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores.IngresoSesion;

public sealed class ConstructorRespuestaIngresoSesion
{
    private readonly IClienteJuegosMisiones _clienteMisiones;

    public ConstructorRespuestaIngresoSesion(IClienteJuegosMisiones clienteMisiones)
    {
        _clienteMisiones = clienteMisiones;
    }

    public async Task<IngresarSesionRespuestaDto> ConstruirAsync(
        Sesion sesion,
        Guid participanteIdentidadId,
        bool ingresoRegistrado,
        bool yaPertenecia,
        string mensaje,
        CancellationToken cancelacion)
    {
        var participacion = CalcularParticipacion(sesion, participanteIdentidadId);
        var requiereEquipo = sesion is SesionGrupal && !participacion.EstaInscrito;
        var puedeCrearEquipo = requiereEquipo &&
            sesion is SesionGrupal grupal && grupal.Equipos.Count < grupal.MaximoEquipos;

        var contenido = new List<ContenidoSesionMovilDto>();
        foreach (var asociacion in sesion.Misiones.OrderBy(m => m.Orden))
        {
            var mision = await _clienteMisiones.ObtenerMisionConEtapasAsync(
                asociacion.MisionId, cancelacion);
            var primeraEtapa = mision?.Etapas.OrderBy(e => e.Orden).FirstOrDefault();
            contenido.Add(new ContenidoSesionMovilDto
            {
                MisionId = asociacion.MisionId,
                Orden = asociacion.Orden,
                Nombre = mision?.Nombre ?? string.Empty,
                Descripcion = mision?.Descripcion,
                Tipo = primeraEtapa?.NombreModoDeJuego ?? string.Empty,
                TiempoLimite = primeraEtapa?.TiempoEstimado
            });
        }

        return new IngresarSesionRespuestaDto
        {
            SesionId = sesion.Id,
            NombreSesion = sesion.Nombre,
            CodigoSesion = sesion.CodigoAcceso,
            Estado = sesion.Estado.ToString(),
            Modo = sesion.TipoSesion,
            IngresoRegistrado = ingresoRegistrado,
            RedirigirADetalle = true,
            RequiereEquipo = requiereEquipo,
            PuedeCrearEquipo = puedeCrearEquipo,
            YaPertenecia = yaPertenecia,
            Mensaje = mensaje,
            ParticipacionActual = participacion,
            Contenido = contenido
        };
    }

    private static ParticipacionActualDto CalcularParticipacion(
        Sesion sesion, Guid participanteIdentidadId)
    {
        if (sesion is SesionIndividual individual)
        {
            var participante = individual.Participantes.FirstOrDefault(
                p => p.ParticipanteIdentidadId == participanteIdentidadId);
            return participante is null
                ? new ParticipacionActualDto { EstaInscrito = false }
                : new ParticipacionActualDto
                {
                    EstaInscrito = true,
                    Tipo = "Individual",
                    ParticipanteSesionId = participante.Id
                };
        }

        if (sesion is SesionGrupal grupal)
        {
            var equipo = grupal.Equipos.FirstOrDefault(
                e => e.ContieneParticipanteIdentidadId(participanteIdentidadId));
            if (equipo is null)
                return new ParticipacionActualDto { EstaInscrito = false };

            var participante = equipo.Participantes.First(
                p => p.ParticipanteIdentidadId == participanteIdentidadId);
            return new ParticipacionActualDto
            {
                EstaInscrito = true,
                Tipo = "Equipo",
                ParticipanteSesionId = participante.Id,
                EquipoId = equipo.Id,
                EquipoNombre = equipo.Nombre.Valor,
                EsLider = equipo.LiderParticipanteId == participante.Id
            };
        }

        return new ParticipacionActualDto { EstaInscrito = false };
    }
}
