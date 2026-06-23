using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
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
    private readonly IUsuarioActual _usuarioActual;

    public ObtenerDetalleSesionDisponibleParticipanteManejador(
        IRepositorioSesiones repositorio,
        IClienteJuegosMisiones clienteMisiones,
        IUsuarioActual usuarioActual)
    {
        _repositorio = repositorio;
        _clienteMisiones = clienteMisiones;
        _usuarioActual = usuarioActual;
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
            CodigoAcceso = sesion.CodigoAcceso,
            ParticipacionActual = CalcularParticipacion(sesion)
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

    // HU40 — Determina la participación del usuario autenticado en la sesión.
    // Si no hay identidad o no pertenece, devuelve EstaInscrito = false.
    private ParticipacionActualDto CalcularParticipacion(Sesion sesion)
    {
        var participanteId = _usuarioActual.ObtenerId();
        if (participanteId is not Guid pid || pid == Guid.Empty)
            return new ParticipacionActualDto { EstaInscrito = false };

        if (sesion is SesionGrupal grupal)
        {
            var equipo = grupal.Equipos
                .FirstOrDefault(e => e.ContieneParticipanteIdentidadId(pid));
            if (equipo is null)
                return new ParticipacionActualDto { EstaInscrito = false };

            var integrante = equipo.Participantes
                .First(p => p.ParticipanteIdentidadId == pid);
            return new ParticipacionActualDto
            {
                EstaInscrito = true,
                Tipo = "Equipo",
                EquipoId = equipo.Id,
                EquipoNombre = equipo.Nombre.Valor,
                EsLider = equipo.LiderParticipanteId == integrante.Id,
                ParticipanteSesionId = integrante.Id
            };
        }

        if (sesion is SesionIndividual individual)
        {
            var participante = individual.Participantes
                .FirstOrDefault(p => p.ParticipanteIdentidadId == pid);
            if (participante is null)
                return new ParticipacionActualDto { EstaInscrito = false };

            return new ParticipacionActualDto
            {
                EstaInscrito = true,
                Tipo = "Individual",
                ParticipanteSesionId = participante.Id
            };
        }

        return new ParticipacionActualDto { EstaInscrito = false };
    }
}
