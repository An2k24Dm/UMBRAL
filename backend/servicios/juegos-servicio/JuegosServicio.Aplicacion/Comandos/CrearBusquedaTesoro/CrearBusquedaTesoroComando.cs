using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.CrearBusquedaTesoro;

public sealed record CrearBusquedaTesoroComando(CrearBusquedaTesoroDto Dto, Guid CreadorId) : IRequest<Guid>;
