using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarBusquedaTesoro;

public sealed record ModificarBusquedaTesoroComando(Guid BusquedaId, ModificarBusquedaTesoroDto Dto) : IRequest;
