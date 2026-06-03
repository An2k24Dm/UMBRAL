using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ModificarBusquedaTesoroComando(Guid BusquedaId, ModificarBusquedaTesoroDto Dto) : IRequest;
