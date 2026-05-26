using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record CrearBusquedaTesoroComando(CrearBusquedaTesoroDto Dto, Guid CreadorId) : IRequest<Guid>;
