using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerBusquedaParticipante;

public sealed record ObtenerBusquedaParticipanteConsulta(Guid BusquedaId) : IRequest<BusquedaTesoroParticipanteDto?>;
