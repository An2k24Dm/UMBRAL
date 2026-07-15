using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ListarSesionesDisponiblesParticipante;

public sealed record ListarSesionesDisponiblesParticipanteConsulta(
    string? Busqueda,
    string? Modo)
    : IRequest<List<SesionDisponibleMovilDto>>;
