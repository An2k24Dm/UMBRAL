using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerMisParticipaciones;

public sealed record ObtenerMisParticipacionesConsulta(int Limite = 20)
    : IRequest<IReadOnlyList<MiParticipacionDto>>;
