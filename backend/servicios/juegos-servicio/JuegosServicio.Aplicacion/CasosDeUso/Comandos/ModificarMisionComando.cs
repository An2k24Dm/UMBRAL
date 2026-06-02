using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ModificarMisionComando(Guid BusquedaId, ModificarMisionDto Dto) : IRequest;
