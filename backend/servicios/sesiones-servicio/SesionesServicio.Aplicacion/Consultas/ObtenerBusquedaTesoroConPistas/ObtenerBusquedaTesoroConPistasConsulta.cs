using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerBusquedaTesoroConPistas;

public sealed record ObtenerBusquedaTesoroConPistasConsulta(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid BusquedaId,
    Guid ParticipanteIdentidadId) : IRequest<BusquedaTesoroConPistasDto?>;
