using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ListarSesionesDisponiblesParticipanteConsulta(
    string? Busqueda,
    string? Modo)
    : IRequest<List<SesionDisponibleMovilDto>>;
