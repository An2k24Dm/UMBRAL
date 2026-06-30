using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.IngresarSesionIndividual;

public sealed record IngresarSesionIndividualComando(
    Guid SesionId) : IRequest<IngresarSesionRespuestaDto>;
